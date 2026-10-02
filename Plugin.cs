global using BepInEx;
global using Freedom_Planet_2_Tails_Mod.Patchers;
global using static Freedom_Planet_2_Tails_Mod.Plugin;
global using HarmonyLib;
global using UnityEngine;
using BepInEx.Logging;
using FP2Lib.Stage;
using System.IO;

namespace Freedom_Planet_2_Tails_Mod
{
    /* TODOs for potential updates:
    TODO: Finish the Emerald Hill based tutorial, just needs Omochao's text proofread and maybe touched up. - Mandatory for initial release.
    TODO: Check for any glaring issues in Emerald Hill's collision and patch them up. - Mandatory for initial release.
    TODO: Actual README for the GitHub repo.

    TODO: Improve Super Tails, try give him the Flickies. - Medium Priority
    TODO: Actual corkscrew set up for Emerald Hill rather than just using scripted tubes. - Medium Priority
    TODO: Make the displays in the Guard Trial show Tails. - Low Priority
    */
    [BepInPlugin("K24_FP2_Tails", "Miles \"Tails\" Prower", "1.0.0")]
    [BepInDependency("000.kuborro.libraries.fp2.fp2lib")]
    public class Plugin : BaseUnityPlugin
    {
        // The asset bundles exported from the Unity project.
        public static AssetBundle tailsAssetBundle;
        public static AssetBundle tailsSceneBundle;

        // The music and jingles.
        public static AudioClip tailsClearJingle;
        public static AudioClip tailsResultsMusic;
        public static AudioClip tailsThemeMusic;
        public static AudioClip tailsJetAnkletJingle;
        public static AudioClip tailsSuperMusic;
        public static AudioClip tailsEHZMapMusic;
        public static AudioClip tailsEHZMusic;
        public static AudioClip tailsEHZClearJingle;

        // The last audio that FPAudio played in its PlayMusic function.
        public static AudioClip lastUsedAudio;

        // The playable character created through FP2Lib.
        public static FP2Lib.Player.PlayableChara playerTails;
        internal static FPCharacterID tailsCharacterID;

        // A copy of Lilac's prefab for the Super sound.
        public static GameObject lilacPrefab;

        // Logger.
        public static ManualLogSource consoleLog;

        private void Awake()
        {
            // Set up the logger.
            consoleLog = Logger;

            // Check for the asset bundles..
            if (!File.Exists($@"{Paths.GameRootPath}\mod_overrides\tails.assets") || !File.Exists($@"{Paths.GameRootPath}\mod_overrides\tails.scene"))
            {
                consoleLog.LogError("Failed to find either the Assets or Scene files! Please ensure they are correctly located in your Freedom Planet 2's mod_overrides folder.");
                return;
            }

            // Load our asset bundles.
            tailsAssetBundle = AssetBundle.LoadFromFile($@"{Paths.GameRootPath}\mod_overrides\tails.assets");
            tailsSceneBundle = AssetBundle.LoadFromFile($@"{Paths.GameRootPath}\mod_overrides\tails.scene");

            // Load our music and jingles.
            tailsClearJingle = tailsAssetBundle.LoadAsset<AudioClip>("M_Clear_Tails");
            tailsResultsMusic = tailsAssetBundle.LoadAsset<AudioClip>("M_Results_Tails");
            tailsThemeMusic = tailsAssetBundle.LoadAsset<AudioClip>("M_Theme_Tails");
            tailsJetAnkletJingle = tailsAssetBundle.LoadAsset<AudioClip>("M_Jingle_JetAnklet");
            tailsSuperMusic = tailsAssetBundle.LoadAsset<AudioClip>("M_SuperTails");
            tailsEHZMapMusic = tailsAssetBundle.LoadAsset<AudioClip>("M_Map_EmeraldHill");
            tailsEHZMusic = tailsAssetBundle.LoadAsset<AudioClip>("M_Stage_EmeraldHillZone");
            tailsEHZClearJingle = tailsAssetBundle.LoadAsset<AudioClip>("M_Clear_Classic");

            // Print all the asset names from the asset bundle as debug writes.
            foreach (string assetName in tailsAssetBundle.GetAllAssetNames())
                consoleLog.LogDebug(assetName);

            // Load Tails' Adventure Mode map sprites, rather than loading the asset nine times.
            Sprite[] worldMapSprites = tailsAssetBundle.LoadAssetWithSubAssets<Sprite>("tails_worldmap");

            // Construct Tails' player object.
            playerTails = new()
            {
                uid = "k24.tails",
                Name = "Tails",
                characterType = "FLIGHT Type",
                skill1 = "Tail Swipe",
                skill2 = "Fly",
                skill3 = "Wrench Toss",
                skill4 = "Guard",
                powerupStartDescription = "You begin the stage with a Jet Anklet.",
                AirMoves = FPPlayerPatcher.Action_Tails_AirMoves,
                GroundMoves = FPPlayerPatcher.Action_Tails_GroundMoves,
                ItemFuelPickup = FPPlayerPatcher.Action_Tails_Fuel,
                Gender = FP2Lib.Player.CharacterGender.MALE,
                element = FP2Lib.Player.CharacterElement.WOOD,
                profilePic = tailsAssetBundle.LoadAsset<Sprite>("tails_file_icon"),
                keyArtSprite = tailsAssetBundle.LoadAsset<Sprite>("tails_character_select"),
                endingKeyArtSprite = tailsAssetBundle.LoadAsset<Sprite>("tails_character_select"),
                charSelectName = tailsAssetBundle.LoadAsset<Sprite>("tails_character_name"),
                prefab = tailsAssetBundle.LoadAsset<GameObject>("player tails"),
                characterSelectPrefab = tailsAssetBundle.LoadAsset<GameObject>("select tails"),
                dataBundle = tailsAssetBundle,
                itemFuel = tailsAssetBundle.LoadAsset<Sprite>("tails_fuel"),
                livesIconAnim = [tailsAssetBundle.LoadAsset<Sprite>("tails_life_icon_0"), tailsAssetBundle.LoadAsset<Sprite>("tails_life_icon_2"), tailsAssetBundle.LoadAsset<Sprite>("tails_life_icon_1")],
                resultsTrack = tailsResultsMusic,
                menuPhotoPose = new()
                { 
                    groundSprites = [tailsAssetBundle.LoadAsset<Sprite>("tails_photo_ground0"), tailsAssetBundle.LoadAsset<Sprite>("tails_photo_ground1"), tailsAssetBundle.LoadAsset<Sprite>("tails_photo_ground2"), tailsAssetBundle.LoadAsset<Sprite>("tails_photo_ground3"), tailsAssetBundle.LoadAssetWithSubAssets<Sprite>("tails_item")[1], tailsAssetBundle.LoadAssetWithSubAssets<Sprite>("tails_idle1")[9]],
                    airSprites = [tailsAssetBundle.LoadAsset<Sprite>("tails_photo_air0"), tailsAssetBundle.LoadAsset<Sprite>("tails_photo_air1"), tailsAssetBundle.LoadAsset<Sprite>("tails_photo_air2")]
                },
                endingTrack = tailsThemeMusic,
                airshipSprite = 0,
                enabledInAventure = false,
                enabledInClassic = true,
                disableSwimming = true,
                eventActivatorCharacter = FPCharacterID.MILLA,
                EventSequenceStart = null,
                piedHurtSprite = tailsAssetBundle.LoadAsset<Sprite>("tails_pie_struggle"),
                piedSprite = tailsAssetBundle.LoadAsset<Sprite>("tails_pie"),
                sagaBlock = tailsAssetBundle.LoadAsset<RuntimeAnimatorController>("Saga Animator Tails"),
                sagaBlockSyntax = tailsAssetBundle.LoadAsset<RuntimeAnimatorController>("Syntax Saga Animator Tails"),
                TutorialScene = "EmeraldHillTutorial",
                useOwnCutsceneActivators = false,
                worldMapIdle = [worldMapSprites[0]],
                worldMapPauseSprite = tailsAssetBundle.LoadAsset<Sprite>("tails_menu"),
                worldMapWalk = [worldMapSprites[1], worldMapSprites[2], worldMapSprites[3], worldMapSprites[4], worldMapSprites[5], worldMapSprites[6], worldMapSprites[7], worldMapSprites[8]],
                zaoBaseballSprite = tailsAssetBundle.LoadAsset<Sprite>("tails_baseball"),
                menuInstructionPrefab = tailsAssetBundle.LoadAsset<GameObject>("guide tails"),
                bfImpaleSprite = tailsAssetBundle.LoadAsset<Sprite>("tails_impaled"),
                statDefaultTopSpeed = 10f
            };

            // Register Tails' player object with FP2Lib's player handler.
            FP2Lib.Player.PlayerHandler.RegisterPlayableCharacterDirect(playerTails);

            // Get the ID that FP2Lib assigned to Tails.
            tailsCharacterID = (FPCharacterID)FP2Lib.Player.PlayerHandler.GetPlayableCharaByUid(playerTails.uid).id;

            // Register Tails' Vinyls.
            FP2Lib.Vinyl.VinylHandler.RegisterVinyl("k24.vinyl_tails_clear", "Stage Clear - Tails", tailsClearJingle, FP2Lib.Vinyl.VAddToShop.All, 1);
            FP2Lib.Vinyl.VinylHandler.RegisterVinyl("k24.vinyl_tails_results", "Results - Tails", tailsResultsMusic, FP2Lib.Vinyl.VAddToShop.All, 1);
            FP2Lib.Vinyl.VinylHandler.RegisterVinyl("k24.vinyl_tails_jetanklet", "Jet Anklet", tailsJetAnkletJingle, FP2Lib.Vinyl.VAddToShop.All, 1);
            FP2Lib.Vinyl.VinylHandler.RegisterVinyl("k24.vinyl_tails_credits", "Believe in Myself (Tails' Theme)", tailsThemeMusic, FP2Lib.Vinyl.VAddToShop.All, 31);
            FP2Lib.Vinyl.VinylHandler.RegisterVinyl("k24.vinyl_tails_ehzmap", "Map - Emerald Hill", tailsEHZMapMusic, FP2Lib.Vinyl.VAddToShop.All, 32);
            FP2Lib.Vinyl.VinylHandler.RegisterVinyl("k24.vinyl_tails_emeraldhill", "Emerald Hill Zone", tailsEHZMusic, FP2Lib.Vinyl.VAddToShop.All, 32);
            FP2Lib.Vinyl.VinylHandler.RegisterVinyl("k24.vinyl_tails_emeraldhillclear", "Stage Clear - Emerald Hill", tailsEHZClearJingle, FP2Lib.Vinyl.VAddToShop.All, 32);
            FP2Lib.Vinyl.VinylHandler.RegisterVinyl("k24.vinyl_tails_superform", "Super Tails", tailsSuperMusic, FP2Lib.Vinyl.VAddToShop.All, 32);

            // Register Tails' Badges.
            FP2Lib.Badge.BadgeHandler.RegisterBadge("k24.badge_tails_clear", "Fly High", "Clear the game as Tails.", tailsAssetBundle.LoadAsset<Sprite>("tails_badge_clear"), FP2Lib.Badge.FPBadgeType.GOLD, FP2Lib.Badge.FPBadgeVisible.ALWAYS);
            FP2Lib.Badge.BadgeHandler.RegisterBadge("k24.badge_tails_partime", "Westside Speedster", "Beat any stage's par time as Tails.", tailsAssetBundle.LoadAsset<Sprite>("tails_badge_partime"), FP2Lib.Badge.FPBadgeType.SILVER, FP2Lib.Badge.FPBadgeVisible.ALWAYS);
            FP2Lib.Badge.BadgeHandler.RegisterBadge("k24.badge_tails_halfpartime", "92 Miles Per Hour", "Beat any stage as Tails in less than half of the par time.", tailsAssetBundle.LoadAsset<Sprite>("tails_badge_halfpartime"), FP2Lib.Badge.FPBadgeType.SILVER, FP2Lib.Badge.FPBadgeVisible.ALWAYS);
            FP2Lib.Badge.BadgeHandler.RegisterBadge("k24.badge_tails_allpartime", "Twin Tail Turbo", "Beat the par times in all stages as Tails.", tailsAssetBundle.LoadAsset<Sprite>("tails_badge_allpartime"), FP2Lib.Badge.FPBadgeType.GOLD, FP2Lib.Badge.FPBadgeVisible.ALWAYS);
            FP2Lib.Badge.BadgeHandler.RegisterBadge("k24.badge_tails_emeraldhill", "Home Sweet Home", "Unlock and complete Emerald Hill Zone.", tailsAssetBundle.LoadAsset<Sprite>("tails_badge_emeraldhill"), FP2Lib.Badge.FPBadgeType.GOLD, FP2Lib.Badge.FPBadgeVisible.HIDDEN);

            // Find and store Lilac's prefab.
            foreach (GameObject obj in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>())
                if (obj.name is "Player Lilac") lilacPrefab = obj;

            // Define and register Emerald Hill.
            CustomStage emeraldHill = new()
            {
                uid = "K24_Tails_EmeraldHill",
                name = "Emerald Hill Zone",
                description = "A recreation of the Emerald Hill Zone Act 1 from Sonic 2, created as an Easter Egg unlockable in the same way that Green Hill Zone Act 1 is for Sonic. This description shouldn't be viewable in game, if it is, then I've fucked something up again or a loader isn't respecting the option to hide it.",
                author = "Knuxfan24",
                version = "1.0.0",
                parTime = 4500,
                sceneName = "EmeraldHill",
                preview = tailsAssetBundle.LoadAsset<Sprite>("tails_ehz_tile"),
                showInCustomStageLoaders = false
            };
            FP2Lib.Stage.StageHandler.RegisterStage(emeraldHill);

            // Patch our classes.
            Harmony.CreateAndPatchAll(typeof(FPAudioPatcher));
            Harmony.CreateAndPatchAll(typeof(FPCheckpointPatcher));
            Harmony.CreateAndPatchAll(typeof(FPEventSequencePatcher));
            Harmony.CreateAndPatchAll(typeof(FPHudMasterPatcher));
            Harmony.CreateAndPatchAll(typeof(FPPlayerPatcher));
            Harmony.CreateAndPatchAll(typeof(FPResultsMenuPatcher));
            Harmony.CreateAndPatchAll(typeof(FPSaveManagerPatcher));
            Harmony.CreateAndPatchAll(typeof(FPStagePatcher));
            Harmony.CreateAndPatchAll(typeof(MenuClassicPatcher));
            Harmony.CreateAndPatchAll(typeof(MenuCreditsPatcher));
            Harmony.CreateAndPatchAll(typeof(PlayerSpawnPointPatcher));
            Harmony.CreateAndPatchAll(typeof(RestoreTailsEnergy));
        }
    }
}
