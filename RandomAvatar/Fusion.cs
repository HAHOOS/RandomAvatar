using BoneLib;

namespace RandomAvatar
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