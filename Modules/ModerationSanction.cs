using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using InnerNet;

namespace TownOfHost.Modules;

/// <summary>
/// Discordで確定したホストの警告・利用制限をWorkerから確認し、
/// 対象ホストだけを部屋から退出させて標準の切断ポップアップを表示する。
/// </summary>
internal static class ModerationSanction
{
    private const string StatusUrl = "https://toh-tm.nkzk-owo.workers.dev/status";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static bool Polling;
    private static bool RequestInFlight;

    public static void Reset()
    {
        Polling = false;
        RequestInFlight = false;
    }

    public static void StartPolling()
    {
        if (Polling) return;
        Polling = true;
        _ = PollAsync();
    }

    private static async Task PollAsync()
    {
        try
        {
            await Task.Delay(2000);
            while (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
                await CheckHostAsync();
                await Task.Delay(5000);
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "ModerationSanctionPoll");
        }
        finally
        {
            Polling = false;
        }
    }

    public static async Task CheckHostAsync()
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
        if (PlayerControl.LocalPlayer == null) return;
        if (RequestInFlight) return;

        var friendCode = PlayerControl.LocalPlayer.GetClient()?.FriendCode;
        if (string.IsNullOrWhiteSpace(friendCode))
        {
            Logger.Warn("モデレーション照会: Friend Codeが取得できません", "ModerationSanction");
            return;
        }

        RequestInFlight = true;
        try
        {
            var url = $"{StatusUrl}?friendCode={Uri.EscapeDataString(friendCode)}";
            using var response = await Client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn($"モデレーション照会失敗: {(int)response.StatusCode}", "ModerationSanction");
                return;
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (!root.TryGetProperty("restricted", out var restricted) || !restricted.GetBoolean())
            {
                Logger.Info("モデレーション照会: 制限なし", "ModerationSanction");
                return;
            }

            var count = root.TryGetProperty("count", out var countValue) ? countValue.GetInt32() : 0;
            var label = root.TryGetProperty("label", out var labelValue) ? labelValue.GetString() : "利用制限";
            var message = BuildDisconnectMessage(count, label, root);

            Logger.Warn($"ホストの利用制限を確認: {label} ({friendCode})", "ModerationSanction");
            // Unityのメインスレッドで標準切断ポップアップを表示する。
            _ = new LateTask(() =>
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
                AmongUsClient.Instance.ExitGame(DisconnectReasons.Custom);
                AmongUsClient.Instance.LastCustomDisconnect = "<size=0%>MOD</size>" + message;
            }, 0.1f, "ModerationSanctionExit", true);
        }
        catch (Exception ex)
        {
            // Workerへ到達できない場合は誤キックを避け、通常どおり起動する。
            Logger.Exception(ex, "ModerationSanction");
        }
        finally
        {
            RequestInFlight = false;
        }
    }

    private static string BuildDisconnectMessage(int count, string label, JsonElement root)
    {
        var period = "";
        if (root.TryGetProperty("until", out var until) && until.ValueKind != JsonValueKind.Null)
        {
            if (long.TryParse(until.GetRawText(), out var unixMilliseconds))
                period = $"\n期限: {DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds).ToLocalTime():yyyy/MM/dd HH:mm:ss}";
        }
        if (string.IsNullOrWhiteSpace(period) && label?.Contains("永久") == true)
            period = "\n期限: 永久";

        var title = "TOH-Tm モデレーション通知";
        var action = label?.Contains("警告") == true ? "注意通知" : "利用制限";
        var reason = root.TryGetProperty("reason", out var reasonValue)
            ? Limit(reasonValue.GetString()?.RemoveHtmlTags(), 48) : "不適切な発言";
        var example = root.TryGetProperty("example", out var exampleValue)
            ? Limit(exampleValue.GetString()?.RemoveHtmlTags(), 48) : "";
        var appealId = root.TryGetProperty("appealId", out var appealValue)
            ? Limit(appealValue.GetString(), 24) : "不明";

        return $"<size=180%>{title}</size>\n\n"
            + $"運営の確認結果: {action}\n"
            + $"判定理由: {reason}\n"
            + (string.IsNullOrWhiteSpace(example) ? "" : $"対象メッセージ:「{example}」\n")
            + $"累計判定: {count}回{period}\n"
            + "異議申立て: 公式Discordの問い合わせ窓口\n"
            + $"照会番号: {appealId}";
    }

    private static string Limit(string value, int max)
    {
        value ??= "";
        return value.Length <= max ? value : value[..max] + "…";
    }
}
