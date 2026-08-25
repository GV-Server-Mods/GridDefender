using System;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using VRage.Game.ModAPI;

namespace GVK.GridDefender.Utils
{
    public static class PlayerUtils
    {
        public static bool IsAdmin(ulong steamId)
        {
            if (steamId == 0) return true; // Server console
            return MySession.Static?.IsUserAdmin(steamId) ?? false;
        }

        public static string GetPlayerName(ulong steamId)
        {
            if (steamId == 0) return "Server Console";
            return Sync.Players?.TryGetIdentityNameFromSteamId(steamId) ?? $"Player_{steamId}";
        }

        public static IMyPlayer GetPlayer(ulong steamId)
        {
            if (Sync.Players == null) return null;
            foreach (var p in Sync.Players.GetOnlinePlayers())
            {
                if (p.Id.SteamId == steamId) return p;
            }
            return null;
        }
    }
}

