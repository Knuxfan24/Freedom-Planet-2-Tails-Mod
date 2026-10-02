namespace Freedom_Planet_2_Tails_Mod.Patchers
{
    internal class FPCheckpointPatcher
    {
        /// <summary>
        /// Sets the Starpost (and any earlier ones) to the Active state when reloading from it.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPCheckpoint), "Start")]
        private static void ActivateStarpostOnReload(FPCheckpoint __instance)
        {
            // Make sure this Checkpoint has child objects to begin with.
            if (__instance.transform.childCount == 0)
                return;

            // Get this checkpoint's child object.
            Transform starpost = __instance.transform.GetChild(0);

            // Check that the child object exists, the active checkpoint's x position is greater than or equal to it and that the child object is actually called Starpost.
            // If these pass, then set the animator to play the Active animation.
            if (starpost != null && FPStage.checkpointPos.x >= __instance.transform.position.x)
                if (starpost.name == "StarPost")
                    starpost.GetComponent<Animator>().Play("Active");
        }

        /// <summary>
        /// Sets the Starpost to the Spin state when passing it.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPCheckpoint), "Set_Checkpoint")]
        private static void ActivateStarpost(FPCheckpoint __instance)
        {
            // Make sure this Checkpoint has child objects to begin with.
            if (__instance.transform.childCount == 0)
                return;

            // Get this checkpoint's child object.
            Transform starpost = __instance.transform.GetChild(0);

            // Check that the child object exists and is actually called Starpost. If either of these checks fail, then abort.
            if (starpost == null)
                return;
            if (starpost.name != "StarPost")
                return;

            // Play the Spin animation.
            starpost.GetComponent<Animator>().Play("Spin");

            // Play the Starpost sound from the asset bundle.
            FPAudio.PlaySfx(tailsAssetBundle.LoadAsset<AudioClip>("classic_starpost"));
        }

        /// <summary>
        /// Sets any earlier Starposts to their Active state when passing one.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPCheckpoint), "Update")]
        private static void ActivateEarlierStarposts(FPCheckpoint __instance)
        {
            // Make sure this Checkpoint has child objects to begin with.
            if (__instance.transform.childCount == 0)
                return;

            // Get this checkpoint's child object.
            Transform starpost = __instance.transform.GetChild(0);

            // Check that the child object exists, the active checkpoint's x position is greater than it and that the child object is actually called Starpost.
            // If these pass, then check the animation of the Starpost and set it to the Active state if its currently in its Idle animation.
            if (starpost != null && FPStage.checkpointPos.x > __instance.transform.position.x)
                if (starpost.name == "StarPost")
                    if (starpost.GetComponent<Animator>().GetCurrentAnimatorClipInfo(0)[0].clip.name == "StarpostIdle")
                        starpost.GetComponent<Animator>().Play("Active");
        }
    }
}

