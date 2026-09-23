using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class Locksmith : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Locksmith),
            player => new Locksmith(player),
            CustomRoles.Locksmith,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            12933,
            SetupOptionItem,
            "ls",
            "#DAA520",
            (9, 7),
            introSound: () => GetIntroSound(RoleTypes.Engineer),
            from: From.None
        );

    public Locksmith(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        count = OptionCount.GetInt();
        cooldown = OptionCooldown.GetInt();
        canOpenDoor = OptionCanOpenDoor.GetBool();
        IsInfinity = count is 0;

        sealedVents = new List<int>();
        banishedPlayers = new Dictionary<byte, float>();
        isBooting = false;
    }

    private static OptionItem OptionCount;
    private static OptionItem OptionCooldown;
    private static OptionItem OptionCanOpenDoor;

    static int cooldown;
    static bool canOpenDoor;
    static bool IsInfinity;
    int count;

    public static List<int> SealedVentsStatic = new List<int>();
    private List<int> sealedVents;
    private Dictionary<byte, float> banishedPlayers;
    private bool isBooting;

    enum OptionName
    {
        OptionCount,
        Cooldown,
        LocksmithCanOpenDoor
    }

    private static void SetupOptionItem()
    {
        OptionCount = IntegerOptionItem.Create(
            RoleInfo, 2910, OptionName.OptionCount,
            new(0, 5, 1), 2, false)
            .SetZeroNotation(OptionZeroNotation.Infinity);

        OptionCooldown = IntegerOptionItem.Create(
            RoleInfo, 2911, OptionName.Cooldown,
            new(0, 180, 1), 30, false);

        OptionCanOpenDoor = BooleanOptionItem.Create(
            RoleInfo, 2912, OptionName.LocksmithCanOpenDoor,
            true, false);
    }

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!CanUseAbility) return false;
        if (sealedVents.Contains(ventId)) return false;

        if (AmongUsClient.Instance.AmHost && !GameStates.CalledMeeting)
        {
            isBooting = true;
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.PlayerId == Player.PlayerId ||
                    p.Data.IsDead) continue;

                if (p.inVent && p.MyPhysics != null)
                {
                    p.MyPhysics.RpcBootFromVent(ventId);
                    p.inVent = false;

                    var allVents = ShipStatus.Instance?.AllVents;
                    if (allVents != null)
                    {
                        var ventObj = allVents.FirstOrDefault(
                            v => v.Id == ventId);
                        if (ventObj != null && p.NetTransform != null)
                        {
                            Vector2 escapePos =
                                (Vector2)ventObj.transform.position +
                                new Vector2(0f, -0.1f);
                            p.NetTransform.SnapTo(escapePos);
                        }
                    }
                    if (!banishedPlayers.ContainsKey(p.PlayerId))
                    {
                        banishedPlayers.Add(p.PlayerId, 1.5f);
                    }
                }
            }
            isBooting = false;
        }

        sealedVents.Add(ventId);
        if (!SealedVentsStatic.Contains(ventId))
        {
            SealedVentsStatic.Add(ventId);
        }

        if (!IsInfinity)
        {
            count--;
            SendRPC();
        }

        Player.KillFlash(false);

        if (physics != null)
        {
            Player.inVent = false;
            physics.RpcExitVent(ventId);
        }

        return false;
    }

    public static bool OnEnterVentOthersHook(
        PlayerPhysics physics, int ventId)
    {
        if (SealedVentsStatic.Contains(ventId))
        {
            if (physics != null && physics.myPlayer != null)
            {
                physics.RpcBootFromVent(ventId);
                physics.myPlayer.inVent = false;

                if (physics.myPlayer.NetTransform != null)
                {
                    var allVents = ShipStatus.Instance?.AllVents;
                    var vent = allVents?.FirstOrDefault(
                        v => v.Id == ventId);
                    if (vent != null)
                    {
                        Vector2 targetPos =
                            (Vector2)vent.transform.position +
                            new Vector2(0f, -0.1f);
                        physics.myPlayer.NetTransform.SnapTo(targetPos);
                    }
                }
            }
            return true;
        }
        return false;
    }

    public override void OnVentilationSystemUpdate(
        PlayerControl user, VentilationSystem.Operation Operation,
        int ventId)
    {
        if (!AmongUsClient.Instance.AmHost || isBooting) return;
        if (user == null || Is(user)) return;

        if (sealedVents.Contains(ventId))
        {
            if (banishedPlayers.ContainsKey(user.PlayerId) &&
                banishedPlayers[user.PlayerId] > 0f)
            {
                user.inVent = false;
                return;
            }

            if (user.MyPhysics != null)
            {
                isBooting = true;
                user.MyPhysics.RpcBootFromVent(ventId);
                user.inVent = false;

                if (!banishedPlayers.ContainsKey(user.PlayerId))
                {
                    banishedPlayers.Add(user.PlayerId, 1.5f);
                }
                else
                {
                    banishedPlayers[user.PlayerId] = 1.5f;
                }

                var allVents = ShipStatus.Instance?.AllVents;
                if (allVents != null)
                {
                    var vent = allVents.FirstOrDefault(v => v.Id == ventId);
                    if (vent != null && user.NetTransform != null)
                    {
                        Vector2 targetPos =
                            (Vector2)vent.transform.position +
                            new Vector2(0f, -0.1f);
                        user.NetTransform.SnapTo(targetPos);
                    }
                }
                isBooting = false;
            }
        }
    }
    public override void OnReportDeadBody(
    PlayerControl reporter, NetworkedPlayerInfo target)
    {
        banishedPlayers.Clear();
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (GameStates.IsInTask && AmongUsClient.Instance.AmHost &&
            banishedPlayers.Count > 0)
        {
            var keys = banishedPlayers.Keys.ToList();
            foreach (var key in keys)
            {
                if (banishedPlayers[key] > 0f)
                {
                    banishedPlayers[key] -= Time.fixedDeltaTime;
                }
            }
        }

        if (!canOpenDoor || !AmongUsClient.Instance.AmHost ||
            !GameStates.IsInTask) return;

        var shipStatus = ShipStatus.Instance;
        if (shipStatus != null && shipStatus.AllDoors != null)
        {
            foreach (var door in shipStatus.AllDoors)
            {
                if (door == null) continue;

                if (!door.IsOpen && Vector2.Distance(
                    Player.transform.position,
                    door.transform.position) < 2.0f)
                {
                    door.SetDoorway(true);
                    break;
                }
            }
        }
    }

    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(count);
        sender.Writer.Write(sealedVents.Count);
        foreach (var ventId in sealedVents)
        {
            sender.Writer.Write(ventId);
        }
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        count = reader.ReadInt32();
        int sealedCount = reader.ReadInt32();
        sealedVents.Clear();
        SealedVentsStatic.Clear();
        for (int i = 0; i < sealedCount; i++)
        {
            int id = reader.ReadInt32();
            sealedVents.Add(id);
            SealedVentsStatic.Add(id);
        }
    }

    public override bool CanVentMoving(
        PlayerPhysics physics, int ventId) => false;

    public override string GetProgressText(
        bool comms = false, bool gamelog = false) =>
        IsInfinity ? "" : Utils.ColorString(
            CanUseAbility ? RoleInfo.RoleColor : Color.gray,
            $"({count})"
        );

    public bool CanUseAbility => IsInfinity || count > 0;
    public override bool CanClickUseVentButton => CanUseAbility;

    public override string GetAbilityButtonText() =>
        CanUseAbility ? "ベント封印" : "";

    public override bool OverrideAbilityButton(out string text)
    {
        if (CanUseAbility)
        {
            text = "Locksmith_Ability";
            return true;
        }
        text = "";
        return false;
    }

    public static Dictionary<int, Achievement> achievements =
        new Dictionary<int, Achievement>();

    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 0, 0, 0);
        achievements.Add(0, n1);

        CustomRoleManager.OnEnterVentOthers.Add(OnEnterVentOthersHook);
    }
}
