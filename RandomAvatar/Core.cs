using Il2CppSystem.Collections.Generic;
using System.IO;

using BoneLib;

using Il2CppSLZ.Bonelab;

using MelonLoader;
using MelonLoader.Utils;

using RandomAvatar.Helper;

using UnityEngine;

using Page = BoneLib.BoneMenu.Page;
using Il2CppSLZ.Marrow.Warehouse;
using System.Threading.Tasks;
using RandomAvatar.Patches;

namespace RandomAvatar
{
    public class Core : MelonMod
    {
        public const string Version = "1.0.0";

        internal static MelonPreferences_Category Preferences_Category { get; private set; }

        internal static MelonPreferences_Entry<bool> Entry_UseRedacted, Entry_EffectWhenSwitching;

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
                var obj = new GameObject("RandomAvatar");
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

        public static bool SwapOnDeath { get; private set; } = false;

        private void PatchLocalRagdoll()
        {
            HarmonyInstance.Patch(
                typeof(LabFusion.Player.LocalRagdoll).GetMethod(nameof(LabFusion.Player.LocalRagdoll.Knockout)),
                postfix: new HarmonyLib.HarmonyMethod(typeof(LocalRagdollPatches).GetMethod(nameof(LocalRagdollPatches.Postfix))));
        }

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Setting up preferences");
            Preferences_Category = MelonPreferences.CreateCategory("RandomAvatar");
            Entry_UseRedacted = Preferences_Category.CreateEntry<bool>("UseRedacted", false, "Use Redacted",
                description: "If true, when switching to random avatar the mod will also include redacted avatars");
            Entry_EffectWhenSwitching = Preferences_Category.CreateEntry<bool>("EffectWhenSwitching", true, "Effect When Switching",
                description: "If true, when switching to random avatar there will be an effect with the Pull Cord Device like when going into an area in a level that changes your avatar");

            Preferences_Category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "RandomAvatar.cfg"));
            Preferences_Category.SaveToFile(false);

            LoggerInstance.Msg("Creating BoneMenu");

            if (HelperMethods.CheckIfAssemblyLoaded("labfusion"))
            {
                PatchLocalRagdoll();
            }

            Page authorPage = Page.Root.CreatePage("HAHOOS", Color.white);
            Page modPage = authorPage.CreatePage("RandomAvatar", Color.magenta);
            modPage.CreateBoolPref("Use Redacted Avatars", Color.cyan, ref Entry_UseRedacted);
            modPage.CreateBoolPref("Add effect when switching avatar", Color.magenta, ref Entry_EffectWhenSwitching);
            modPage.CreateFunction("Switch to random avatar", Color.white, SwapToRandom);

            int delay = 10;
            modPage.CreateBlank();
            modPage.CreateInt("Change Delay", Color.yellow, 30, 10, 10, 3600, (v) => delay = v);
            modPage.CreateToggleFunction("Switch to random avatar every set seconds", Color.white, async (element) =>
            {
                while (element.IsRunning)
                {
                    SwapToRandom();
                    await Task.Delay(delay * 1000);
                }
            });

            modPage.CreateBlank();
            modPage.CreateBool("Switch to random avatar on death", Color.cyan, false, (v) => SwapOnDeath = v);

            LoggerInstance.Msg("Initialized.");
        }
    }
}