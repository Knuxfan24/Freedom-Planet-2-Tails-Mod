using BepInEx.Bootstrap;
using System.Linq;

namespace Freedom_Planet_2_Tails_Mod.Patchers
{
    internal class FPHudMasterPatcher
    {
        private static readonly Sprite chaosEmeraldSprite = tailsAssetBundle.LoadAsset<Sprite>("hud_chaosemeralds");

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPHudMaster), "LateUpdate")]
        private static void HUDIcons(FPHudMaster __instance)
        {
            // Check that the player is Tails.
            if (FPPlayerPatcher.player.characterID == tailsCharacterID)
            {
                // Check if Sonic is installed and the Chaos Emerald item has been found.
                if (Chainloader.PluginInfos.ContainsKey("K24_FP2_Sonic") && FPPlayerPatcher.ChaosEmeraldsItem != null)
                {
                    // Check if we have the Chaos Emeralds equipped.
                    if (FPPlayerPatcher.player.powerups.Contains((FPPowerup)FPPlayerPatcher.ChaosEmeraldsItem.itemID))
                    {
                        // Check if we meet the criteria for the Chaos Emerald icon to show on the HUD.
                        if ((!FPPlayerPatcher.isSuper && FPPlayerPatcher.player.totalCrystals >= 50) || FPPlayerPatcher.isSuper)
                        {
                            __instance.hudBike[0].GetRenderer().enabled = true;
                            __instance.hudBike[0].GetComponent<SpriteRenderer>().sprite = chaosEmeraldSprite;
                        }
                    }
                }

                // Check that the Jet Anklet is active.
                if (FPPlayerPatcher.player.powerupTimer > 0)
                {
                    // Replace Carol's bike icon with the Jet Anklet one. We do this here so the Sonic mod's Chaos Emeralds don't take priority.
                    __instance.hudBike[0].GetComponent<SpriteRenderer>().sprite = tailsAssetBundle.LoadAsset<Sprite>("tails_fuel_hud");

                    // Blink every four frames if we have between 60 and 120 frames left on the Jet Anklet.
                    if (FPPlayerPatcher.player.powerupTimer <= 120 && FPPlayerPatcher.player.powerupTimer > 60)
                    {
                        if (Mathf.Floor(FPPlayerPatcher.player.powerupTimer) % 8 == 0)
                            __instance.hudBike[0].GetRenderer().enabled = false;
                        else if (Mathf.Floor(FPPlayerPatcher.player.powerupTimer) % 4 == 0)
                            __instance.hudBike[0].GetRenderer().enabled = true;
                    }

                    // Blink every two frames if we have less than 60 frames left on the Jet Anklet.
                    else if (FPPlayerPatcher.player.powerupTimer <= 60)
                    {
                        if (Mathf.Floor(FPPlayerPatcher.player.powerupTimer) % 4 == 0)
                            __instance.hudBike[0].GetRenderer().enabled = false;
                        else if (Mathf.Floor(FPPlayerPatcher.player.powerupTimer) % 2 == 0)
                            __instance.hudBike[0].GetRenderer().enabled = true;
                    }

                    // Show the icon if we have the Jet Anklet and more than 120 frames left on it.
                    else
                        __instance.hudBike[0].GetRenderer().enabled = true;
                }
            }
        }
    }
}
