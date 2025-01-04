using BoneLib;
using BoneLib.BoneMenu;

using Il2CppSLZ.Marrow.Warehouse;

using RandomAvatar.Helper;
using RandomAvatar.Utilities;

using UnityEngine;

namespace RandomAvatar.Menu
{
    public static class BoneMenu
    {
        /// <summary>
        /// Page appearing first, which is the author: "HAHOOS"
        /// </summary>
        public static Page AuthorPage { get; private set; }

        /// <summary>
        /// SubPage of <see cref="AuthorPage"/>, which contains all of the settings and functions
        /// </summary>
        public static Page ModPage { get; private set; }

        /// <summary>
        /// SubPage of <see cref="ModPage"/>, which contains all of the settings for the mod
        /// </summary>
        public static Page SettingsPage { get; private set; }

        /// <summary>
        /// SubPage of <see cref="ModPage"/>, which contains settings and toggle button for repeated avatar changes which happen every set seconds
        /// </summary>
        public static Page RepeatingPage { get; private set; }

        /// <summary>
        /// SubPage of <see cref="ModPage"/>, which contains page for challenges such as changing avatar when you die or change levels
        /// </summary>
        public static Page ChallengesPage { get; private set; }

        /// <summary>
        /// SubPage of <see cref="ModPage"/>, which allows to whitelist or blacklist certain avatar tags/groups
        /// </summary>
        public static Page BlacklistWhitelistPage { get; private set; }

        internal static FunctionElement RemainingLabel { get; private set; }

        private static float elapsed = 0f;

        public static int Delay { get; internal set; } = 30;
        public static int Remaining { get; internal set; } = 0;
        public static bool SwitchEvery { get; internal set; } = false;

        internal static bool IsSetup = false;

        internal static void AssetWarehouseReady()
        {
            BlacklistWhitelist.SetupPage();
            Hooking.OnWarehouseReady -= AssetWarehouseReady;
        }

        internal static void Setup()
        {
            if (IsSetup)
                return;
            IsSetup = true;

            AuthorPage = Page.Root.CreatePage("HAHOOS", Color.white);
            ModPage = AuthorPage.CreatePage("RandomAvatar", Color.magenta);
            SettingsPage = ModPage.CreatePage("Settings", Color.cyan, 2);
            SettingsPage.CreateBoolPref("Use Redacted Avatars", Color.cyan, ref Core.Entry_UseRedacted);
            SettingsPage.CreateBoolPref("Add effect when switching avatar", Color.magenta, ref Core.Entry_EffectWhenSwitching);

            BlacklistWhitelistPage = ModPage.CreatePage("Blacklist/Whitelist", Color.magenta, 10);
            BlacklistWhitelist.Page = BlacklistWhitelistPage;
            if (!AssetWarehouse.ready)
                Hooking.OnWarehouseReady += BoneMenu.AssetWarehouseReady;
            else
                BlacklistWhitelist.SetupPage();

            RepeatingPage = ModPage.CreatePage("Repeating", Color.red, 3);

            RepeatingPage.CreateInt("Change Delay", Color.yellow, Delay, 10, 10, 3600, (v) => Delay = v);
            FunctionElement remainingElement = null;
            var _switchEvery = RepeatingPage.CreateToggleFunction("Switch to random avatar every set seconds", Color.white, null);
            _switchEvery.Started += () =>
            {
                Remaining = Delay;
                Core.SwapToRandom();
                SwitchEvery = true;
            };
            _switchEvery.Cancelled += () => SwitchEvery = false;
            remainingElement = RepeatingPage.CreateFunction("Time until switch: N/A", Color.white, null);
            remainingElement.SetProperty(ElementProperties.NoBorder);
            RemainingLabel = remainingElement;

            ChallengesPage = ModPage.CreatePage("Challenges", Color.yellow, 2);
            ChallengesPage.CreateBool("Switch to random avatar on death", Color.cyan, false, (v) => Core.SwapOnDeath = v);
            ChallengesPage.CreateBool("Switch to random avatar on level change", Color.blue, false, (v) => Core.SwapOnLevelChange = v);

            ModPage.CreateFunction("Switch to random avatar", Color.white, Core.SwapToRandom);
            ModPage.CreateLabel($"Version: v{Core.Version}{(Core.IsLatestVersion ? string.Empty : "<br><color=#2EFF2E>(Update Available)</color>")}", Color.white);
        }

        internal static void Update()
        {
            if (RemainingLabel != null)
            {
                if (SwitchEvery)
                {
                    elapsed += TimeUtilities.DeltaTime;
                    if (elapsed >= 1f)
                    {
                        Remaining--;
                        elapsed = 0f;
                    }
                    if (Remaining <= 0)
                    {
                        Core.SwapToRandom();
                        Remaining = Delay;
                    }
                    if (!RemainingLabel.ElementName.EndsWith(Remaining.ToString()))
                        RemainingLabel.ElementName = $"Time until switch: {Remaining}";
                }
                else
                {
                    if (!RemainingLabel.ElementName.EndsWith("N/A"))
                        RemainingLabel.ElementName = "Time until switch: N/A";
                }
            }
        }
    }
}