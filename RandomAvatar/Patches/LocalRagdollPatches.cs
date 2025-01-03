namespace RandomAvatar.Patches
{
    public static class LocalRagdollPatches
    {
        public static void Postfix()
        {
            if (!LabFusion.Data.RigData.HasPlayer)
                return;

            if (Core.SwapOnDeath)
                Core.SwapToRandom();
        }
    }
}