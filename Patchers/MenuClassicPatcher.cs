using FP2Lib.Stage;
using System;

namespace Freedom_Planet_2_Tails_Mod.Patchers
{
    internal class MenuClassicPatcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuClassic), "Start", MethodType.Normal)]
        static void CreateEmeraldHillTile(MenuClassic __instance)
        {
            // Only add Emerald Hill if the player is Tails.
            // Using something like the World Map Character Changer invalidates the stage ID and boots it back to Dragon Valley so that's not a problem.
            if (FPSaveManager.character == tailsCharacterID)
            {
                // Instantiate the background object from the asset bundle.
                GameObject mapBackground = GameObject.Instantiate(tailsAssetBundle.LoadAsset<GameObject>("Emerald Hill Map Background"));

                // Instantiate the Emerald Hill panel from the asset bundle.
                GameObject mapPanel = GameObject.Instantiate(tailsAssetBundle.LoadAsset<GameObject>("StageIcon_EmeraldHill"));

                // Create a tile entry for Emerald Hill.
                MenuClassicTile levelSelectTile = new MenuClassicTile
                {
                    // Set the BGM and Background for Emerald Hill's tile.
                    bgm = tailsEHZMapMusic,
                    background = mapBackground.transform,

                    // Tell the game that Emerald Hill doesn't have a Vinyl.
                    hudVinylID = -1,

                    // Attach the stage to Weapon's Core.
                    left = 30,
                    right = -1,
                    down = -1,
                    up = -1,
                    stageRequirement = [30],

                    // Get the icon from the panel.
                    icon = mapPanel.GetComponent<SpriteRenderer>(),

                    // Get Emerald Hill's ID.
                    stageID = StageHandler.getCustomStageByUid("K24_Tails_EmeraldHill").id
                };

                // Add Emerald Hill to the stage list.
                __instance.stages = __instance.stages.AddToArray(levelSelectTile);

                // Attach Emerald Hill to Weapon's Core's entry.
                __instance.stages[30].right = Array.IndexOf(__instance.stages, levelSelectTile);
            }
        }
    }
}
