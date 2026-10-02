using UnityEngine.SceneManagement;

namespace Freedom_Planet_2_Tails_Mod.Patchers
{
    internal class FPEventSequencePatcher
    {
        // The event activator for the Kalaw intro never addresses Milla's object, so the other replacement doesn't work on it.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPStage), "Start")]
        static void ReplaceKalawIntro()
        {
            // Only do this if we're Tails and in the Kalaw intro cutscene.
            if (FPSaveManager.character != tailsCharacterID || SceneManager.GetActiveScene().name != "Battlesphere_Kalaw")
                return;

            // Find Milla's cutscene object.
            GameObject milla = GameObject.Find("Cutscene_Milla");

            // Swap out the animator and kill the tail object.
            milla.GetComponent<Animator>().runtimeAnimatorController = tailsAssetBundle.LoadAsset<RuntimeAnimatorController>("Tails Event Animator");
            milla.transform.GetChild(0).gameObject.SetActive(false);
        }

        /// <summary>
        /// Replaces the animator on Milla's cutscene objects if we're playing as Tails.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FPEventSequence), "Action_StartEvent")]
        static void LilacEventReplacer(ref FPDialogEvent e)
        {
            // Don't do this if we're not Tails and not in the Ending.
            if (FPSaveManager.character != tailsCharacterID || SceneManager.GetActiveScene().name == "Cutscene_Ending2" || SceneManager.GetActiveScene().name == "Cutscene_Ending3")
                return;

            // Check that this event targets an object.
            if (e.targetObject != null)
            {
                // Get the target's animator and check that it actually has one.
                Animator animator = e.targetObject.gameObject.GetComponent<Animator>();
                if (animator != null)
                {
                    // Check if this animator is Milla's event one.
                    if (animator.runtimeAnimatorController.name == "Milla")
                    {
                        // Kill the object for Milla's tail.
                        if (e.targetObject.transform.GetChild(0).name == "tail")
                            e.targetObject.transform.GetChild(0).gameObject.SetActive(false);

                        // Swap to Sonic's animator depending on if we're Super or not.
                        animator.runtimeAnimatorController = tailsAssetBundle.LoadAsset<RuntimeAnimatorController>("Tails Event Animator");
                    }

                    // DEBUG: Log the object, pose and move pose.
                    if (animator.runtimeAnimatorController.name == "Tails Event Animator")
                        consoleLog.LogDebug($"Object: {e.targetObject.name}\r\n\tPose: {e.pose}\r\n\tMove Pose: {e.movePose}");
                }
            }
        }
    }
}