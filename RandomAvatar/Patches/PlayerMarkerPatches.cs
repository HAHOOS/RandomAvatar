using HarmonyLib;

using Il2CppSLZ.Marrow.SceneStreaming;

namespace RandomAvatar.Patches
{
    [HarmonyPatch(typeof(PlayerMarker))]
    public static class PlayerMarkerPatches
    {
        public static PlayerMarker Instance;

        [HarmonyPatch(nameof(PlayerMarker.Awake))]
        public static void Postfix(PlayerMarker __instance)
        {
            Instance = __instance;
        }
    }
}