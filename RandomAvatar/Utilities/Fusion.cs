using System;

using BoneLib;

using Il2CppSLZ.Marrow;

using RandomAvatar.Menu;

using UnityEngine;

namespace RandomAvatar.Utilities
{
    public static class Fusion
    {
        internal const string Allow_MetadataKey = "RandomAvatar.Allow";
        internal const string Delay_MetadataKey = "RandomAvatar.Delay";
        internal const int FusionDelay = 20;
        internal const int DefaultDelay = BoneMenu._rmd;

        internal static Action ServerChanged;

        internal static bool Internal_IsConnected()
        {
            return LabFusion.Network.NetworkInfo.HasServer;
        }

        internal static bool Internal_IsMine(Component comp)
        {
            return comp.GetComponentInParent<RigManager>() == Player.RigManager;
        }

        internal static bool IsMine(Component comp)
        {
            if (IsConnected()) return Internal_IsMine(comp);
            return true;
        }

        internal static void Internal_UpdateMetadata()
        {
            if (Core.Entry_AllowRandomAvatarInYourLobbies == null)
                return;

            if (!LabFusion.Network.NetworkInfo.IsHost)
                return;

            if (LabFusion.Player.LocalPlayer.Metadata == null)
                return;

            LabFusion.Player.LocalPlayer.Metadata.Metadata.TrySetMetadata(Allow_MetadataKey, Core.Entry_AllowRandomAvatarInYourLobbies.Value.ToString());
            LabFusion.Player.LocalPlayer.Metadata.Metadata.TrySetMetadata(Delay_MetadataKey, Core.Entry_MinimumFusionDelay.Value.ToString());
        }

        internal static void UpdateMetadata()
        {
            if (IsConnected()) Internal_UpdateMetadata();
        }

        internal static void Internal_Setup()
        {
            LabFusion.Utilities.MultiplayerHooking.OnJoinedServer += () => ServerChanged?.Invoke();
            LabFusion.Utilities.MultiplayerHooking.OnStartedServer += () => ServerChanged?.Invoke();
            LabFusion.Utilities.MultiplayerHooking.OnDisconnected += () => ServerChanged?.Invoke();
        }

        internal static int Internal_GetMinimumDelay()
        {
            if (Core.Entry_MinimumFusionDelay == null)
                return DefaultDelay;

            if (!LabFusion.Network.NetworkInfo.HasServer)
                return DefaultDelay;

            if (!LabFusion.Entities.NetworkPlayerManager.TryGetPlayer(LabFusion.Player.PlayerIDManager.HostSmallID, out LabFusion.Entities.NetworkPlayer player))
                return DefaultDelay;

            if (player.PlayerID.Metadata?.Metadata?.TryGetMetadata(Delay_MetadataKey, out string val) == null)
            {
                return FusionDelay;
            }
            else
            {
                bool success = int.TryParse(val, out var delay);
                if (!success)
                    return FusionDelay;

                return delay;
            }
        }

        internal static int GetMinimumDelay()
        {
            if (!IsConnected()) return DefaultDelay;
            else return Internal_GetMinimumDelay();
        }

        internal static void Setup()
        {
            if (IsConnected()) Internal_Setup();
        }

        internal static bool Internal_IsAllowed()
        {
            if (!LabFusion.Entities.NetworkPlayerManager.TryGetPlayer(LabFusion.Player.PlayerIDManager.HostSmallID, out LabFusion.Entities.NetworkPlayer player))
                return true;

            if (player.PlayerID.Metadata?.Metadata?.TryGetMetadata(Allow_MetadataKey, out string val) == null)
            {
                return true;
            }
            else
            {
                bool success = bool.TryParse(val, out bool res);
                if (!success)
                    return true;

                return res;
            }
        }

        public static bool IsAllowed()
        {
            if (!IsConnected()) return true;
            else return Internal_IsAllowed();
        }

        internal static void Internal_EnsureSync()
        {
            if (!LabFusion.Network.NetworkInfo.IsHost)
                return;

            if (IsAllowed() != Core.Entry_AllowRandomAvatarInYourLobbies.Value) UpdateMetadata();
            else if (GetMinimumDelay() != Core.Entry_MinimumFusionDelay.Value) UpdateMetadata();
        }

        public static void EnsureSync()
        {
            if (IsConnected()) Internal_EnsureSync();
        }

        public static bool IsConnected()
        {
            if (HelperMethods.CheckIfAssemblyLoaded("labfusion"))
                return Internal_IsConnected();
            else
                return false;
        }
    }
}