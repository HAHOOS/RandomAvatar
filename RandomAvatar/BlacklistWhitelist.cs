using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using BoneLib.BoneMenu;

using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.MLAgents;

using MelonLoader;
using MelonLoader.Utils;

using RandomAvatar.Helper;
using RandomAvatar.Patches;

using UnityEngine;

namespace RandomAvatar
{
    internal interface IFilterHandler
    {
        public Func<AvatarCrate, bool> IsOnList { get; set; }

        public abstract bool IsEmpty();

        public abstract void SetupPage();
    }

    internal class FilterHandler<T, TConfig>(BoneLib.BoneMenu.Page mainPage, MelonPreferences_Category category, string entryName) : IFilterHandler
        where T : class
        where TConfig : class
    {
        public BoneLib.BoneMenu.Page MainPage { get; internal set; } = mainPage;

        public MelonPreferences_Category Category { get; internal set; } = category;

        public MelonPreferences_Entry<List<TConfig>> List { get; internal set; } = category.CreateEntry<List<TConfig>>(entryName, []);

        public bool UseSortMode { get; set; } = true;

        public ViewMode ViewMode { get; set; } = ViewMode.All;

        public SortDirection SortDirection { get; set; } = SortDirection.Descending;

        public SortMode SortMode { get; set; } = SortMode.Amount;

        public string Search { get; set; } = string.Empty;

        public string Name { get; set; } = entryName ?? "N/A";

        public Color Color { get; set; } = Color.white;

        public BoneLib.BoneMenu.Page Page { get; private set; }

        public Func<T, TConfig> GetConfigItem;

        public Func<T, string> GetSearchString;

        public Func<T, string> FetchElementName;

        public Func<T[]> GetItems;

        public Func<AvatarCrate, bool> IsOnList { get; set; }

        public void SetupPage()
        {
            Page ??= MainPage.CreatePage(Name, Color, 10);
            BlacklistWhitelist.CleanupPage(Page);
            Page.CreateEnum("Sort Direction", Color.yellow, SortDirection, (val) =>
            {
                SortDirection = (SortDirection)val;
                SetupPage();
            });
            if (UseSortMode)
            {
                Page.CreateEnum("Sort Mode", Color.cyan, SortMode, (val) =>
                {
                    SortMode = (SortMode)val;
                    SetupPage();
                });
            }
            Page.CreateEnum("View Mode", Color.magenta, ViewMode, (val) =>
            {
                ViewMode = (ViewMode)val;
                SetupPage();
            });
            Page.CreateString("Search", Color.cyan, Search, (val) =>
            {
                Search = val;
                SetupPage();
            });

            Page.CreateFunction("Add All", new Color(0, 1, 0), () =>
            {
                GetItems.Invoke().ForEach(x => ChangeList(x, false));
                SetupPage();
                Category.SaveToFile(false);
            });
            Page.CreateFunction("Remove All", new Color(0, 1, 0), () =>
            {
                GetItems.Invoke().ForEach(x => ChangeList(x, true));
                SetupPage();
                Category.SaveToFile(false);
            });
            Page.CreateFunction("Refresh", Color.yellow, () => SetupPage());
            if (!UseSortMode)
                Page.CreateBlank();
            Page.CreateBlank();
            Page.CreateLabel($"Go to the next page to see all the {Name.ToLower()}", Color.white);
            Page.CreateBlank();

            bool any = false;

            foreach (var item in GetItems.Invoke())
            {
                if (item == null) return;
                if (!DoesViewModeAllow(List.Value, GetConfigItem(item), ViewMode)
                    || (!string.IsNullOrWhiteSpace(Search)
                    && !GetSearchString.Invoke(item).Contains(Search, System.StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue;
                }

                FunctionElement element = null;
                any = true;
                element = Page.CreateFunction(FetchElementName?.Invoke(item), OnList(item) ? new Color(0, 1, 0) : Color.red, () =>
                {
                    if (OnList(item))
                    {
                        ChangeList(item, true);
                        element.ElementColor = Color.red;
                        if (ViewMode == ViewMode.On) SetupPage();
                    }
                    else
                    {
                        ChangeList(item, false);
                        element.ElementColor = new Color(0, 1, 0);
                        if (ViewMode == ViewMode.Off) SetupPage();
                    }
                    Category.SaveToFile(false);
                });
            }
            if (!any)
                Page.CreateLabel("Nothing to show here :(", Color.white);
        }

        public bool IsEmpty()
            => List.Value == null || List.Value.Count == 0;

        private void ChangeList(T item, bool remove)
        {
            var configItem = GetConfigItem(item);
            if (remove)
                List.Value.Remove(configItem);
            else
                List.Value.Add(configItem);
        }

        private bool OnList(T item)
        {
            var configItem = GetConfigItem(item);
            return List.Value.Contains(configItem);
        }

        private static bool DoesViewModeAllow(List<TConfig> values, TConfig value, ViewMode mode)
        {
            if (value == null) return true;
            if (mode == ViewMode.All) return true;
            if (values.Contains(value)) return mode == ViewMode.On;
            else return mode == ViewMode.Off;
        }
    }

    internal static class BlacklistWhitelist
    {
        public static MelonPreferences_Category Category { get; private set; }

        public static MelonPreferences_Entry<bool> IsWhitelist, Enabled;

        public static BoneLib.BoneMenu.Page Page { get; internal set; }

        public static bool IsSetup { get; private set; } = false;

        internal static List<IFilterHandler> FilterHandlers { get; } = [];

        public static FilterHandler<AvatarTag, string> TagsHandler { get; private set; }

        public static FilterHandler<AvatarCrate, string> AvatarHandler { get; private set; }

        public static FilterHandler<PalletRef, string> PalletHandler { get; private set; }

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

            TagsHandler = new FilterHandler<AvatarTag, string>(Page, Category, "Tags")
            {
                FetchElementName = (tag) => $"{tag.Tag} [{tag.Count}]",
                GetConfigItem = (tag) => tag.Tag,
                GetItems = GetAllTags,
                GetSearchString = (tag) => tag.Tag,
                Color = Color.cyan,
                UseSortMode = true,
            };
            TagsHandler.IsOnList = (crate) => TagsHandler.List.Value.Any(x => crate.Tags.Contains(x));
            FilterHandlers.Add(TagsHandler);

            AvatarHandler = new FilterHandler<AvatarCrate, string>(Page, Category, "Avatars")
            {
                FetchElementName = (av) => $"{av.Title}",
                GetConfigItem = (av) => av.Barcode.ID,
                GetItems = GetAllAvatars,
                GetSearchString = (av) => av.Title,
                Color = Color.magenta,
                UseSortMode = false,
            };
            AvatarHandler.IsOnList = (crate) => AvatarHandler.List.Value.Any(x => crate.Barcode.ID == x);
            FilterHandlers.Add(AvatarHandler);

            PalletHandler = new FilterHandler<PalletRef, string>(Page, Category, "Pallets")
            {
                FetchElementName = (pallet) => $"{pallet.Pallet.Title} [{pallet.Avatars.Count}]",
                GetConfigItem = (pallet) => pallet.Pallet.Barcode.ID,
                GetItems = GetAllPallets,
                GetSearchString = (pallet) => pallet.Pallet.Title,
                Color = Color.yellow,
                UseSortMode = true
            };
            PalletHandler.IsOnList = (crate) => PalletHandler.List.Value.Any(x => crate.Pallet.Barcode.ID == x);
            FilterHandlers.Add(PalletHandler);

            Category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "RandomAvatar.cfg"));
            Category.SaveToFile(false);
        }

        public static AvatarTag[] GetAllTags()
        {
            if (!AssetWarehouse.ready)
                return [];

            List<AvatarTag> tags = [];
            var avatars = AssetWarehouse.Instance.GetCrates<AvatarCrate>();
            avatars.ForEach((System.Action<AvatarCrate>)(x =>
            {
                x.Tags.ForEach((System.Action<string>)(y =>
                {
                    var index = tags.FindIndex(x => x.Tag == y);
                    if (index != -1)
                        tags[index].Count++;
                    else
                        tags.Add(new(y, 1));
                }));
            }));

            if (TagsHandler.SortMode == SortMode.Amount)
                return [.. TagsHandler.SortDirection == SortDirection.Ascending ? tags.OrderBy(x => x.Count) : tags.OrderByDescending(x => x.Count)];
            else
                return [.. TagsHandler.SortDirection == SortDirection.Ascending ? tags.OrderBy(x => x.Tag) : tags.OrderByDescending(x => x.Tag)];
        }

        public static AvatarCrate[] GetAllAvatars()
        {
            if (!AssetWarehouse.ready)
                return [];

            var _avatars = AssetWarehouse.Instance.GetCrates<AvatarCrate>();
            if (!Core.Entry_UseRedacted.Value)
                _avatars.RemoveAll((Il2CppSystem.Predicate<AvatarCrate>)(x => x.Redacted));
            var avatarsList = _avatars.ToArray().ToList();
            return AvatarHandler.SortDirection == SortDirection.Ascending ? [.. avatarsList.OrderBy(x => x.Title)] : [.. avatarsList.OrderByDescending(x => x.Title)];
        }

        public static PalletRef[] GetAllPallets()
        {
            if (!AssetWarehouse.ready)
                return [];

            List<PalletRef> refs = [];
            foreach (var p in AssetWarehouse.Instance.GetPallets())
            {
                var @ref = new PalletRef(p);
                if (@ref?.Pallet != null && @ref.Avatars?.Count > 0)
                    refs.Add(@ref);
            }
            if (PalletHandler.SortMode == SortMode.Alphabetical)
                return PalletHandler.SortDirection == SortDirection.Ascending ? [.. refs.OrderBy(x => x.Pallet.Title)] : [.. refs.OrderByDescending(x => x.Pallet.Title)];
            else
                return PalletHandler.SortDirection == SortDirection.Ascending ? [.. refs.OrderBy(x => x.Avatars.Count)] : [.. refs.OrderByDescending(x => x.Avatars.Count)];
        }

        public static void CleanupPage(BoneLib.BoneMenu.Page page)
        {
            page.IndexPages.ForEach(x =>
            {
                x.RemoveAll();

                // fuck this fuckass code, why the FUCK does it sitll not work
                // i swear indexed pages are such a pain, just fucking work
                // why can i not fucking REMOVE them
                BoneLib.BoneMenu.Menu.DestroyPage(x);
            });
            page.RemoveAll();
        }

        public static void RemoveElement(BoneLib.BoneMenu.Page page, Element element, bool removeSubPages = true, bool removeMainPage = false)
        {
            if (removeMainPage) page.Remove([element]);
            else page.Remove(element);
            page.IndexPages.ForEach(subPage =>
            {
                if (subPage != null && subPage.Elements?.Count > 0)
                {
                    if (removeSubPages /*&& page.SubPages.Count != 1*/) subPage.Remove([element]);
                    else subPage.Remove(element);
                }
            });
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
            FilterHandlers.ForEach(x => x.SetupPage());
        }

        public static bool IsAvatarAllowed(AvatarCrate crate)
        {
            if (crate == null)
                return false;

            if (!IsSetup)
                return true;

            if (!Enabled.Value)
                return true;

            if (FilterHandlers.Any(x => x.IsOnList.Invoke(crate)))
                return IsWhitelist.Value;
            else
                return !IsWhitelist.Value;
        }
    }

    public class AvatarTag(string tag, int count = 0)
    {
        public string Tag { get; set; } = tag;
        public int Count { get; set; } = count;
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