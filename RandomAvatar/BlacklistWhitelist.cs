using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using BoneLib.BoneMenu;

using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow.Warehouse;

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
        public static MelonPreferences_Entry<List<string>> TagsList, AvatarList, PalletList;

        public static BoneLib.BoneMenu.Page Page { get; internal set; }

        private static BoneLib.BoneMenu.Page TagsPage { get; set; }

        private static BoneLib.BoneMenu.Page AvatarPage { get; set; }

        private static BoneLib.BoneMenu.Page PalletPage { get; set; }

        public static bool IsSetup { get; private set; } = false;

        public static Dictionary<string, int> Tags = [];

        public static Dictionary<string, string> Avatars = [];

        public static List<PalletRef> PalletRefs = [];

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
            AvatarList = Category.CreateEntry<List<string>>("AvatarList", [], "Avatar List",
                description: "List of all avatar");
            PalletList = Category.CreateEntry<List<string>>("PalletList", [], "Pallet List",
                description: "List of all pallets");

            Category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "RandomAvatar.cfg"));
            Category.SaveToFile(false);
        }

        private static Dictionary<string, int> GetAllTags(SortMode mode, SortDirection direction)
        {
            if (!AssetWarehouse.ready)
                return null;

            Dictionary<string, int> tags = [];
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

            if (mode == SortMode.Amount)
            {
                return direction == SortDirection.Ascending ? tags.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value) : tags.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            }
            else
            {
                return direction == SortDirection.Ascending ? tags.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value) : tags.OrderByDescending(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        private static Dictionary<string, string> GetAllAvatars(SortDirection direction)
        {
            if (!AssetWarehouse.ready)
                return null;

            Dictionary<string, string> avatars = [];
            var _avatars = AssetWarehouse.Instance.GetCrates<AvatarCrate>();
            Avatars?.Clear();
            _avatars.ForEach((Il2CppSystem.Action<AvatarCrate>)(x =>
            {
                if (!Core.Entry_UseRedacted.Value && x.Redacted)
                    return;

                avatars.Add(x.Barcode.ID, x.Title);
            }));

            return direction == SortDirection.Ascending ? avatars.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value) : avatars.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        }

        private static List<PalletRef> GetAllPallets(SortMode mode, SortDirection direction)
        {
            if (!AssetWarehouse.ready)
                return null;

            List<PalletRef> refs = [];
            var pallets = AssetWarehouse.Instance.GetPallets();
            foreach (var p in pallets)
            {
                var @ref = new PalletRef(p);
                if (@ref?.Pallet != null && @ref.Avatars?.Count > 0)
                    refs.Add(@ref);
            }
            if (mode == SortMode.Alphabetical)
            {
                return direction == SortDirection.Ascending ? [.. refs.OrderBy(x => x.Pallet.Title)] : [.. refs.OrderByDescending(x => x.Pallet.Title)];
            }
            else
            {
                return direction == SortDirection.Ascending ? [.. refs.OrderBy(x => x.Avatars.Count)] : [.. refs.OrderByDescending(x => x.Avatars.Count)];
            }
        }

        static SortDirection tags_sortDirection = SortDirection.Descending;
        static SortMode tags_sortMode = SortMode.Amount;

        static SortDirection avatars_sortDirection = SortDirection.Ascending;

        static SortDirection pallets_sortDirection = SortDirection.Descending;
        static SortMode pallets_sortMode = SortMode.Amount;

        static ViewMode tags_viewMode = ViewMode.All;
        static ViewMode avatars_viewMode = ViewMode.All;
        static ViewMode pallets_viewMode = ViewMode.All;

        static string tags_search = string.Empty;
        static string avatars_search = string.Empty;
        static string pallets_search = string.Empty;

        static readonly List<FunctionElement> tagElements = [];

        static readonly List<FunctionElement> avatarElements = [];

        static readonly List<FunctionElement> palletElements = [];

        public static void CleanupPage(BoneLib.BoneMenu.Page page, bool onlySubPages = false, bool removeSubPages = true, bool removeMainPage = false)
        {
            Dictionary<BoneLib.BoneMenu.Page, List<Element>> elements = [];
            page.SubPages.ForEach(subPage =>
            {
                if (subPage != null && subPage.Elements?.Count > 0)
                {
                    elements.Add(subPage, [.. subPage.Elements]);
                }
            });
            foreach (var p in elements)
            {
                if (removeSubPages && page.SubPages.Count != 1) p.Key.Remove([.. p.Value]);
                else p.Value.ForEach(x => p.Key.Remove(x));
            }
            if (!onlySubPages)
            {
                var _elements = new List<Element>(page.Elements);
                if (removeMainPage)
                {
                    page.Remove([.. _elements]);
                }
                else
                {
                    _elements.ForEach(x => page.Remove(x));
                }
            }
        }

        public static void RemoveElement(BoneLib.BoneMenu.Page page, Element element, bool removeSubPages = true, bool removeMainPage = false)
        {
            if (removeMainPage) page.Remove([element]);
            else page.Remove(element);
            page.SubPages.ForEach(subPage =>
            {
                if (subPage != null && subPage.Elements?.Count > 0)
                {
                    if (removeSubPages /*&& page.SubPages.Count != 1*/) subPage.Remove([element]);
                    else subPage.Remove(element);
                }
            });
        }

        private static bool tags_isSetup = false;

        public static void SetupTagsPage()
        {
            if (!IsSetup)
                return;

            Tags = GetAllTags(tags_sortMode, tags_sortDirection);
            TagsPage ??= Page.CreatePage("Tags", Color.red, 10);
            CleanupPage(TagsPage, true);
            if (!tags_isSetup)
            {
                TagsPage.CreateEnum("Sort Direction", Color.yellow, tags_sortDirection, (val) =>
                {
                    tags_sortDirection = (SortDirection)val;
                    SetupTagsPage();
                });
                TagsPage.CreateEnum("Sort Mode", Color.cyan, tags_sortMode, (val) =>
                {
                    tags_sortMode = (SortMode)val;
                    SetupTagsPage();
                });
                TagsPage.CreateEnum("View Mode", Color.magenta, tags_viewMode, (val) =>
                {
                    tags_viewMode = (ViewMode)val;
                    SetupTagsPage();
                });
                TagsPage.CreateString("Search", Color.cyan, tags_search, (val) =>
                {
                    tags_search = val;
                    SetupTagsPage();
                });

                TagsPage.CreateFunction("Add all", new Color(0, 1, 0), () =>
                {
                    List<string> keys = [];
                    foreach (var item in Tags)
                    {
                        keys.Add(item.Key);
                    }
                    TagsList.Value = keys;
                    tagElements.ForEach(item => item.ElementColor = new Color(0, 1, 0));
                    Category.SaveToFile(false);
                });
                TagsPage.CreateFunction("Remove all", new Color(0, 1, 0), () =>
                {
                    TagsList.Value = [];
                    tagElements.ForEach(item => item.ElementColor = Color.red);
                    Category.SaveToFile(false);
                });
                TagsPage.CreateFunction("Refresh", Color.yellow, () => SetupTagsPage());
                TagsPage.CreateBlank();
                TagsPage.CreateLabel("Go to the next page to see all the tags", Color.white);
                TagsPage.CreateBlank();
                tags_isSetup = true;
            }
            tagElements.Clear();
            foreach (var tag in Tags)
            {
                if (!DoesViewModeAllow(TagsList.Value, tag.Key, tags_viewMode) || (!string.IsNullOrWhiteSpace(tags_search) && !tag.Key.Contains(tags_search, System.StringComparison.InvariantCultureIgnoreCase))) continue;
                FunctionElement element = null;
                element = TagsPage.CreateFunction($"{tag.Key} [{tag.Value}]", TagsList.Value.Contains(tag.Key) ? new Color(0, 1, 0) : Color.red, () =>
                {
                    if (TagsList.Value.Contains(tag.Key))
                    {
                        TagsList.Value.Remove(tag.Key);
                        element.ElementColor = Color.red;
                        if (tags_viewMode == ViewMode.On) RemoveElement(TagsPage, element);
                    }
                    else
                    {
                        TagsList.Value.Add(tag.Key);
                        element.ElementColor = new Color(0, 1, 0);
                        if (tags_viewMode == ViewMode.Off) RemoveElement(TagsPage, element);
                    }
                    Category.SaveToFile(false);
                });
                tagElements.Add(element);
            }
            if (tagElements.Count == 0)
                TagsPage.CreateLabel("Nothing to show here :(", Color.white);
        }

        private static bool avatars_isSetup = false;

        public static void SetupAvatarsPage()
        {
            if (!IsSetup)
                return;

            Avatars = GetAllAvatars(avatars_sortDirection);
            AvatarPage ??= Page.CreatePage("Avatars", Color.magenta, 10);
            CleanupPage(AvatarPage, true);
            if (!avatars_isSetup)
            {
                AvatarPage.CreateEnum("Sort Direction", Color.yellow, avatars_sortDirection, (val) =>
                {
                    avatars_sortDirection = (SortDirection)val;
                    SetupAvatarsPage();
                });
                AvatarPage.CreateEnum("View Mode", Color.magenta, avatars_viewMode, (val) =>
                {
                    avatars_viewMode = (ViewMode)val;
                    SetupAvatarsPage();
                });
                AvatarPage.CreateString("Search", Color.yellow, avatars_search, (val) =>
                {
                    avatars_search = val;
                    SetupAvatarsPage();
                });

                AvatarPage.CreateFunction("Add all", new Color(0, 1, 0), () =>
                {
                    List<string> values = [];
                    foreach (var item in Avatars)
                    {
                        values.Add(item.Value);
                    }
                    AvatarList.Value = values;
                    avatarElements.ForEach(item => item.ElementColor = new Color(0, 1, 0));
                    Category.SaveToFile(false);
                });
                AvatarPage.CreateFunction("Remove all", new Color(0, 1, 0), () =>
                {
                    AvatarList.Value = [];
                    avatarElements.ForEach(item => item.ElementColor = Color.red);
                    Category.SaveToFile(false);
                });
                AvatarPage.CreateFunction("Refresh", Color.yellow, () => SetupAvatarsPage());
                AvatarPage.CreateBlank();
                AvatarPage.CreateBlank();
                AvatarPage.CreateLabel("Go to the next page to see all the avatars", Color.white);
                AvatarPage.CreateBlank();
                avatars_isSetup = true;
            }
            avatarElements.Clear();
            foreach (var avatar in Avatars)
            {
                if (!DoesViewModeAllow(AvatarList.Value, avatar.Key, avatars_viewMode) || (!string.IsNullOrWhiteSpace(avatars_search) && !avatar.Value.Contains(avatars_search, System.StringComparison.InvariantCultureIgnoreCase))) continue;
                FunctionElement element = null;
                element = AvatarPage.CreateFunction(avatar.Value, AvatarList.Value.Contains(avatar.Key) ? new Color(0, 1, 0) : Color.red, () =>
                {
                    if (AvatarList.Value.Contains(avatar.Key))
                    {
                        AvatarList.Value.Remove(avatar.Key);
                        element.ElementColor = Color.red;
                        if (avatars_viewMode == ViewMode.On) RemoveElement(AvatarPage, element);
                    }
                    else
                    {
                        AvatarList.Value.Add(avatar.Key);
                        element.ElementColor = new Color(0, 1, 0);
                        if (avatars_viewMode == ViewMode.On) RemoveElement(AvatarPage, element);
                    }
                    Category.SaveToFile(false);
                });
                avatarElements.Add(element);
            }
            if (avatarElements.Count == 0)
                AvatarPage.CreateLabel("Nothing to show here :(", Color.white);
        }

        private static bool pallets_isSetup = false;

        public static void SetupPalletsPage()
        {
            if (!IsSetup)
                return;

            PalletRefs = GetAllPallets(pallets_sortMode, pallets_sortDirection);
            PalletPage ??= Page.CreatePage("Pallets", Color.cyan, 10);
            CleanupPage(PalletPage, true);
            if (!pallets_isSetup)
            {
                PalletPage.CreateEnum("Sort Direction", Color.yellow, pallets_sortDirection, (val) =>
                {
                    pallets_sortDirection = (SortDirection)val;
                    SetupPalletsPage();
                });
                PalletPage.CreateEnum("Sort Mode", Color.blue, pallets_sortMode, (val) =>
                {
                    pallets_sortMode = (SortMode)val;
                    SetupPalletsPage();
                });
                PalletPage.CreateEnum("View Mode", Color.magenta, pallets_viewMode, (val) =>
                {
                    pallets_viewMode = (ViewMode)val;
                    SetupPalletsPage();
                });
                PalletPage.CreateString("Search", Color.cyan, pallets_search, (val) =>
                {
                    pallets_search = val;
                    SetupPalletsPage();
                });

                PalletPage.CreateFunction("Add all", new Color(0, 1, 0), () =>
                {
                    List<string> keys = [];
                    foreach (var item in PalletRefs)
                    {
                        keys.Add(item.Pallet.Barcode.ID);
                    }
                    PalletList.Value = keys;
                    palletElements.ForEach(item => item.ElementColor = new Color(0, 1, 0));
                    Category.SaveToFile(false);
                });
                PalletPage.CreateFunction("Remove all", new Color(0, 1, 0), () =>
                {
                    PalletList.Value = [];
                    palletElements.ForEach(item => item.ElementColor = Color.red);
                    Category.SaveToFile(false);
                });
                PalletPage.CreateFunction("Refresh", Color.yellow, () => SetupPalletsPage());
                PalletPage.CreateBlank();
                PalletPage.CreateLabel("Go to the next page to see all the tags", Color.white);
                PalletPage.CreateBlank();
                pallets_isSetup = true;
            }
            palletElements.Clear();
            foreach (var pallet in PalletRefs)
            {
                if (pallet == null) return;
                if (!DoesViewModeAllow(PalletList.Value, pallet.Pallet.Barcode.ID, pallets_viewMode) || (!string.IsNullOrWhiteSpace(pallets_search) && !pallet.Pallet.Title.Contains(pallets_search, System.StringComparison.InvariantCultureIgnoreCase))) continue;
                FunctionElement element = null;
                element = PalletPage.CreateFunction($"{pallet.Pallet.Title} [{pallet.Avatars.Count}]", PalletList.Value.Contains(pallet.Pallet.Barcode.ID) ? new Color(0, 1, 0) : Color.red, () =>
                {
                    if (PalletList.Value.Contains(pallet.Pallet.Barcode.ID))
                    {
                        PalletList.Value.Remove(pallet.Pallet.Barcode.ID);
                        element.ElementColor = Color.red;
                        if (pallets_viewMode == ViewMode.On) RemoveElement(PalletPage, element);
                    }
                    else
                    {
                        PalletList.Value.Add(pallet.Pallet.Barcode.ID);
                        element.ElementColor = new Color(0, 1, 0);
                        if (pallets_viewMode == ViewMode.Off) RemoveElement(PalletPage, element);
                    }
                    Category.SaveToFile(false);
                });
                palletElements.Add(element);
            }
            if (palletElements.Count == 0)
                PalletPage.CreateLabel("Nothing to show here :(", Color.white);
        }

        private static bool DoesViewModeAllow(List<string> values, string value, ViewMode mode)
        {
            if (value == null) return true;
            if (mode == ViewMode.All) return true;
            if (values.Contains(value)) return mode == ViewMode.On;
            else return mode == ViewMode.Off;
        }

        private static bool mainPageSetup = false;

        public static void SetupPage()
        {
            if (!IsSetup)
                return;

            if (!mainPageSetup)
            {
                Page.CreateBoolPref("Enabled", Color.red, ref Enabled, Category);
                Page.CreateBoolPref("Is Whitelist", Color.cyan, ref IsWhitelist, Category);
                Page.CreateBlank();
                mainPageSetup = true;
            }
            SetupTagsPage();
            SetupAvatarsPage();
            SetupPalletsPage();
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
                    found = true;
            });
            if (!found)
            {
                AvatarList.Value.ForEach(x =>
                {
                    if (crate.Barcode.ID == x)
                        found = true;
                });
            }
            if (!found)
            {
                PalletList.Value.ForEach(x =>
                {
                    if (crate.Pallet.Barcode.ID == x)
                        found = true;
                });
            }
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

        public enum ViewMode
        {
            All,
            On,
            Off
        }
    }

    internal class PalletRef(Pallet pallet)
    {
        public Pallet Pallet { get; set; } = pallet;

        public Il2CppSystem.Collections.Generic.List<Crate> Avatars
        {
            get
            {
                return Pallet.Crates.FindAll((Il2CppSystem.Predicate<Crate>)(x => x.GetIl2CppType().Name == "AvatarCrate"));
            }
        }
    }
}