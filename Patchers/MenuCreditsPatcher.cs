namespace Freedom_Planet_2_Tails_Mod.Patchers
{
    internal class MenuCreditsPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuCredits), "Start")]
        static void EditCredits(ref AudioClip[] ___castVoice, ref float ___normalSpeed)
        {
            // If we're not Tails, then don't make the edits.
            if (FPSaveManager.character != tailsCharacterID)
                return;

            // Move the portrait down a bit. 
            var portraitPosition = GameObject.Find("Credits").transform.GetChild(19);
            portraitPosition.localPosition = new(portraitPosition.localPosition.x, 0, portraitPosition.localPosition.z);

            // Find and replace the text for Milla with Tails. Ideally I'd add him into the cast list, but that sounds painful to do.
            GameObject.Find("CharacterName (3)").GetComponent<TextMesh>().text = "Miles \"Tails\"\r\nPrower";

            // Replace Milla's cutscene animator with the custom one for Tails.
            GameObject.Find("Cutscene_Milla").GetComponent<Animator>().runtimeAnimatorController = tailsAssetBundle.LoadAsset<RuntimeAnimatorController>("Tails Credits Animator");

            // Hide Milla's tail.
            GameObject.Find("Cutscene_Milla").GetComponent<Animator>().transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = false;

            // Set Tails' voice actor and remove the voice line for Milla's animation.
            GameObject.Find("ActorName (3)").GetComponent<TextMesh>().text = "Amy\r\n Palant";
            ___castVoice[3] = null;
        }
    }
}
