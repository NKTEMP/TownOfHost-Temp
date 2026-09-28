using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using Hazel;
using HarmonyLib;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor
{
    public sealed class StepBomber : RoleBase, IImpostor, IKiller
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(StepBomber),
                player => new StepBomber(player),
                CustomRoles.StepBomber,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                53900,
                SetupOptionItem,
                "sb",
                colorCode: "#FF0000",
                OptionSort: (3, 2)
            );

        public static OptionItem OptionStepsToDetonate;
        public static OptionItem OptionNotificationTiming;
        public static OptionItem OptionBlastRange;
        public static OptionItem OptionCooldown;
        public static OptionItem OptionMaxBombCount;
        public static OptionItem OptionCanKillImpostor;
        public static OptionItem OptionHasNormalKill;

        public static Dictionary<byte, byte> PlantedBombs = new Dictionary<byte, byte>();
        public static HashSet<byte> IgnitedPlayers = new HashSet<byte>();
        public static Dictionary<byte, Vector2> LastPositions = new Dictionary<byte, Vector2>();
        public static Dictionary<byte, float> RemainingSteps = new Dictionary<byte, float>();

        private static byte _pendingTargetId = byte.MaxValue;
        private static float _clickWindowTimer = 0f;
        private const float DoubleClickThreshold = 0.3f;

        private int _bombCount;
        static bool IsStart = OptionNotificationTiming.GetValue() == (int)NotifyType.Start;
        static bool IsVote = OptionNotificationTiming.GetValue() == (int)NotifyType.Vote;
        static bool IsMend = OptionNotificationTiming.GetValue() == (int)NotifyType.MeetingEnd;

        public StepBomber(PlayerControl player) : base(RoleInfo, player)
        {
            _bombCount = OptionMaxBombCount.GetInt();
        }
        enum OptionName
        {
            MaxBombCount
        }
        public enum NotifyType
        {
            Start,
            Vote,
            MeetingEnd
        }

        private static void SetupOptionItem()
        {
            var targetingModeNames = Enum.GetNames(typeof(NotifyType));

            OptionStepsToDetonate = FloatOptionItem.Create(
                RoleInfo, 10, "StepsToDetonate", new(5f, 300f, 5f), 20f, false);

            OptionNotificationTiming = StringOptionItem.Create(
                RoleInfo, 11, "NotificationTiming", targetingModeNames, 0, false);

            OptionBlastRange = FloatOptionItem.Create(
                RoleInfo, 12, "BlastRange", new(1f, 20f, 1f), 3f, false)
                .SetValueFormat(OptionFormat.Multiplier);

            OptionCooldown = FloatOptionItem.Create(
                RoleInfo, 13, GeneralOption.Cooldown, new(0f, 999f, 0.5f), 15f, false)
                .SetValueFormat(OptionFormat.Seconds);

            OptionMaxBombCount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.MaxBombCount, new(1, 14, 1), 2, false);

            OptionCanKillImpostor = BooleanOptionItem.Create(
                RoleInfo, 15, "CanKillImpostor", false, false);

            OptionHasNormalKill = BooleanOptionItem.Create(
                RoleInfo, 16, "HasNormalKill", false, false);
        }

        public override void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
        {
            roleColor = RoleInfo.RoleColor;
            roleText = GetString("StepBomber");
        }

        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            if (info.AttemptTarget == null || info.AttemptTarget.Data.IsDead)
            {
                info.CanKill = false;
                info.DoKill = false;
                return;
            }

            byte targetId = info.AttemptTarget.PlayerId;

            if (OptionHasNormalKill.GetBool())
            {
                if (_pendingTargetId == targetId && _clickWindowTimer > 0f)
                {
                    info.CanKill = true;
                    info.DoKill = true;
                    _pendingTargetId = byte.MaxValue;
                    _clickWindowTimer = 0f;
                    return;
                }

                _pendingTargetId = targetId;
                _clickWindowTimer = DoubleClickThreshold;

                info.CanKill = false;
                info.DoKill = false;
                return;
            }

            ExecuteBombPlant(info, targetId);
        }

        private void ExecuteBombPlant(MurderInfo info, byte targetId)
        {
            if (_bombCount <= 0 || PlantedBombs.ContainsKey(targetId))
            {
                info.CanKill = false;
                info.DoKill = false;
                return;
            }

            PlantedBombs.Add(targetId, Player.PlayerId);
            _bombCount--;

            Logger.Info($"[StepBomber] Bomb plant: {Player.Data.PlayerName} => {targetId}", "StepBomber");

            info.IsGuard = true;
            info.GuardPower = info.KillPower;
            info.CanKill = false;
            info.DoKill = false;
        }
        public void OnCheckMurderDontKill(MurderInfo info)
        {
            if (OptionHasNormalKill.GetBool() && _clickWindowTimer > 0f)
            {
                return;
            }

            Player.ResetKillCooldown();
            if (CheckMurderPatch.TimeSinceLastKill.ContainsKey(Player.PlayerId))
            {
                CheckMurderPatch.TimeSinceLastKill[Player.PlayerId] = 0f;
            }
        }

        public void OnMurderPlayerAsKiller(MurderInfo info)
        {
        }

        public override string GetProgressText(bool comms = false, bool gamelog = false)
            => Utils.ColorString(_bombCount > 0 ? Color.red : Color.gray, $"({_bombCount})");

        public override void ApplyGameOptions(IGameOptions opt)
        {
        }

        public override void OnFixedUpdate(PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost) return;

            // 
            if (OptionHasNormalKill.GetBool() && _clickWindowTimer > 0f)
            {
                _clickWindowTimer -= Time.fixedDeltaTime;

                // 0.3秒間、2回目のクリックがされずにタイムアップした場合（単発押しが確定）
                if (_clickWindowTimer <= 0f && _pendingTargetId != byte.MaxValue)
                {
                    var target = PlayerCatch.GetPlayerById(_pendingTargetId);
                    if (target != null && target.IsAlive() && _bombCount > 0 && !PlantedBombs.ContainsKey(_pendingTargetId))
                    {
                        PlantedBombs.Add(_pendingTargetId, Player.PlayerId);
                        _bombCount--;

                        Player.ResetKillCooldown();
                        if (CheckMurderPatch.TimeSinceLastKill.ContainsKey(Player.PlayerId))
                        {
                            CheckMurderPatch.TimeSinceLastKill[Player.PlayerId] = 0f;
                        }
                        Player.SetKillCooldown(target: target);

                        Logger.Info($"[StepBomber] DoubleClick timed out. Bomb planted on: {target.Data.PlayerName}", "StepBomber");
                    }

                    // 判定用変数をリセット
                    _pendingTargetId = byte.MaxValue;
                    _clickWindowTimer = 0f;
                }
            }


            if (GameStates.CalledMeeting || GameStates.IsMeeting) return;
            if (!GameStates.IsInTask || IgnitedPlayers.Count == 0) return;

            foreach (var targetId in IgnitedPlayers.ToList())
            {
                var target = PlayerCatch.GetPlayerById(targetId);
                if (target == null || !target.IsAlive())
                {
                    CleanUpTarget(targetId);
                    continue;
                }

                Vector2 currentPos = target.transform.position;

                if (!LastPositions.ContainsKey(targetId))
                {
                    LastPositions[targetId] = currentPos;
                    RemainingSteps[targetId] = OptionStepsToDetonate.GetFloat();
                    continue;
                }

                float distance = Vector2.Distance(LastPositions[targetId], currentPos);

                if (distance > 0.12f)
                {
                    if (distance > 1.5f)
                    {
                        LastPositions[targetId] = currentPos;
                        continue;
                    }

                    float stepsMoved = distance / 0.4f;
                    RemainingSteps[targetId] -= stepsMoved;
                    LastPositions[targetId] = currentPos;

                    if (RemainingSteps[targetId] <= 0f)
                    {
                        Detonate(target);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
        class SafeMeetingStartPatch
        {
            static void Postfix()
            {
                LastPositions.Clear();
                _pendingTargetId = byte.MaxValue;
                _clickWindowTimer = 0f;
                CheckAndTriggerNotification(NotifyType.Start);
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
        class SafeMeetingVotePatch
        {
            public static bool hasTriggeredVote = false;
            static void Postfix(MeetingHud __instance)
            {
                int currentState = Convert.ToInt32(__instance.state);

                if (currentState == 1 && !hasTriggeredVote)
                {
                    CheckAndTriggerNotification(NotifyType.Start);
                    hasTriggeredVote = true;
                }
                else if (currentState != 1)
                {
                    hasTriggeredVote = false;
                }
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
        class SafeMeetingClosePatch
        {
            static void Postfix()
            {
                CheckAndTriggerNotification(NotifyType.MeetingEnd);
            }
        }

        [HarmonyPatch(typeof(CustomRoleManager), nameof(CustomRoleManager.Initialize))]
        class GameInitializePatch
        {
            static void Prefix() => ResetAll();
        }

        public static void CheckAndTriggerNotification(NotifyType currentTiming)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (OptionNotificationTiming.GetValue() != (int)currentTiming) return;

            foreach (var bomb in PlantedBombs.ToList())
            {
                byte targetId = bomb.Key;
                if (IgnitedPlayers.Contains(targetId)) continue;

                var target = PlayerCatch.GetPlayerById(targetId);
                if (target != null && target.IsAlive())
                {
                    Utils.SendMessage(GetString("StepBomberAlert"), target.PlayerId);
                    IgnitedPlayers.Add(targetId);
                }
            }
        }

        private static void Detonate(PlayerControl target)
        {
            if (target == null || !target.IsAlive()) return;

            byte targetId = target.PlayerId;
            Vector2 explodePos = target.transform.position;
            byte bomberId = PlantedBombs.TryGetValue(targetId, out var bId) ? bId : targetId;
            var bomber = PlayerCatch.GetPlayerById(bomberId);

            if (CustomRoleManager.OnCheckMurder(bomber, target, target, target, force: true, DontRoleAbility: true, Killpower: 1, deathReason: CustomDeathReason.Bombed))
            {
                RPC.PlaySoundRPC(bomberId, Sounds.KillSound);
                Utils.SendMessage(string.Format(GetString("StepBomberDetonate"), target.Data.PlayerName), target.PlayerId);
            }

            float range = OptionBlastRange.GetFloat();
            foreach (var alivePlayer in PlayerCatch.AllAlivePlayerControls)
            {
                if (alivePlayer.PlayerId == targetId) continue;

                if (Vector2.Distance(explodePos, alivePlayer.transform.position) <= range)
                {
                    if (!OptionCanKillImpostor.GetBool() && alivePlayer.GetCustomRole().IsImpostor())
                        continue;

                    if (CustomRoleManager.OnCheckMurder(bomber, alivePlayer, alivePlayer, alivePlayer, force: true, DontRoleAbility: true, Killpower: 1, deathReason: CustomDeathReason.Bombed))
                    {
                        Utils.SendMessage(string.Format(GetString("StepBomberSplash"), alivePlayer.Data.PlayerName), alivePlayer.PlayerId);
                    }
                }
            }

            CleanUpTarget(targetId);
        }

        public static void CleanUpTarget(byte targetId)
        {
            PlantedBombs.Remove(targetId);
            IgnitedPlayers.Remove(targetId);
            LastPositions.Remove(targetId);
            RemainingSteps.Remove(targetId);
        }

        public static void ResetAll()
        {
            PlantedBombs.Clear();
            IgnitedPlayers.Clear();
            LastPositions.Clear();
            RemainingSteps.Clear();
            _pendingTargetId = byte.MaxValue;
            _clickWindowTimer = 0f;
            SafeMeetingVotePatch.hasTriggeredVote = false;
        }
    }
}
