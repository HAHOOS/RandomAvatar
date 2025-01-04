using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using LabFusion;

using RandomAvatar.Utilities;

namespace RandomAvatar.Patches
{
    [HarmonyPatch(typeof(FusionMod))]
    public static class FusionPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(FusionMod.OnMainSceneInitializeDelayed))]
        public static void Postfix()
        {
            if (!Fusion.IsConnected())
                return;

            Core.LevelLoaded();
        }
    }
}