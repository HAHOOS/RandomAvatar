using System;

using HarmonyLib;

using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow;

using RandomAvatar.Utilities;

namespace RandomAvatar.Patches
{
    [HarmonyPatch(typeof(Player_Health))]
    public static class PlayerHealthPatches
    {
        [HarmonyPatch(nameof(Player_Health.Death))]
        [HarmonyPostfix]
        public static void Postfix(Player_Health __instance)
        {
            if (!Fusion.IsMine(__instance))
                return;

            if (!Core.SwapOnDeath)
                return;

            if (Core.Entry_EffectWhenSwitching.Value && PlayerMarkerPatches.Instance != null && (!PlayerMarkerPatches.Instance.TryGetComponent<PlayerHealthDecorator>(out PlayerHealthDecorator decorator) || !decorator._reloadLevelOnDeath))
                Core.SwapOnNextLevelChange = true;

            Core.SwapToRandom();
        }

        private static DateTime _lastDamage = DateTime.Now;
        private const int _damageDelay = 250; // In milliseconds!!!!

        [HarmonyPatch(nameof(Player_Health.TAKEDAMAGE))]
        [HarmonyPostfix]
        public static void Postfix2(Player_Health __instance)
        {
            if (!Fusion.IsMine(__instance))
                return;

            if (!Core.SwapOnDamaged)
                return;

            if ((DateTime.Now - _lastDamage).TotalMilliseconds >= _damageDelay)
                Core.SwapToRandom();

            _lastDamage = DateTime.Now;
        }
    }
}