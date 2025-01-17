using System;
using System.Collections.Generic;
using System.Reflection;

using BoneLib.BoneMenu;
using BoneLib.BoneMenu.UI;

using HarmonyLib;

using Il2CppTMPro;

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

    [HarmonyPatch(typeof(Keyboard))]
    public static class KeyboardPatches
    {
        [HarmonyPatch(nameof(Keyboard.SubmitOutput))]
        [HarmonyPrefix]
        public static bool Prefix(StringElement ____connectedElement, GUIStringElement ____connectedGUIElement, TMP_InputField ____inputField)
        {
            if (____connectedElement == null)
            {
                throw new NullReferenceException("Connected element is not connected, or is null!");
            }

            ____connectedElement.Value = ____inputField.text;
            ____connectedElement.OnElementSelected();
            return false;
        }
    }

    [HarmonyPatch(typeof(BoneLib.BoneMenu.Menu))]
    public static class MenuPatches
    {
        [HarmonyPatch(nameof(BoneLib.BoneMenu.Menu.DestroyPage))]
        [HarmonyPrefix]
        public static bool Prefix(Page page)
        {
            var _page = page.GetType() != typeof(SubPage) ? page : page.Parent;
            bool currentPage = page.GetType() != typeof(SubPage) ? BoneLib.BoneMenu.Menu.CurrentPage == page : BoneLib.BoneMenu.Menu.CurrentPage == page.Parent;

            if (page.IsIndexedChild && currentPage)
            {
                if (page.Parent.GetNextPage() != null)
                {
                    BoneLib.BoneMenu.Menu.OpenPage(page.Parent.NextPage());
                    return false;
                }

                if (page.Parent.GetPreviousPage() != null)
                {
                    BoneLib.BoneMenu.Menu.OpenPage(page.Parent.PreviousPage());
                    return false;
                }
            }

            var method = typeof(BoneLib.BoneMenu.Menu).GetMethod("Internal_OnPageRemoved", BindingFlags.NonPublic | BindingFlags.Static);
            method?.Invoke(null, [page]);

            BoneLib.BoneMenu.Menu.PageDirectory.Remove(page.Name);

            if (page.Parent.ChildPages.ContainsKey(page.Name))
            {
                page.Parent.ChildPages.Remove(page.Name);
            }

            return false;
        }
    }
}