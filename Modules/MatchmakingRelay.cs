using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HarmonyLib;
using InnerNet;

namespace TownOfHost.Modules;

/// <summary>
/// Discord Webhookそのものは持たず、Cloudflare Workerへ募集情報だけ送る。
/// Worker側がDiscordへの投稿・編集・削除を担当する。
/// </summary>
internal static class MatchmakingRelay
{
    private const string RelayUrl = "https://toh-temp.nkzk-owo.workers.dev/";
    private const int UpdateIntervalMs = 6000;
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly object Sync = new();
    private static readonly string StatePath = Path.Combine(Main.BaseDirectory, "toh_temp_matchmaking.txt");

    private static bool active;
    private static bool deleteAfterCreate;
    private static string messageId = "";
    private static string lastSnapshot = "";
    private static long nextUpdateAt;

    public static void Toggle()
    {
        if (!CanUse()) return;

        lock (Sync)
        {
            if (active)
            {
                active = false;
                deleteAfterCreate = true;
                lastSnapshot = "";
            }
            else
            {
                active = true;
                deleteAfterCreate = false;
                nextUpdateAt = 0;
            }
        }

        if (IsActive())
            SendCreate();
        else
            Delete("Private");
    }

    public static void Tick()
    {
        if (!CanUse() || !IsActive()) return;

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (Sync)
        {
            if (now < nextUpdateAt) return;
            nextUpdateAt = now + UpdateIntervalMs;
        }

        var payload = CollectPayload();
        var snapshot = JsonSerializer.Serialize(payload);
        lock (Sync)
        {
            if (snapshot == lastSnapshot) return;
            lastSnapshot = snapshot;
        }

        string id;
        lock (Sync) id = messageId;
        if (string.IsNullOrWhiteSpace(id)) return;

        payload["action"] = "update";
        payload["messageId"] = id;
        Send(payload);
    }

    public static void Delete(string reason)
    {
        string id;
        lock (Sync)
        {
            if (!active && string.IsNullOrWhiteSpace(messageId)) return;
            active = false;
            lastSnapshot = "";
            id = messageId;
            messageId = "";
        }

        if (string.IsNullOrWhiteSpace(id)) return;
        var payload = new Dictionary<string, object>
        {
            ["action"] = "delete",
            ["messageId"] = id,
            ["roomCode"] = GetRoomCode(),
            ["reason"] = reason ?? ""
        };
        Send(payload, clearAfter: true);
    }

    private static void SendCreate()
    {
        var payload = CollectPayload();
        payload["action"] = "create";
        Send(payload, saveCreatedMessage: true);
    }

    private static Dictionary<string, object> CollectPayload()
    {
        var client = AmongUsClient.Instance;
        var roomCode = GetRoomCode();
        var hostName = PlayerControl.LocalPlayer?.Data?.PlayerName ?? "不明なホスト";
        var players = client?.allClients?.Count ?? 0;
        var maxPlayers = Main.NormalOptions?.MaxPlayers ?? 15;
        var region = ServerManager.Instance?.CurrentRegion?.Name ?? "不明";
        var inGame = client != null && (client.IsGameStarted || GameStates.IsInGame);

        return new Dictionary<string, object>
        {
            ["roomCode"] = roomCode,
            ["hostName"] = hostName,
            ["players"] = players,
            ["maxPlayers"] = maxPlayers,
            ["region"] = region,
            ["state"] = inGame ? "ゲーム中" : "ロビー",
            ["modVersion"] = Main.PluginVersion
        };
    }

    private static string GetRoomCode()
    {
        try
        {
            return GameCode.IntToGameName(AmongUsClient.Instance.GameId);
        }
        catch
        {
            return "";
        }
    }

    private static bool CanUse()
        => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;

    private static bool IsActive()
    {
        lock (Sync) return active;
    }

    private static void Send(
        Dictionary<string, object> payload,
        bool saveCreatedMessage = false,
        bool clearAfter = false)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");
                using var response = await Client.PostAsync(RelayUrl, content);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warn($"Matchmaking relay failed: {(int)response.StatusCode} {body}", "MatchmakingRelay");
                    return;
                }

                using var json = JsonDocument.Parse(body);
                if (saveCreatedMessage && json.RootElement.TryGetProperty("messageId", out var id))
                {
                    // Discord returns message.id as a JSON number, not a string.
                    var newId = id.ValueKind == JsonValueKind.String
                        ? id.GetString() ?? ""
                        : id.ToString();
                    bool deleteImmediately;
                    lock (Sync)
                    {
                        messageId = newId;
                        deleteImmediately = deleteAfterCreate || !active;
                        deleteAfterCreate = false;
                    }
                    try { File.WriteAllText(StatePath, newId); } catch { }

                    // If the user turned recruitment off while the create
                    // request was still in flight, remove the just-created
                    // message instead of leaving an orphaned Discord post.
                    if (deleteImmediately)
                    {
                        Delete("PrivateDuringCreate");
                    }
                }
                else if (clearAfter)
                {
                    try { if (File.Exists(StatePath)) File.Delete(StatePath); } catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "MatchmakingRelay");
            }
        });
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.FixedUpdate))]
    private static class FixedUpdatePatch
    {
        public static void Postfix() => Tick();
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
    private static class ExitGamePatch
    {
        public static void Prefix() => Delete("ExitGame");
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnDisconnected))]
    private static class DisconnectedPatch
    {
        public static void Prefix() => Delete("Disconnected");
    }
}
