using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

using BoneLib;
using BoneLib.BoneMenu;

using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.RoadToMarrow;

using MelonLoader;
using MelonLoader.Utils;

using RandomAvatar.Helper;
using RandomAvatar.Patches;

using UnityEngine;

namespace RandomAvatar
{
    internal static class BlacklistWhitelist
    {
        public static MelonPreferences_Category Category;

        public static MelonPreferences_Entry<bool> IsWhitelist, Enabled;
        public static MelonPreferences_Entry<List<string>> TagsList;

        public static Page Page { get; internal set; }

        public static bool IsSetup { get; private set; } = false;

        public static Dictionary<string, int> Tags = [];

        public static void Setup()
        {
            if (IsSetup)
                return;
            IsSetup = true;

            Category = MelonPreferences.CreateCategory("RandomAvatar_BlacklistWhitelist");

            Enabled = Category.CreateEntry("Enabled", false, "Enabled",
                description: "If true, whitelist/blacklist will be checked when changing into random avatar");
            IsWhitelist = Category.CreateEntry("IsWhitelist", false, "Is Whitelist",
                description: "If true, the avatar will be allowed if it has the tags, if false, it will be allowed only if it doesn't have the tag");
            TagsList = Category.CreateEntry<List<string>>("TagsList", [], "Tags List",
                description: "List of all tags/groups");

            Category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "RandomAvatar.cfg"));
            Category.SaveToFile(false);

            if (AssetWarehouse.ready)
                Hooks();
            else
                Hooking.OnWarehouseReady += Hooks;
        }

        private static void Hooks()
        {
            AssetWarehouse.Instance.OnChanged += (Il2CppSystem.Action)(() => refreshRequired = true);
            AssetWarehouse.Instance.OnCrateAdded += (Il2CppSystem.Action<Barcode>)((_) => refreshRequired = true);
        }

        static bool refreshRequired = false;

        private static Dictionary<string, int> GetAllTags(SortMode mode, SortDirection direction)
        {
            if (!AssetWarehouse.ready)
                return null;

            Dictionary<string, int> tags = [];
            if (Tags == null || Tags.Count == 0 || refreshRequired)
            {
                var avatars = AssetWarehouse.Instance.GetCrates<AvatarCrate>();
                Tags?.Clear();
                avatars.ForEach((Il2CppSystem.Action<AvatarCrate>)(x =>
                {
                    x.Tags.ForEach((Il2CppSystem.Action<string>)(x =>
                    {
                        if (tags.ContainsKey(x))
                            tags[x]++;
                        else
                            tags.Add(x, 1);
                    }));
                }));
            }
            else
            {
                tags = Tags ?? [];
            }

            if (mode == SortMode.Amount)
            {
                return direction == SortDirection.Ascending ? tags.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value) : tags.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            }
            else
            {
                return direction == SortDirection.Ascending ? tags.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value) : tags.OrderByDescending(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        static SortDirection sortDirection = SortDirection.Descending;
        static SortMode sortMode = SortMode.Amount;
        static readonly List<FunctionElement> tagElemens = [];

        public static void SetupPage()
        {
            if (!IsSetup)
                return;

            if (tagElemens != null || tagElemens.Count > 0)
            {
                try
                {
                    ModConsolePatches.Ignore = false;
                    tagElemens.ForEach(x =>
                    {
                        Page.SubPages.ForEach(y =>
                        {
                            if (y.Elements.Contains(x))
                                y.Remove(x);
                        });
                    });
                }
                finally
                {
                    ModConsolePatches.Ignore = true;
                }
            }
            Page.RemoveAll();
            Tags = GetAllTags(sortMode, sortDirection);
            Page.CreateBoolPref("Enabled", Color.red, ref Enabled, Category);
            Page.CreateBoolPref("Is Whitelist", Color.cyan, ref IsWhitelist, Category);
            Page.CreateEnum("Sort Direction", Color.yellow, sortDirection, (val) =>
            {
                sortDirection = (SortDirection)val;
                SetupPage();
            });
            Page.CreateEnum("Sort Mode", Color.blue, sortMode, (val) =>
            {
                sortMode = (SortMode)val;
                SetupPage();
            });

            Page.CreateFunction("Add all", new Color(0, 1, 0), () =>
            {
                List<string> keys = [];
                foreach (var item in Tags)
                {
                    keys.Add(item.Key);
                }
                TagsList.Value = keys;
                tagElemens.ForEach(item => item.ElementColor = new Color(0, 1, 0));
                Category.SaveToFile(false);
            });
            Page.CreateFunction("Remove all", new Color(0, 1, 0), () =>
            {
                TagsList.Value = [];
                tagElemens.ForEach(item => item.ElementColor = Color.red);
                Category.SaveToFile(false);
            });
            Page.CreateFunction("Refresh", Color.yellow, SetupPage);
            Page.CreateBlank();
            Page.CreateLabel("Go to the next page to see all the tags", Color.white);
            Page.CreateBlank();
            tagElemens.Clear();
            foreach (var tag in Tags)
            {
                FunctionElement element = null;
                element = Page.CreateFunction($"{tag.Key} [{tag.Value}]", TagsList.Value.Contains(tag.Key) ? new Color(0, 1, 0) : Color.red, () =>
                {
                    if (TagsList.Value.Contains(tag.Key))
                    {
                        TagsList.Value.Remove(tag.Key);
                        element.ElementColor = Color.red;
                    }
                    else
                    {
                        TagsList.Value.Add(tag.Key);
                        element.ElementColor = new Color(0, 1, 0);
                    }
                    Category.SaveToFile(false);
                });
                tagElemens.Add(element);
            }
        }

        public static bool IsAvatarAllowed(AvatarCrate crate)
        {
            if (crate == null)
                return false;

            if (!IsSetup)
                return true;

            if (!Enabled.Value)
                return true;

            bool found = false;
            TagsList.Value.ForEach(x =>
            {
                if (crate.Tags.Contains(x))
                {
                    found = true;
                }
            });
            if (found)
                return IsWhitelist.Value;
            else
                return !IsWhitelist.Value;
        }

        public enum SortMode
        {
            Amount,
            Alphabetical
        }

        public enum SortDirection
        {
            Ascending,
            Descending
        }
    }
}