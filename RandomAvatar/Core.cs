using Il2CppSystem.Collections.Generic;
using System.IO;

using BoneLib;

using Il2CppSLZ.Bonelab;

using MelonLoader;
using MelonLoader.Utils;

using RandomAvatar.Helper;

using UnityEngine;

using Il2CppSLZ.Marrow.Warehouse;
using RandomAvatar.Patches;
using System;
using RandomAvatar.Utilities;
using Semver;
using RandomAvatar.Menu;

namespace RandomAvatar
{
    public class Core : MelonMod
    {
        public const string Version = "1.1.0";

        internal static MelonPreferences_Category Preferences_Category { get; private set; }

        internal static MelonPreferences_Entry<bool> Entry_UseRedacted, Entry_EffectWhenSwitching;

        internal static MelonLogger.Instance Logger { get; private set; }

        internal static LevelInfo LevelInfo { get; private set; }

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
                Fusion.SendPullCordEffect();
                comp.ForceChange(comp.rigManager.gameObject);
            }
            else
            {
                Player.RigManager.SwapAvatarCrate(new Barcode(barcode), true);
            }
        }

        public static Barcode RandomAvatar(List<AvatarCrate> avatars = null)
        {
            var r = new System.Random();
            avatars ??= AssetWarehouse.Instance.GetCrates<AvatarCrate>();
            var avatar = avatars[r.Next(avatars.Count)];
            if (avatar == null) return RandomAvatar(avatars);
            if (!Entry_UseRedacted.Value && avatar.Redacted) return RandomAvatar(avatars);
            if (!BlacklistWhitelist.IsAvatarAllowed(avatar)) return RandomAvatar(avatars);
            return avatar.Barcode;
        }

        public static void SwapToRandom()
        {
            if (!AssetWarehouse.ready)
            {
                BLHelper.SendNotification("Failure", "Asset Warehouse is not ready and therefore the avatar cannot be switched", true, 2f, BoneLib.Notifications.NotificationType.Error);
                return;
            }

            var avatar = RandomAvatar();
            SwapAvatar(avatar.ID, Entry_EffectWhenSwitching.Value);
        }

        public static bool SwapOnDeath { get; internal set; } = false;

        public static bool SwapOnLevelChange { get; internal set; } = false;

        // Used if the level reloads on death and the effect is on
        public static bool SwapOnNextLevelChange { get; internal set; } = false;

        private void PatchLocalRagdoll()
        {
            HarmonyInstance.Patch(
                typeof(LabFusion.Player.LocalRagdoll).GetMethod(nameof(LabFusion.Player.LocalRagdoll.Knockout)),
                postfix: new HarmonyLib.HarmonyMethod(typeof(LocalRagdollPatches).GetMethod(nameof(LocalRagdollPatches.Postfix))));
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
                            LoggerInstance.Msg(System.Drawing.Color.Aqua, $"A new version of RandomAvatar is available: v{ThunderstorePackage.Latest.Version} while the current is v{Version}");
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

            Preferences_Category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "RandomAvatar.cfg"));
            Preferences_Category.SaveToFile(false);

            if (HelperMethods.CheckIfAssemblyLoaded("labfusion"))
            {
                PatchLocalRagdoll();
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