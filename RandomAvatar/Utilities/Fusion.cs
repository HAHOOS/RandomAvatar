using BoneLib;

using Il2CppSLZ.Marrow;

using UnityEngine;

namespace RandomAvatar.Utilities
{
    public static class Fusion
    {
        internal static void Internal_SendPullCordEffect()
        {
            LabFusion.Senders.PullCordSender.SendBodyLogEffect();
        }

        public static void SendPullCordEffect()
        {
            if (IsConnected()) Internal_SendPullCordEffect();
        }

        internal static bool Internal_IsConnected()
        {
            return LabFusion.Network.NetworkInfo.HasServer;
        }

        internal static bool Internal_IsMine(Component comp)
        {
            return LabFusion.Extensions.ComponentExtensions.IsPartOfSelf(comp) && LabFusion.Extensions.ComponentExtensions.IsPartOfPlayer(comp);
        }

        internal static bool IsMine(Component comp)
        {
            if (IsConnected()) return Internal_IsMine(comp);
            return true;
        }

        public static bool IsConnected()
        {
            if (HelperMethods.CheckIfAssemblyLoaded("labfusion"))
            {
                return Internal_IsConnected();
            }
            else
            {
                return false;
            }
        }
    }
}