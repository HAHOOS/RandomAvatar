using RandomAvatar.Utilities;

namespace RandomAvatar.Patches
{
    public static class FusionPatches
    {
        public static void Postfix()
        {
            if (!Fusion.IsConnected())
                return;

            Core.LevelLoaded();
        }
    }
}