namespace Freedom_Planet_2_Tails_Mod.Patchers
{
    internal class FPSaveManagerPatcher
    {
        /// <summary>
        /// Unlocks Tails' game clear badge.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FPSaveManager), "GameClearBadgeCheck")]
        private static void UnlockGameClearBadge(ref FPCharacterID ___character)
        {
            // If the player is Tails, then unlock his game clear badge.
            if (___character == tailsCharacterID)
                FP2Lib.Badge.BadgeHandler.UnlockBadge("k24.badge_tails_clear");
        }
    }
}
