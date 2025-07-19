using System;
using System.Collections.Generic;
using System.IO;

using BoneLib;

using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow.Warehouse;

using MelonLoader;
using MelonLoader.Utils;

using RandomAvatar.Helper;
using RandomAvatar.Menu;
using RandomAvatar.Patches;
using RandomAvatar.Utilities;

using Semver;

using UnityEngine;

namespace RandomAvatar
{
    public class Core : MelonMod
    {
        public const string Version = "1.2.2";

        internal static MelonPreferences_Category Preferences_Category { get; private set; }

        internal static MelonPreferences_Entry<bool> Entry_UseRedacted, Entry_EffectWhenSwitching, Entry_AllowRandomAvatarInYourLobbies;
        internal static MelonPreferences_Entry<int> Entry_MinimumFusionDelay;

        internal static MelonLogger.Instance Logger { get; private set; }

        internal static LevelInfo LevelInfo { get; private set; }

        internal static Dictionary<DateTimeOffset, AvatarCrate> _avatarHistory = [];

        public static void SwapAvatar(string barcode, bool usePCFC)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return;

            if (!AssetWarehouse.Instance.HasCrate(new Barcode(barcode)))
                return;

            if (Player.RigManager == null)
                return;

            if (usePCFC)
            {
                var obj = GameObject.Find("RandomAvatar") ?? new GameObject("RandomAvatar");
                var comp = obj.AddComponent<PullCordForceChange>();
                comp.avatarCrate = new AvatarCrateReference(barcode);
                comp.rigManager = Player.RigManager;
                comp.ForceChange(comp.rigManager.gameObject);
            }
            else
            {
                Player.RigManager.SwapAvatarCrate(new Barcode(barcode), true);
            }
        }

        public static Barcode RandomAvatar()
        {
            if (BlacklistWhitelist.Enabled.Value && ((BlacklistWhitelist.TagsList.Value == null || BlacklistWhitelist.TagsList.Value.Count == 0) && (BlacklistWhitelist.AvatarList.Value == null || BlacklistWhitelist.AvatarList.Value.Count == 0) && (BlacklistWhitelist.PalletList.Value == null || BlacklistWhitelist.PalletList.Value.Count == 0)))
            {
                BLHelper.SendNotification("Failure", "When having blacklist/whitelist enabled, you need to have at least one tag, avatar and/or pallet selected!", true, 3f, BoneLib.Notifications.NotificationType.Error);
                Logger.Error("Cannot swap to random avatar. When having blacklist/whitelist enabled, you need to have at least one tag, avatar and/or pallet selected!");
                return null;
            }

            var r = new System.Random();
            var avatars = AssetWarehouse.Instance.GetCrates<AvatarCrate>();
            avatars.RemoveAll((Il2CppSystem.Predicate<AvatarCrate>)(x =>
            {
                if (x == null) return true;
                if (!Entry_UseRedacted.Value && x.Redacted) return true;
                if (!BlacklistWhitelist.IsAvatarAllowed(x)) return true;
                return false;
            }));
            var avatar = avatars[r.Next(avatars.Count)];
            _avatarHistory.Add(DateTimeOffset.Now, avatar);
            BoneMenu.SetupHistoryPage();
            return avatar.Barcode;
        }

        public static void SwapToRandom()
        {
            if (!AssetWarehouse.ready)
            {
                BLHelper.SendNotification("Failure", "Asset Warehouse is not ready and therefore the avatar cannot be switched", true, 2f, BoneLib.Notifications.NotificationType.Error);
                return;
            }

            if (!Fusion.IsAllowed())
            {
                BLHelper.SendNotification("Disallowed", "The host of this server has disabled the usage of RandomAvatar, cannot change you into a random avatar", true, 3.5f, BoneLib.Notifications.NotificationType.Error);
                return;
            }

            // It is 1.5 instead of 0 to avoid issues with the repeating change being incorrectly hit by the cooldown
            if (BoneMenu.Cooldown > 1.5)
            {
                BLHelper.SendNotification("Cooldown", $"Wait {BoneMenu.Cooldown} more seconds", true, 1.5f, BoneLib.Notifications.NotificationType.Warning);
                return;
            }

            var avatar = RandomAvatar();

            if (avatar == null)
                return;

            if (Fusion.IsConnected())
                BoneMenu.Cooldown = BoneMenu.MinimumDelay;

            SwapAvatar(avatar.ID, Entry_EffectWhenSwitching.Value);
        }

        public static bool SwapOnDeath { get; internal set; } = false;

        public static bool SwapOnLevelChange { get; internal set; } = false;

        // Used if the level reloads on death and the effect is on
        public static bool SwapOnNextLevelChange { get; internal set; } = false;

        public static bool SwapOnDamaged { get; internal set; } = false;

        private void PatchFusion()
        {
            HarmonyInstance.Patch(
                typeof(LabFusion.Player.LocalRagdoll).GetMethod(nameof(LabFusion.Player.LocalRagdoll.Knockout), [typeof(float)]),
                postfix: new HarmonyLib.HarmonyMethod(typeof(LocalRagdollPatches).GetMethod(nameof(LocalRagdollPatches.Postfix))));
            HarmonyInstance.Patch(
                typeof(LabFusion.Player.LocalRagdoll).GetMethod(nameof(LabFusion.Player.LocalRagdoll.Knockout), [typeof(float), typeof(bool)]),
                postfix: new HarmonyLib.HarmonyMethod(typeof(LocalRagdollPatches).GetMethod(nameof(LocalRagdollPatches.Postfix))));
            HarmonyInstance.Patch(
                typeof(LabFusion.FusionMod).GetMethod(nameof(LabFusion.FusionMod.OnMainSceneInitializeDelayed)),
                postfix: new HarmonyLib.HarmonyMethod(typeof(FusionPatches).GetMethod(nameof(FusionPatches.Postfix))));
        }

        internal static Thunderstore Thunderstore;
        internal static Package ThunderstorePackage;
        internal static bool IsLatestVersion = false;

        public override void OnInitializeMelon()
        {
            Logger = LoggerInstance;
            try
            {
                Thunderstore = new Thunderstore($"RandomAvatar / {Version} A BONELAB Code Mod");
                ThunderstorePackage = Thunderstore.GetPackage("HAHOOS", "RandomAvatar");
                if (ThunderstorePackage != null)
                {
                    if (ThunderstorePackage.Latest != null && !string.IsNullOrWhiteSpace(ThunderstorePackage.Latest.Version))
                    {
                        IsLatestVersion = ThunderstorePackage.IsLatestVersion(Version);
                        if (!IsLatestVersion)
                        {
                            LoggerInstance.Msg(System.ConsoleColor.Cyan, $"A new version of RandomAvatar is available: v{ThunderstorePackage.Latest.Version} while the current is v{Version}");
                        }
                        else
                        {
                            if (SemVersion.Parse(Version) == ThunderstorePackage.Latest.SemVersion)
                            {
                                LoggerInstance.Msg($"Latest version of RandomAvatar is installed! --> v{Version}");
                            }
                            else
                            {
                                LoggerInstance.Msg($"Beta release of RandomAvatar is installed (v{ThunderstorePackage.Latest.Version} is newest, v{Version} is installed)");
                            }
                        }
                    }
                    else
                    {
                        LoggerInstance.Error("Latest version could not be found or the version is empty");
                    }
                }
                else
                {
                    LoggerInstance.Error("Could not find Thunderstore package for RandomAvatar");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"An unexpected error has occurred while trying to gather thunderstore package information, exception:\n{ex}");
            }

            LoggerInstance.Msg("Setting up preferences");
            Preferences_Category = MelonPreferences.CreateCategory("RandomAvatar");
            Entry_UseRedacted = Preferences_Category.CreateEntry<bool>("UseRedacted", false, "Use Redacted",
                description: "If true, when switching to random avatar the mod will also include redacted avatars");
            Entry_EffectWhenSwitching = Preferences_Category.CreateEntry<bool>("EffectWhenSwitching", true, "Effect When Switching",
                description: "If true, when switching to random avatar there will be an effect with the Pull Cord Device like when going into an area in a level that changes your avatar");
            Entry_AllowRandomAvatarInYourLobbies = Preferences_Category.CreateEntry<bool>("AllowRandomAvatarInYourLobbies", true, "Allow Random Avatar In Your Lobbies",
                description: "If true, random avatar will work for players when you create your lobby/server, if false they won't be able to use it on versions after 1.1.0");
            Entry_MinimumFusionDelay = Preferences_Category.CreateEntry<int>("MinimumFusionDelay", Fusion.FusionDelay, "Minimum Fusion Delay",
                description: "The minimum delay value (in seconds) for all players using RandomAvatar on versions after 1.1.0");

            Preferences_Category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "RandomAvatar.cfg"));
            Preferences_Category.SaveToFile(false);

            if (HelperMethods.CheckIfAssemblyLoaded("labfusion"))
            {
                LoggerInstance.Msg("Setting up Fusion");
                PatchFusion();
                Fusion.Setup();
            }
            else
            {
                LoggerInstance.Warning("Fusion was not found");
            }

            LoggerInstance.Msg("Setting up blacklist/whitelist");
            BlacklistWhitelist.Setup();

            LoggerInstance.Msg("Creating BoneMenu");

            BoneMenu.Setup();

            LoggerInstance.Msg("Adding hooks");

            Hooking.OnLevelLoaded += (info) =>
            {
                LevelInfo = info;
                if (!Fusion.IsConnected())
                    LevelLoaded();
            };

            LoggerInstance.Msg("Initialized.");
        }

        internal static void LevelLoaded()
        {
            if (SwapOnLevelChange || SwapOnNextLevelChange)
            {
                if (PlayerRefs.Instance == null)
                    return;

                SwapOnNextLevelChange = false;

                if (PlayerRefs.Instance.HasRefs)
                    SwapToRandom();
                else
                    PlayerRefs.Instance.OnRefsComplete += (Action)SwapToRandom;
            }
        }

        public override void OnFixedUpdate()
        {
            TimeUtilities.OnEarlyFixedUpdate();
        }

        public override void OnUpdate()
        {
            TimeUtilities.OnEarlyUpdate();
            BoneMenu.Update();
        }
    }
}