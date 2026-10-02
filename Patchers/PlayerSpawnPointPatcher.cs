using UnityEngine.SceneManagement;

namespace Freedom_Planet_2_Tails_Mod.Patchers
{
    internal class PlayerSpawnPointPatcher
    {
        /// <summary>
        /// Moves the spawn point in Gravity Bubble to the ground so Tails doesn't fall down in his standing pose.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerSpawnPoint), "Start")]
        private static void AdjustGravityBubbleSpawn(PlayerSpawnPoint __instance)
        {
            if (FPSaveManager.character != tailsCharacterID || SceneManager.GetActiveScene().name != "GravityBubble")
                return;

            __instance.transform.position = new(128, -564, 0);
            __instance.powerupOffset = new(182, -16);
        }

    }
}
