using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Connecting
    {
        private static readonly int Id = 16700;
        public static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Connecting);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Ψ");
        public static List<byte> playerIdList = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Connecting, new(2, 15, 1));
            AddOnsAssignDataTeamImp.Create(Id + 10, CustomRoles.Connecting, true, true, true);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
    }
}