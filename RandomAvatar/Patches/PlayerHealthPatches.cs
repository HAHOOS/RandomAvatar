using HarmonyLib;

using Il2CppSLZ.Marrow;

namespace RandomAvatar.Patches
{
    [HarmonyPatch(typeof(Player_Health))]
    public static class PlayerHealthPatches
    {
        internal static bool Internal_IsMine(Player_Health health)
        {
            return LabFusion.Extensions.ComponentExtensions.IsPartOfSelf(health) && LabFusion.Extensions.ComponentExtensions.IsPartOfPlayer(health);
        }

        internal static bool IsMine(Player_Health health)
        {
            if (Fusion.IsConnected()) return Internal_IsMine(health);
            return true;
        }

        [HarmonyPatch(nameof(Player_Health.Death))]
        public static void Postfix(Player_Health __instance)
        {
            if (!IsMine(__instance))
                return;

            if (Core.SwapOnDeath)
                Core.SwapToRandom();
        }
    }
}