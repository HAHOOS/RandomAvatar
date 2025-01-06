using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;

namespace RandomAvatar.Patches
{
    [HarmonyPatch]
    public static class ModConsolePatches
    {
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("BoneLib.ModConsole");
            var method = AccessTools.FirstMethod(type, (x) =>
            {
                List<ParameterInfo> @params = [.. x.GetParameters()];
                if (@params.Count != 2)
                    return false;

                return @params[0].Name == "txt" && @params[1].Name == "loggingMode";
            });
            return method;
        }

        public static bool Prefix(string txt)
        {
            return txt != "Remove Element";
        }
    }
}