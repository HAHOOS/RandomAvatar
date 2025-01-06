using System;
using System.Collections.Generic;
using System.Linq;

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
        /// SubPage of <see cref="ModPage"/>, which contains settings and information for Fusion servers
        /// </summary>
        public static Page FusionPage { get; private set; }

        /// <summary>
        /// SubPage of <see cref="ModPage"/>, which contains history of chosen avatars
        /// </summary>
        public static Page HistoryPage { get; private set; }

        /// <summary>
        /// SubPage of <see cref="HistoryPage"/>, which contains information about a selected avatar in <see cref="HistoryPage"/>
        /// </summary>
        public static Page AvatarHistoryPage { get; private set; }

        /// <summary>
        /// SubPage of <see cref="HistoryPage"/>, which contains tags of a selected avatar in <see cref="HistoryPage"/>
        /// </summary>
        public static Page AvatarHistoryTagsPage { get; private set; }

        /// <summary>
        /// SubPage of <see cref="ModPage"/>, which allows to whitelist or blacklist certain avatar tags/groups
        /// </summary>
        public static Page BlacklistWhitelistPage { get; private set; }

        internal static FunctionElement RemainingLabel { get; private set; }

        private static float elapsed = 0f;

        public static int Delay { get; private set; } = 30;
        public static int Remaining { get; private set; } = 0;
        public static bool SwitchEvery { get; private set; } = false;

        public static bool SwapOnStart { get; private set; } = true;

        private static IntElement DelayElement;

        public static int MinimumDelay { get { return Fusion.GetMinimumDelay(); } }

#if DEBUG
        internal const int _rmd = 1;
#else
        internal const int _rmd = 10;
#endif

        private static int RepeatingMinimumDelay { get { var delay = Fusion.GetMinimumDelay(); if (delay < _rmd) delay = _rmd; return delay; } }

        internal static bool IsSetup = false;

        internal static Action EverySecond;

        internal static void AssetWarehouseReady()
        {
            BlacklistWhitelist.SetupPage();
            Hooking.OnWarehouseReady -= AssetWarehouseReady;
        }

        internal static int Cooldown = 0;
        internal static FunctionElement CooldownLabel { get; private set; }

        internal static void Setup()
        {
            if (IsSetup)
                return;
            IsSetup = true;

            AuthorPage = Page.Root.CreatePage("HAHOOS", Color.white);
            ModPage = AuthorPage.CreatePage("RandomAvatar", Color.magenta);
            SettingsPage = ModPage.CreatePage("Settings", Color.cyan, 4);
            SettingsPage.CreateBoolPref("Use Redacted Avatars", Color.cyan, ref Core.Entry_UseRedacted);
            SettingsPage.CreateBoolPref("Add effect when switching avatar", Color.magenta, ref Core.Entry_EffectWhenSwitching);

            BlacklistWhitelistPage = ModPage.CreatePage("Blacklist/Whitelist", Color.magenta, 10);
            BlacklistWhitelist.Page = BlacklistWhitelistPage;
            if (!AssetWarehouse.ready)
                Hooking.OnWarehouseReady += BoneMenu.AssetWarehouseReady;
            else
                BlacklistWhitelist.SetupPage();

            _lastMinimumDelay = MinimumDelay;

            HistoryPage = ModPage.CreatePage("History", Color.yellow, 10);
            AvatarHistoryPage = HistoryPage.CreatePage("Placeholder", Color.white, 0, false);
            AvatarHistoryTagsPage = HistoryPage.CreatePage("Tags", Color.red, 0, false);
            SetupHistoryPage();

            RepeatingPage = ModPage.CreatePage("Repeating", Color.red, 4);

            UpdateRepeating();

            ChallengesPage = ModPage.CreatePage("Challenges", Color.yellow, 2);
            ChallengesPage.CreateBool("Switch to random avatar on death", Color.cyan, false, (v) => Core.SwapOnDeath = v);
            ChallengesPage.CreateBool("Switch to random avatar on level change", Color.yellow, false, (v) => Core.SwapOnLevelChange = v);

            if (HelperMethods.CheckIfAssemblyLoaded("labfusion"))
            {
                FusionPage = ModPage.CreatePage("Fusion", Color.cyan);
                SetupFusionPage();
                Fusion.ServerChanged += () =>
                {
                    SetupFusionPage();
                    Cooldown = 0;
                };
                EverySecond += UpdateLabels;
                EverySecond += Fusion.EnsureSync;
            }
            EverySecond += CheckDelay;

            ModPage.CreateFunction("Switch to random avatar", Color.white, Core.SwapToRandom);
            if (HelperMethods.CheckIfAssemblyLoaded("labfusion"))
            {
                CooldownLabel = ModPage.CreateLabel($"{(Cooldown > 0 ? $"Cooldown: {Cooldown}" : "Ready to use")}", Color.white);
            }
            EverySecond += () =>
            {
                if (CooldownLabel != null)
                {
                    if (Fusion.IsConnected() && Cooldown > 0)
                        Cooldown--;
                    else if (!Fusion.IsConnected())
                        Cooldown = 0;
                    if (Fusion.IsConnected())
                        CooldownLabel.ElementName = $"{(Cooldown > 0 ? $"Cooldown: {Cooldown}" : "Ready to use")}";
                    else
                        CooldownLabel.ElementName = string.Empty;
                }
            };
            ModPage.CreateLabel($"Version: v{Core.Version}{(Core.IsLatestVersion ? string.Empty : "<br><color=#2EFF2E>(Update Available)</color>")}", Color.white);
        }

        internal static FunctionElement IsAllowedLabel;
        internal static FunctionElement FusionDelayLabel;

        internal static KeyValuePair<string, string> SelectedAvatarHistory;

        internal static void SetupTagsPage(AvatarCrate crate)
        {
            BlacklistWhitelist.refreshRequired = true;
            BlacklistWhitelist.SetupTagsPage(true);
            BlacklistWhitelist.CleanupPage(AvatarHistoryTagsPage);
            if (crate.Tags.Count <= 0)
            {
                AvatarHistoryTagsPage.CreateLabel("Nothing to show here :(", Color.white);
            }
            else
            {
                crate.Tags.ForEach((System.Action<string>)(x =>
                {
                    if (string.IsNullOrWhiteSpace(x))
                        return;

                    //if (!BlacklistWhitelist.Tags.ContainsKey(x))
                    //    return;

                    int amount = BlacklistWhitelist.Tags.ContainsKey(x) ? BlacklistWhitelist.Tags[x] : 0;
                    FunctionElement element = null;
                    element = AvatarHistoryTagsPage.CreateFunction($"{x} [{amount}]", BlacklistWhitelist.TagsList.Value.Contains(x) ? new Color(0, 1, 0) : Color.red, () =>
                    {
                        if (BlacklistWhitelist.TagsList.Value.Contains(x))
                        {
                            BlacklistWhitelist.TagsList.Value.Remove(x);
                            element.ElementColor = Color.red;
                        }
                        else
                        {
                            BlacklistWhitelist.TagsList.Value.Add(x);
                            element.ElementColor = new Color(0, 1, 0);
                        }
                        BlacklistWhitelist.Category.SaveToFile(false);
                    });
                }));
            }
        }

        internal static void SetupPageForAvatar(DateTimeOffset offset, AvatarCrate crate)
        {
            AvatarHistoryPage.Name = crate.Title;
            AvatarHistoryPage.RemoveAll();
            AvatarHistoryPage.CreateFunction(crate.Title, Color.white, null);
            AvatarHistoryPage.CreateFunction(crate.Barcode.ID, Color.white, null);
            FunctionElement palletElement = null;
            palletElement = AvatarHistoryPage.CreateFunction(crate.Pallet.Title, BlacklistWhitelist.PalletList.Value.Contains(crate.Pallet.Barcode.ID) ? new Color(0, 1, 0) : Color.red, () =>
            {
                if (BlacklistWhitelist.PalletList.Value.Contains(crate.Pallet.Barcode.ID))
                {
                    BlacklistWhitelist.PalletList.Value.Remove(crate.Pallet.Barcode.ID);
                    palletElement.ElementColor = Color.red;
                }
                else
                {
                    BlacklistWhitelist.PalletList.Value.Add(crate.Pallet.Barcode.ID);
                    palletElement.ElementColor = new Color(0, 1, 0);
                }
                BlacklistWhitelist.Category.SaveToFile(false);
            });
            AvatarHistoryPage.CreateFunction($"Tags ({crate.Tags.Count})", Color.red, () =>
            {
                SetupTagsPage(crate);
                BoneLib.BoneMenu.Menu.OpenPage(AvatarHistoryTagsPage);
            });
            AvatarHistoryPage.CreateFunction($"Time: {offset.ToString("T")}", Color.white, null);
            FunctionElement element = null;
            element = AvatarHistoryPage.CreateFunction(BlacklistWhitelist.AvatarList.Value.Contains(crate.Barcode.ID) ? (BlacklistWhitelist.IsWhitelist.Value ? "Whitelisted" : "Blacklisted") : (BlacklistWhitelist.IsWhitelist.Value ? "Not Whitelisted" : "Not Blacklisted"), BlacklistWhitelist.AvatarList.Value.Contains(crate.Barcode.ID) ? new Color(0, 1, 0) : Color.red, () =>
            {
                if (BlacklistWhitelist.AvatarList.Value.Contains(crate.Barcode.ID))
                {
                    BlacklistWhitelist.AvatarList.Value.Remove(crate.Barcode.ID);
                    element.ElementName = BlacklistWhitelist.IsWhitelist.Value ? "Not Whitelisted" : "Not Blacklisted";
                    element.ElementColor = Color.red;
                }
                else
                {
                    BlacklistWhitelist.AvatarList.Value.Add(crate.Barcode.ID);
                    element.ElementName = BlacklistWhitelist.IsWhitelist.Value ? "Whitelisted" : "Blacklisted";
                    element.ElementColor = new Color(0, 1, 0);
                }
                BlacklistWhitelist.Category.SaveToFile(false);
            });
        }

        internal static void SetupHistoryPage()
        {
            if (HistoryPage == null)
                return;

            BlacklistWhitelist.CleanupPage(HistoryPage);
            HistoryPage.CreateFunction("Clear all", Color.red, () =>
            {
                Core._avatarHistory.Clear();
                SetupHistoryPage();
            }).SetProperty(ElementProperties.NoBorder);
            HistoryPage.CreateBlank();
            if (Core._avatarHistory.Count <= 0)
            {
                HistoryPage.CreateLabel("Nothing to show here :(", Color.white);
            }
            else
            {
                Core._avatarHistory = Core._avatarHistory.OrderByDescending(x => x.Key.ToUnixTimeMilliseconds()).ToDictionary(x => x.Key, x => x.Value);
                Core._avatarHistory.ForEach(y =>
                {
                    var x = y.Value;
                    HistoryPage.CreateFunction(x.Title, Color.yellow, () =>
                    {
                        SetupPageForAvatar(y.Key, x);
                        BoneLib.BoneMenu.Menu.OpenPage(AvatarHistoryPage);
                    }).SetProperty(ElementProperties.NoBorder);
                });
            }
        }

        internal static void UpdateRepeating()
        {
            if (RepeatingPage == null)
                return;

            RepeatingPage.RemoveAll();

            DelayElement = RepeatingPage.CreateInt("Change Delay", Color.yellow, Delay, 10, RepeatingMinimumDelay, 3600, (v) => Delay = v);
            FunctionElement remainingElement = null;
            RepeatingPage.CreateBool("Swap on start", Color.red, SwapOnStart, (v) => SwapOnStart = v);
            var _switchEvery = RepeatingPage.CreateToggleFunction("Switch to random avatar every set seconds", Color.white, null);
            _switchEvery.Started += () =>
            {
                if (SwitchEvery)
                    return;
                if (Delay < RepeatingMinimumDelay)
                {
                    Core.Logger.Error("Cannot use repeated avatar change, set delay is below minimum");
                    _switchEvery.Cancel();
                    return;
                }
                Remaining = Delay;
                if (SwapOnStart) Core.SwapToRandom();
                _lastDelay = Delay;
                SwitchEvery = true;
            };
            _switchEvery.Cancelled += () => SwitchEvery = false;
            if (SwitchEvery) _switchEvery.Start();
            remainingElement = RepeatingPage.CreateFunction("Time until switch: N/A", Color.white, null);
            remainingElement.SetProperty(ElementProperties.NoBorder);
            RemainingLabel = remainingElement;
        }

        internal static void UpdateLabels()
        {
            if (IsAllowedLabel == null)
                return;

            if (Fusion.IsConnected())
            {
                IsAllowedLabel.ElementName = $"Is currently allowed: {(Fusion.IsAllowed() ? "<color=#2EFF2E>Yes</color>" : "<color=#FF0000>No</color>")}";
                FusionDelayLabel.ElementName = $"Minimum Delay: <color=#00FFFF>{MinimumDelay}{(MinimumDelay != RepeatingMinimumDelay ? $" (Repeated {RepeatingMinimumDelay})" : "")}</color>";
            }
            else
            {
                IsAllowedLabel.ElementName = string.Empty;
                FusionDelayLabel.ElementName = string.Empty;
            }
        }

        internal static void CheckDelay()
        {
            if (DelayElement != null && Delay < RepeatingMinimumDelay)
            {
                Delay = RepeatingMinimumDelay;
                DelayElement.Value = Delay;
            }
        }

        internal static void SetupFusionPage()
        {
            if (FusionPage == null || !IsSetup)
                return;

            FusionPage.RemoveAll();

            FusionPage.CreateBoolPref("Allow Random Avatar", Color.cyan, ref Core.Entry_AllowRandomAvatarInYourLobbies, callback: (_) => Fusion.UpdateMetadata());
            FusionPage.CreateIntPref("Delay/Cooldown", Color.yellow, ref Core.Entry_MinimumFusionDelay, 5, 0, 3600, callback: (_) => Fusion.UpdateMetadata());
            Fusion.UpdateMetadata();
            IsAllowedLabel = FusionPage.CreateLabel($"Is currently allowed: {(Fusion.IsAllowed() ? "<color=#2EFF2E>Yes</color>" : "<color=#FF0000>No</color>")}", Color.white);
            FusionDelayLabel = FusionPage.CreateLabel($"Minimum Delay/Cooldown: <color=#00FFFF>{MinimumDelay}{(MinimumDelay != RepeatingMinimumDelay ? $" (Repeated {RepeatingMinimumDelay})" : "")}</color>", Color.white);
        }

        private static float _generalElapsed;

        private static float _lastDelay;
        private static int _lastMinimumDelay;

        internal static void Update()
        {
            if (_lastMinimumDelay != RepeatingMinimumDelay)
            {
                UpdateRepeating();
                _lastMinimumDelay = RepeatingMinimumDelay;
            }
            _generalElapsed += TimeUtilities.DeltaTime;
            if (_generalElapsed >= 1f)
            {
                _generalElapsed = 0f;
                EverySecond?.Invoke();
            }

            if (RemainingLabel != null)
            {
                if (SwitchEvery)
                {
                    if (Fusion.IsAllowed())
                    {
                        if (_lastDelay == Delay)
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
                        }
                        else
                        {
                            _lastDelay = Delay;
                            Remaining = Delay;
                            elapsed = 0f;
                        }
                    }
                    else
                    {
                        _lastDelay = 0f;
                        Remaining = 0;
                        elapsed = 0f;
                        SwitchEvery = false;
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