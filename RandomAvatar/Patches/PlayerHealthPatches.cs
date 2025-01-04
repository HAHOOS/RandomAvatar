using HarmonyLib;

using Il2CppSLZ.Marrow;

using RandomAvatar.Utilities;

namespace RandomAvatar.Patches
{
    [HarmonyPatch(typeof(Player_Health))]
    public static class PlayerHealthPatches
    {
        [HarmonyPatch(nameof(Player_Health.Death))]
        public static void Postfix(Player_Health __instance)
        {
            if (!Fusion.IsMine(__instance))
                return;

            if (Core.SwapOnDeath)
                Core.SwapToRandom();
        }
    }
}