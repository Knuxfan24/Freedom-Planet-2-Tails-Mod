using BepInEx.Bootstrap;
using FP2Lib.Item;
using Freedom_Planet_2_Tails_Mod.CustomObjectScripts;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace Freedom_Planet_2_Tails_Mod.Patchers
{
    internal class FPPlayerPatcher
    {
        // Holds a reference to the player's object.
        public static FPPlayer player;

        // A multiplier for how strong the Spin Dash's release should be.
        private static float SpinDashMultiplier = 1f;

        // Holds references to the Apply Force functions, as they're private.
        private static readonly MethodInfo applyGround = typeof(FPPlayer).GetMethod("ApplyGroundForces", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo applyAir = typeof(FPPlayer).GetMethod("ApplyAirForces", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo applyGravity = typeof(FPPlayer).GetMethod("ApplyGravityForce", BindingFlags.NonPublic | BindingFlags.Instance);

        // Animations that should make the looping tail sound play.
        private static string[] tailSounds = ["Fly", "TopSpeed", "UpAir"];

        // Values for Super Tails.
        public static ItemData ChaosEmeraldsItem;
        private static GameObject TransformEmeralds;
        public static bool isSuper;
        private static float SuperStartTimer;
        private static float superTimeCounter;
        private static bool createdSparks;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPPlayer), "Start")]
        private static void Setup(FPPlayer __instance)
        {
            // Get the player's object.
            player = __instance;

            // Reset the Super flag.
            isSuper = false;

            // Only do any of the setup if we're Tails.
            if (player.characterID == tailsCharacterID)
            {
                // Reset the Tail Sound array to undo Emerald Hill's change.
                tailSounds = ["Fly", "TopSpeed", "UpAir"];

                // Get the ID of the Chaos Emeralds item if Sonic is installed.
                if (Chainloader.PluginInfos.ContainsKey("K24_FP2_Sonic"))
                    ChaosEmeraldsItem = FP2Lib.Item.ItemHandler.GetItemDataByUid("k24.sonic.chaosemeralds");

                // Replace the stage clear jingle with the one from Adventure 2 if not in the Emerald Hill recreation.
                if (SceneManager.GetActiveScene().name != "EmeraldHill")
                    UnityEngine.Object.FindObjectOfType<FPAudio>().jingleStageComplete = tailsClearJingle;

                // Create and set up the extra audio channel object for Tails' flight sound.
                GameObject flightSound = new("PlayerAudioSource");
                flightSound.transform.parent = player.gameObject.transform;
                player.audioChannel = player.audioChannel.AddToArray(flightSound.AddComponent<AudioSource>());
                player.audioChannel[4].volume = FPSaveManager.volumeSfx;
                player.audioChannel[4].clip = player.sfxMove;
                player.audioChannel[4].loop = true;

                if (SceneManager.GetActiveScene().name == "EmeraldHillTutorial")
                {
                    player.vaClear = [tailsAssetBundle.LoadAsset<AudioClip>("all01_e02_tl")];
                    player.vaJackpotClear = [tailsAssetBundle.LoadAsset<AudioClip>("all01_e02_tl")];
                    player.vaLowDamageClear = [tailsAssetBundle.LoadAsset<AudioClip>("all01_e02_tl")];
                }

                // Emerald Hill Zone edits for the SA2-esque Easter Egg.
                if (SceneManager.GetActiveScene().name == "EmeraldHill")
                {
                    player.sfxJump = tailsAssetBundle.LoadAsset<AudioClip>("classic_jump");
                    player.sfxSkid = tailsAssetBundle.LoadAsset<AudioClip>("classic_skid");

                    player.vaKO = null;
                    player.vaAttack = new AudioClip[1];
                    player.vaHardAttack = new AudioClip[3]; // Set to 3 instead of 1 specifically so the Tail Swipe doesn't cause errors.
                    player.vaSpecialA = new AudioClip[1];
                    player.vaSpecialB = new AudioClip[1];
                    player.vaHit = new AudioClip[1];
                    player.vaRevive = new AudioClip[1];
                    player.vaStart = new AudioClip[1];
                    player.vaItemGet = new AudioClip[1];
                    player.vaClear = new AudioClip[1];
                    player.vaJackpotClear = new AudioClip[1];
                    player.vaLowDamageClear = new AudioClip[1];
                    player.vaExtra = new AudioClip[1];

                    player.bgmResults = tailsAssetBundle.LoadAsset<AudioClip>("M_ClearSilent");

                    player.extraLifeCost = 100;
                    player.crystals = 100;

                    player.audioChannel[4].clip = tailsAssetBundle.LoadAsset<AudioClip>("classic_fly");
                    tailSounds = ["Fly", "UpAir"];
                }
            }
        }

        /// <summary>
        /// Handle playing and stopping Tails' flight sound.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPPlayer), "LateUpdate")]
        private static void TailSound(FPPlayer __instance)
        {
            // Only do this if we're playing as Tails.
            if (__instance.characterID != tailsCharacterID)
                return;

            // Check if the sound isn't already playing and we're in one of the animations that should play the sound and play it if so.
            if (!__instance.audioChannel[4].isPlaying && tailSounds.Contains(__instance.currentAnimation))
                __instance.audioChannel[4].Play();

            // If those conditions fail, then stop the sound.
            if (__instance.audioChannel[4].isPlaying && !tailSounds.Contains(__instance.currentAnimation))
                __instance.audioChannel[4].Stop();
        }

        #region Ground States/Actions
        /// <summary>
        /// Tails' moves when in the ground state.
        /// </summary>
        public static void Action_Tails_GroundMoves()
        {
            player.displayMoveJump = "Jump";
            player.displayMoveAttack = "-";
            player.displayMoveSpecial = "Wrench Toss";
            player.displayMoveGuard = "-";

            // Refill the energy gauge when on the ground.
            if (player.onGround)
                player.energy = 100f;

            #region Spin Dash
            // Check that the player is holding down, has pressed jump and are in the crouching state.
            if (player.input.down && player.state == new FPObjectState(player.State_Crouching))
            {
                player.displayMoveJump = "Spin Dash";

                if (player.input.jumpPress)
                {
                    // Set the player to the Spindash animation.
                    player.SetPlayerAnimation("Spindash");

                    // Reset the generic timer.
                    player.genericTimer = 0f;

                    // Set the player's state to Tails' Spin Dash state.
                    player.state = State_Tails_SpinDash;

                    // Play the Spin Dash charge sound.
                    player.Action_PlaySound(player.sfxBoostCharge);

                    // Create the smoke particles.
                    CreateSpinDashSmoke(24, 1);
                    CreateSpinDashSmoke(32, 1.25f);
                    CreateSpinDashSmoke(48, 1.5f);
                    CreateSpinDashSmoke(64, 1.75f);
                }
            }
            #endregion

            #region Rolling
            // Check that the player holding down, isn't crouching or rolling and has at least 3 ground velocity.
            if (player.input.down && player.state != new FPObjectState(player.State_Crouching) && player.state != new FPObjectState(State_Tails_Roll) && Mathf.Abs(player.groundVel) > 3f)
            {
                // Reset the generic timer.
                player.genericTimer = 0f;

                // Set the player's state to Tails' rolling state.
                player.state = State_Tails_Roll;

                // Play the rolling sound effect.
                player.Action_PlaySoundUninterruptable(player.sfxRolling);
            }
            #endregion

            #region Tail Swipe
            // Check that we're not already in the Tail Swipe state, are pressing attack but not up.
            if (player.state != new FPObjectState(State_Tails_TailSwipe) && !player.input.up)
            {
                player.displayMoveAttack = "Tail Swipe";

                if (player.input.attackPress)
                {
                    // Play the uppercut sound.
                    player.Action_PlaySound(player.sfxDivekick1);

                    // Reset the idle timer based on the fight stance time.
                    player.idleTimer = 0f - player.fightStanceTime;

                    // Play the grounded Tail Swipe animation.
                    player.SetPlayerAnimation("TailSwipe");

                    // Set our state to the Tail Swipe's.
                    player.state = State_Tails_TailSwipe;

                    // Play a sound from the hard attack array.
                    player.audioChannel[0].PlayOneShot(player.vaHardAttack[UnityEngine.Random.Range(0, 3)]);
                }
            }
            #endregion

            #region Wrench Toss
            // Check that we've pressed the special button.
            if (player.input.specialPress)
            {
                // Play the right toss animation depending on the player's speed.
                if (player.groundVel >= 3 || player.groundVel <= -3) player.SetPlayerAnimation("WrenchToss_Run");
                else player.SetPlayerAnimation("WrenchToss_Stand");
                
                // Swap to the Wrench Throw state.
                player.state = State_Tails_WrenchThrow;
            }
            #endregion

            #region Guard
            // Check that we can guard and aren't in any state that we shouldn't be able to guard cancel out of.
            if ((player.guardTime <= 0f || player.cancellableGuard) && player.state != new FPObjectState(State_Tails_SpinDash))
            {
                player.displayMoveGuard = "Guard";

                if (player.input.guardPress)
                {
                    player.displayMoveGuard = "-";

                    // Check if we're moving slow enough to use a standing guard.
                    if (Mathf.Abs(player.groundVel) < 3f)
                    {
                        // Play the standing guard animation.
                        player.SetPlayerAnimation("Guard");

                        // Reset the idle timer.
                        player.idleTimer = Mathf.Min(player.idleTimer, 0f);

                        // Kill our ground velocity so we stop in place.
                        player.groundVel = 0f;
                    }
                    else
                    {
                        // Play the running guard animation.
                        player.SetPlayerAnimation("GuardRun");

                        // Edit the animator's speed depending on the player velocity.
                        player.animator.SetSpeed(Mathf.Max(1f, 0.7f + Mathf.Abs(player.velocity.x * 0.05f)));
                    }

                    // Run the guard actions.
                    player.Action_Guard();
                    player.Action_ShadowGuard();

                    // Create the guard flash and parent it to Sonic.
                    GuardFlash guardFlash = (GuardFlash)FPStage.CreateStageObject(GuardFlash.classID, player.position.x, player.position.y);
                    guardFlash.parentObject = player;

                    // Stop playing sounds (this is just what the base game does so we're copying it).
                    player.Action_StopSound();

                    // Play the guard sound.
                    FPAudio.PlaySfx(15);
                }
            }
            #endregion

            #region Back Rotor
            // Check that we're not already in the Back Rotor state, but are pressing attack and up.
            if (player.state != new FPObjectState(State_Tails_BackRotor) && player.input.up)
            {
                player.displayMoveAttack = "Back Rotor";

                if (player.input.attackPress)
                {
                    // Reset the idle timer based on the fight stance time.
                    player.idleTimer = 0f - player.fightStanceTime;

                    // Play the Back Rotor's animation.
                    player.SetPlayerAnimation("UpAir");

                    // Reset the animator speed.
                    player.animator.SetSpeed(1);

                    // Set the player's velocity.
                    player.velocity.x /= 2;
                    if (player.direction == FPDirection.FACING_LEFT) player.velocity.x = System.Math.Abs(player.velocity.x);
                    else player.velocity.x = System.Math.Abs(player.velocity.x) * -1;
                    player.velocity.y = Mathf.Max(player.velocity.y + 6, 10);

                    // Take the player off the ground.
                    player.onGround = false;

                    // Set our state to the Back Rotor's.
                    player.state = State_Tails_BackRotor;
                }
            }
            #endregion
        }

        /// <summary>
        /// Logic for Tails' Spin Dash charging state. Copied from my Sonic mod.
        /// </summary>
        public static void State_Tails_SpinDash()
        {
            player.displayMoveJump = "Spin Dash";
            player.displayMoveAttack = "-";
            player.displayMoveSpecial = "-";
            player.displayMoveGuard = "-";

            // Set the player's attack stats to Tails' roll.
            player.attackStats = AttackStats_Tails_Roll;

            // Increment the generic timer.
            player.genericTimer += FPStage.deltaTime;

            // If the player has any horizontal momentum, then halt it.
            if (player.velocity.x != 0)
                player.velocity = new(0, player.velocity.y);
            player.groundVel = 0;

            // If the player isn't on the ground, then apply gravity to them and stop the rest of the function from running.
            if (!player.onGround)
            {
                applyGravity.Invoke(player, new object[] { });
                return;
            }

            // Check that the player is pressing down and jump.
            if (player.input.down && player.input.jumpPress)
            {
                // Create the smoke particles.
                CreateSpinDashSmoke(24, 1);
                CreateSpinDashSmoke(32, 1.25f);
                CreateSpinDashSmoke(48, 1.5f);
                CreateSpinDashSmoke(64, 1.75f);

                // Play the Spin Dash charge sound again.
                player.Action_PlaySound(player.sfxBoostCharge);

                // Increment the Spin Dash's multiplier up to a maximum of 2 (the check is 1.9, but that's so the decaying force can't push it over.
                if (SpinDashMultiplier < 1.9f)
                    SpinDashMultiplier += 0.2f;

                // Reset the generic timer.
                player.genericTimer = 0f;
            }

            // Check if the generic timer has reached 15.
            if (player.genericTimer >= 15)
            {
                // Reset the generic timer.
                player.genericTimer = 0f;

                // Decay a bit of force from the Spin Dash.
                if (SpinDashMultiplier > 1)
                    SpinDashMultiplier -= 0.1f;
            }

            // Check if the player has released down.
            if (!player.input.down)
            {
                // Set the player's ground velocity based on direction.
                if (player.direction == FPDirection.FACING_LEFT)
                    player.groundVel = Mathf.Min(Mathf.Min(player.groundVel, 0f) * 0.5f - 15f, player.groundVel) * SpinDashMultiplier;
                else
                    player.groundVel = Mathf.Max(Mathf.Max(player.groundVel, 0f) * 0.5f + 15f, player.groundVel) * SpinDashMultiplier;

                // Reset the generic timer.
                player.genericTimer = 0f;

                // Set the player's state to Tails' rolling state.
                player.state = State_Tails_Roll;

                // Stop the Spin Dash charge sound.
                player.Action_StopSound();

                // Play the Spin Dash release sound.
                player.Action_PlaySoundUninterruptable(player.sfxBoostLaunch);

                // Reset the Spin Dash's multiplier.
                SpinDashMultiplier = 1f;
            }
        }

        /// <summary>
        /// Logic for Tails' rollling state. Copied from my Sonic mod.
        /// </summary>
        public static void State_Tails_Roll()
        {
            // Set the player's attack stats to Tails' roll.
            player.attackStats = AttackStats_Tails_Roll;

            // Increment the generic timer.
            player.genericTimer += FPStage.deltaTime;

            // Set the player to the rolling animation.
            player.SetPlayerAnimation("Rolling");

            // Check if the player is on the ground.
            if (player.onGround)
            {
                // Set the animator's speed based on the player's ground velocity.
                player.animator.SetSpeed(Mathf.Abs(player.groundVel) * 0.15f);

                // Run the Ground Moves action.
                Action_Tails_GroundMoves();

                // Check if the player presses jump.
                if (player.input.jumpPress)
                {
                    // Perform the soft jump action.
                    player.Action_SoftJump();

                    // Set the animator speed to 2.
                    player.animator.SetSpeed(2f);
                }

                // Check if the player hasn't pressed jump.
                else
                {
                    // If the generic timer has gone above 15, apply ground forces onto the player.
                    if (player.genericTimer > 15f)
                        applyGround.Invoke(player, new object[] { false });

                    // Set the player's angle to their ground angle.
                    player.angle = player.groundAngle;
                }
            }

            // Check if the player is in the air.
            else
            {
                // Apply the air and gravity forces onto the player.
                applyAir.Invoke(player, new object[] { false });
                applyGravity.Invoke(player, new object[] { });

                // Check if the player isn't holding jump and has just released it.
                if (!player.input.jumpHold && player.jumpReleaseFlag)
                {
                    // Reset the jump release flag.
                    player.jumpReleaseFlag = false;

                    // Cap the player's y velocity.
                    if (player.velocity.y > player.jumpRelease)
                        player.velocity.y = player.jumpRelease;
                }
            }

            // If the player is on the ground below a certain speed, set them back to the normal ground state.
            if (player.onGround && Mathf.Abs(player.groundVel) <= 1.5f)
                player.state = player.State_Ground;

            // If the player isn't on the ground, then set them to the in air state.
            if (!player.onGround)
                player.state = player.State_InAir;
        }

        /// <summary>
        /// Stops Tails from jumping when crouching so he can Spin Dash.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FPPlayer), "Action_Jump")]
        private static bool StopCrouchJump()
        {
            // If the player is Tails and is crouching, then stop the original function from running.
            if (player.state == new FPObjectState(player.State_Crouching) && player.characterID == tailsCharacterID)
                return false;

            // Otherwise, run the original function as normal.
            return true;
        }

        /// <summary>
        /// Creates the smoke for the Spin Dash.
        /// </summary>
        /// <param name="player">The player object creating the smoke.</param>
        /// <param name="xOffset">How far offset on the X axis this smoke should be.</param>
        /// <param name="scaleModifier">The scale modifier for this smoke.</param>
        private static void CreateSpinDashSmoke(float xOffset, float scaleModifier)
        {
            // Create a dust object and set up the static values.
            Dust dust = (Dust)FPStage.CreateStageObject(Dust.classID, player.position.x, player.position.y);
            dust.SetParentObject(player);
            dust.yOffset = -32f;

            // Set the scale of the dust.
            dust.scale = new Vector3(scaleModifier, scaleModifier, scaleModifier);

            // Set the velocity and x offset of the dust depending on the player's direction.
            if (player.direction == FPDirection.FACING_RIGHT)
            {
                dust.velocity = new Vector2(-0.2f, 0f);
                dust.xOffset = -xOffset;
            }
            else
            {
                dust.velocity = new Vector2(0.2f, 0f);
                dust.xOffset = xOffset;
            }
        }
        #endregion

        #region Air States/Actions
        /// <summary>
        /// Tails' moves when in the air state.
        /// </summary>
        public static void Action_Tails_AirMoves()
        {
            player.displayMoveJump = "-";
            player.displayMoveAttack = "-";
            player.displayMoveSpecial = "Wrench Toss";
            player.displayMoveGuard = "-";

            #region Flying
            // Check if the player has enough energy and presses Jump.
            if (player.energy > 10)
            {
                player.displayMoveJump = "Fly";

                if (player.input.jumpPress)
                {
                    // Take ten energy away.
                    player.energy -= 10;

                    // Change to the flight state.
                    player.state = State_Tails_Flight;

                    // Give the player three units of upwards momentum atop their current velocity.
                    player.velocity.y += 3 * FPStage.deltaTime;

                    // Play a voice line from the SpecialA array.
                    if (player.targetWaterSurface == null) player.Action_PlayVoiceArray("SpecialA");
                }
            }
            #endregion

            #region Tail Swipe
            // Check that we're not already in the Tail Swipe state and are pressing attack but not up or down.
            if (player.state != new FPObjectState(State_Tails_TailSwipe) && !player.input.down && !player.input.up)
            {
                player.displayMoveAttack = "Tail Swipe";

                if (player.input.attackPress)
                {
                    // Play the uppercut sound.
                    player.Action_PlaySound(player.sfxDivekick1);

                    // Reset the idle timer based on the fight stance time.
                    player.idleTimer = 0f - player.fightStanceTime;

                    // Play the aerial Tail Swipe animation.
                    player.SetPlayerAnimation("TailSwipe_Air");

                    // Set our state to the Tail Swipe's.
                    player.state = State_Tails_TailSwipe;

                    // Play a sound from the hard attack array.
                    player.audioChannel[0].PlayOneShot(player.vaHardAttack[UnityEngine.Random.Range(0, 3)]);
                }
            }
            #endregion
            
            #region Tails Dunk
            // Check that we're not already in the Tails Dunk state, but are pressing attack and down.
            if (player.state != new FPObjectState(State_Tails_TailsDunk) && player.input.down)
            {
                player.displayMoveAttack = "Tails Dunk";

                if (player.input.attackPress)
                {
                    // Play the uppercut sound.
                    player.Action_PlaySound(player.sfxDivekick1);

                    // Reset the idle timer based on the fight stance time.
                    player.idleTimer = 0f - player.fightStanceTime;

                    // Play the Tails Dunk animation.
                    player.SetPlayerAnimation("DownAir");

                    // Set our state to the Tails Dunk's.
                    player.state = State_Tails_TailsDunk;

                    // Play a sound from the hard attack array.
                    player.audioChannel[0].PlayOneShot(player.vaHardAttack[UnityEngine.Random.Range(0, 3)]);
                }
            }
            #endregion

            #region Back Rotor
            // Check that we're not already in the Back Rotor state, but are pressing attack and up.
            if (player.state != new FPObjectState(State_Tails_BackRotor) && player.input.up)
            {
                player.displayMoveAttack = "Back Rotor";

                if (player.input.attackPress)
                {
                    // Reset the idle timer based on the fight stance time.
                    player.idleTimer = 0f - player.fightStanceTime;

                    // Play the Back Rotor's animation.
                    player.SetPlayerAnimation("UpAir");

                    // Reset the animator speed.
                    player.animator.SetSpeed(1);

                    // Set the player's velocity.
                    player.velocity.x /= 2;
                    if (player.direction == FPDirection.FACING_LEFT) player.velocity.x = System.Math.Abs(player.velocity.x);
                    else player.velocity.x = System.Math.Abs(player.velocity.x) * -1;
                    player.velocity.y = Mathf.Max(player.velocity.y + 6, 10);

                    // Set our state to the Back Rotor's.
                    player.state = State_Tails_BackRotor;
                }
            }
            #endregion

            #region Wrench Toss
            // Check that we've pressed the special button.
            if (player.input.specialPress)
            {
                // Play the aerial toss animation.
                player.SetPlayerAnimation("WrenchToss_Air");

                // Swap to the Wrench Throw state.
                player.state = State_Tails_WrenchThrow;
            }
            #endregion

            #region Super Tails
            // Only do any of this if Sonic is installed.
            if (Chainloader.PluginInfos.ContainsKey("K24_FP2_Sonic"))
            {
                // Check that the player meets the criteria to go Super.
                if (player.powerups.Contains((FPPowerup)ChaosEmeraldsItem.itemID) && player.totalCrystals >= 50 && player.state != new FPObjectState(State_Tails_Roll) && player.currentAnimation == "Rolling" && !isSuper)
                {
                    player.displayMoveGuard = "<w><c=energy>Super Tails</c></w>";

                    if (player.input.guardPress)
                    {
                        // Swap to the Super Transform state.
                        player.state = State_Tails_SuperTransform;

                        // Kill the player's velocity.
                        player.velocity = Vector2.zero;

                        // Set the player to the SuperStart animation.
                        player.SetPlayerAnimation("SuperStart");

                        // Spawn the orbiting Chaos Emeralds.
                        TransformEmeralds = GameObject.Instantiate(tailsAssetBundle.LoadAsset<GameObject>("Chaos Emerald Orbit"));
                        TransformEmeralds.transform.position = new(player.transform.position.x, player.transform.position.y, player.transform.position.z);
                        TransformEmeralds.AddComponent<OrbitingEmeralds>();

                        // Set the player's invincibility time high so that Tails can't be knocked out of the Super Transformation state.
                        player.invincibilityTime = 9999;
                    }
                }

                // Check that player meets the criteria to detransform.
                if (player.state != new FPObjectState(State_Tails_Roll) && player.currentAnimation == "Rolling" && isSuper)
                {
                    player.displayMoveGuard = "<c=energy>Detransform</c>";

                    if (player.input.guardPress)
                    {
                        // Reset the player's jump strength.
                        player.jumpStrength = player.GetPlayerStat_Default_JumpStrength();

                        // If the super music is playing, then play the last used audio we stored.
                        if (FPAudio.GetCurrentMusic() == tailsSuperMusic)
                            FPAudio.PlayMusic(lastUsedAudio);

                        // Reset the player's velocity.
                        player.velocity = Vector2.zero;

                        // Spawn the expelling Emeralds.
                        GameObject detransformEmeralds = GameObject.Instantiate(tailsAssetBundle.LoadAsset<GameObject>("Chaos Emerald Orbit"));
                        detransformEmeralds.transform.position = new(player.transform.position.x, player.transform.position.y, player.transform.position.z);
                        var emeraldScript = detransformEmeralds.AddComponent<OrbitingEmeralds>();
                        emeraldScript.expel = true;

                        // Set the player's state to the Super Detransformation one.
                        player.state = State_Tails_SuperDetransform;
                    }
                }
            }
            #endregion

            #region Guard
            // Check that we can guard and aren't in any state that we shouldn't be able to guard cancel out of.
            if ((player.guardTime <= 0f || player.cancellableGuard) && player.state != new FPObjectState(State_Tails_SuperTransform) && !isSuper)
            {
                player.displayMoveGuard = "Guard";

                if (player.input.guardPress)
                {
                    if (player.displayMoveGuard != "<w><c=energy>Super Tails</c></w>") player.displayMoveGuard = "-";

                    // Play the Guard animation.
                    player.SetPlayerAnimation("GuardAir", 0f, 0f);

                    // Edit the animator's speed depending on the player velocity.
                    player.animator.SetSpeed(Mathf.Max(1f, 0.7f + Mathf.Abs(player.velocity.x * 0.05f)));

                    // Run the guard actions.
                    player.Action_Guard();
                    player.Action_ShadowGuard();

                    // Create the guard flash and parent it to Sonic.
                    GuardFlash guardFlash = (GuardFlash)FPStage.CreateStageObject(GuardFlash.classID, player.position.x, player.position.y);
                    guardFlash.parentObject = player;

                    // Stop playing sounds (this is just what the base game does so we're copying it).
                    player.Action_StopSound();

                    // Play the guard sound.
                    FPAudio.PlaySfx(15);
                }
            }
            #endregion
        }

        /// <summary>
        /// Logic for Tails' normal flight.
        /// </summary>
        private static void State_Tails_Flight()
        {
            player.displayMoveJump = "Fly";
            player.displayMoveAttack = "-";
            player.displayMoveSpecial = "Dummy Ring Bomb";
            player.displayMoveGuard = "-";

            #region Guard
            // Check that we can guard and aren't in any state that we shouldn't be able to guard cancel out of.
            if ((player.guardTime <= 0f || player.cancellableGuard))
            {
                player.displayMoveGuard = "Guard";

                if (player.input.guardPress)
                {
                    player.displayMoveGuard = "-";

                    // Play the Guard animation.
                    player.SetPlayerAnimation("GuardAir", 0f, 0f);

                    // Edit the animator's speed depending on the player velocity.
                    player.animator.SetSpeed(Mathf.Max(1f, 0.7f + Mathf.Abs(player.velocity.x * 0.05f)));

                    // Run the guard actions.
                    player.Action_Guard();
                    player.Action_ShadowGuard();

                    // Create the guard flash and parent it to Sonic.
                    GuardFlash guardFlash = (GuardFlash)FPStage.CreateStageObject(GuardFlash.classID, player.position.x, player.position.y);
                    guardFlash.parentObject = player;

                    // Stop playing sounds (this is just what the base game does so we're copying it).
                    player.Action_StopSound();

                    // Play the guard sound.
                    FPAudio.PlaySfx(15);

                    // Don't run any more of the flight state.
                    return;
                }
            }
            #endregion
            
            #region Tails Dunk
            // Check that we're not already in the Tails Dunk state, but are pressing attack and down.
            if (player.state != new FPObjectState(State_Tails_TailsDunk) && player.input.down)
            {
                player.displayMoveAttack = "Tails Dunk";

                if (player.input.attackPress)
                {
                    // Play the uppercut sound.
                    player.Action_PlaySound(player.sfxDivekick1);

                    // Reset the idle timer based on the fight stance time.
                    player.idleTimer = 0f - player.fightStanceTime;

                    // Play the Tails Dunk animation.
                    player.SetPlayerAnimation("DownAir");

                    // Set our state to the Tails Dunk's.
                    player.state = State_Tails_TailsDunk;

                    // Play a sound from the hard attack array.
                    player.audioChannel[0].PlayOneShot(player.vaHardAttack[UnityEngine.Random.Range(0, 3)]);

                    // Don't run any more of the flight state.
                    return;
                }
            }
            #endregion

            // Decrement the generic timer if a value is set on it.
            if (player.genericTimer > 0)
                player.genericTimer -= FPStage.deltaTime;

            // Set Tails to his flight or swim animation.
            if (player.targetWaterSurface == null) player.SetPlayerAnimation("Fly");
            else player.SetPlayerAnimation("Swim");

            // Swap to the flight attack stats.
            player.attackStats = AttackStats_Tails_Flight;

            // Drain some energy from the energy gauge.
                player.energy -= 0.3f * FPStage.deltaTime;

            // Hack to refil the energy gague in the end part of Gravity Bubble or in Bubble Battle.
            if (player.targetWaterSurface != null)
                if (player.targetWaterSurface.GetType() == typeof(FPWaterSquare) && (SceneManager.GetActiveScene().name == "GravityBubble" || SceneManager.GetActiveScene().name == "Battlesphere_Arena2"))
                    player.energy = 100;

            // Increase the speed if we have the Jet Anklet active.
            if (player.powerupTimer > 0)
            {
                player.topSpeed = player.GetPlayerStat_Default_TopSpeed() * 2;
                player.airAceleration = player.GetPlayerStat_Default_AirAceleration() * 2;

                // Create a trail object every four frames. Means the trail will be thicker or thinner depending on framerate but OH WELL.
                if (Mathf.Floor(FPPlayerPatcher.player.powerupTimer) % 4 == 0)
                {
                    GameObject trailObject = new($"JetAnkletTrail");
                    trailObject.layer = player.gameObject.layer;
                    trailObject.transform.position = player.gameObject.transform.position;
                    trailObject.transform.rotation = player.gameObject.transform.rotation;
                    trailObject.transform.localScale = player.gameObject.transform.localScale;

                    SpriteRenderer trailSprite = trailObject.AddComponent<SpriteRenderer>();
                    trailSprite.sprite = player.gameObject.GetComponent<SpriteRenderer>().sprite;
                    trailSprite.sortingOrder = player.gameObject.GetComponent<SpriteRenderer>().sortingOrder - 1;

                    trailObject.AddComponent<JetAnkletTrail>();
                }
            }

            // Apply the air and gravity forces onto the player.
            applyAir.Invoke(player, new object[] { false });
            applyGravity.Invoke(player, new object[] { });

            // Undo the speed change from the Jet Anklet.
            if (player.powerupTimer > 0)
            {
                player.topSpeed = player.GetPlayerStat_Default_TopSpeed();
                player.airAceleration = player.GetPlayerStat_Default_AirAceleration();
            }

            // Cap the player's downward velocity if they're not holding down.
            if (player.velocity.y < -6f && !player.input.down)
                player.velocity.y = -6f;

            if (player.powerupTimer > 0 && player.velocity.y < -3f && !player.input.down)
                player.velocity.y = -3f;

            // Exit the flight state if we're on the ground.
            if (player.onGround)
                player.state = player.State_Ground;

            // Throw a Dummy Ring Bomb if the special button is pressed.
            if (player.input.specialPress && player.genericTimer <= 0)
            {
                player.genericTimer = 15;
                Action_Tails_DummyRing();
            }

            // Check if the player has energy.
            if (player.energy > 0)
            {
                // If the player presses Jump, then give them six units of upwards momentum atop their current velocity.
                if (player.input.jumpPress)
                    player.velocity.y += 6 * FPStage.deltaTime;

                // Cap the player's upward velocity to eight.
                if (player.velocity.y > 8)
                    player.velocity.y = 8;

                // Don't run the rest of this function.
                return;
            }

            // Play the tired voice line and sound if we're not in water.
            if (player.targetWaterSurface == null) 
            {
                player.Action_PlayVoice(player.vaExtra[0]);
                player.Action_PlaySound(player.sfxIdle);
            }

            // Swap to the tired flight state.
            player.state = State_Tails_TiredFlight;
        }

        /// <summary>
        /// Logic for Tails' flight when he's run out of energy.
        /// </summary>
        private static void State_Tails_TiredFlight()
        {
            player.displayMoveJump = "-";
            player.displayMoveAttack = "-";
            player.displayMoveSpecial = "Dummy Ring Bomb";
            player.displayMoveGuard = "-";

            // Set Tails to his tired flight or swim animation.
            if (player.targetWaterSurface == null) player.SetPlayerAnimation("FlyTired");
            else player.SetPlayerAnimation("SwimTired");

            // Apply the air and gravity forces onto the player.
            applyAir.Invoke(player, new object[] { false });
            applyGravity.Invoke(player, new object[] { });

            // Cap the player's downward velocity if they're not holding down.
            if (player.velocity.y < -12f && !player.input.down)
                player.velocity.y = -12f;

            // Exit the flight state if we're on the ground.
            if (player.onGround)
                player.state = player.State_Ground;
        }

        /// <summary>
        /// Logic for throwing a Dummy Ring Bomb.
        /// </summary>
        private static void Action_Tails_DummyRing()
        {
            // Play the voice line and sound for throwing the Dummy Ring Bomb.
            player.Action_PlayVoiceArray("SpecialB");
            player.Action_PlaySound(player.sfxDivekick1);

            // Create the Dummy Ring Bomb and set up its script.
            GameObject dummyRingBomb = UnityEngine.Object.Instantiate(tailsAssetBundle.LoadAsset<GameObject>("Dummy Ring Capsule"));
            var ringBombScript = dummyRingBomb.AddComponent<DummyRingCapsule>();
            if (player.direction == FPDirection.FACING_RIGHT) dummyRingBomb.transform.position = new(player.transform.position.x + 16, player.transform.position.y - 32, player.transform.position.z);
            else dummyRingBomb.transform.position = new(player.transform.position.x - 16, player.transform.position.y - 32, player.transform.position.z);
            ringBombScript.velocity.x = player.velocity.x / 4;
        }
        
        /// <summary>
        /// Logic for the Tails Dunk attack.
        /// TODO: Allow this to be chained?
        /// </summary>
        public static void State_Tails_TailsDunk()
        {
            player.displayMoveJump = "-";
            player.displayMoveAttack = "-";
            player.displayMoveSpecial = "-";
            player.displayMoveGuard = "-";

            // Set the player's attack stats to Tails' tail swipe.
            player.attackStats = AttackStats_Tails_TailSwipe;
            
                // Check that we're on the ground still and swap to the grounded state if so.
            if (player.onGround)
            {
                player.state = player.State_Ground;
            }

            // Apply the air and gravity forces onto the player if they're airborne instead.
            else
            {
                applyAir.Invoke(player, new object[] { false });
                applyGravity.Invoke(player, new object[] { });
            }

            // Check if we've reached the end of the dunk animation.
            if (player.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.95f)
            {
                // Check that we're on the ground still and swap to the grounded state if so.
                if (player.onGround)
                {
                    player.state = player.State_Ground;
                }

                // Swap to the in air state and the jump animation if we're in the air instead.
                else
                {
                    player.state = player.State_InAir;
                    player.SetPlayerAnimation("Jumping_Loop");
                }
            }

            // Cap the Y velocity to -9.
            player.velocity.y = Mathf.Max(-9f, player.velocity.y);
        }

        /// <summary>
        /// Logic for the Back Rotor move.
        /// </summary>
        public static void State_Tails_BackRotor()
        {
            player.displayMoveJump = "-";
            player.displayMoveAttack = "-";
            player.displayMoveSpecial = "-";
            player.displayMoveGuard = "-";

            // Disable air drag.
            player.airDrag = false;

            // Reenable air drag and start flying if we're at the end of the exit animation.
            if (player.currentAnimation == "UpAir_Exit" && player.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.95f)
            {
                player.airDrag = true;
                player.state = State_Tails_Flight;
            }

            // Set the player's attack stats to Tails' tail swipe.
            player.attackStats = AttackStats_Tails_TailSwipe;

            // If we're on the ground, then reenable air drag and go to the normal ground state.
            if (player.onGround)
            {
                player.airDrag = true;
                player.state = player.State_Ground;
            }

            else
            {
                // Apply the air and gravity forces onto the player.
                applyAir.Invoke(player, new object[] { true });
                applyGravity.Invoke(player, new object[] { });

                // If we've started descending, then reenable air drag and swap to the exit animation.
                if (player.velocity.y <= 0 && player.currentAnimation == "UpAir")
                {
                    player.airDrag = true;
                    player.SetPlayerAnimation("UpAir_Exit");
                }
            }
        }

        /// <summary>
        /// Logic for transforming into Super Tails.
        /// </summary>
        public static void State_Tails_SuperTransform()
        {
            if (TransformEmeralds != null)
                return;

            if (!createdSparks)
            {
                // If the super music isn't playing, then play it.
                if (FPAudio.GetCurrentMusic() != tailsSuperMusic)
                    FPAudio.PlayMusic(tailsSuperMusic);

                // Swap the Super Form version of the flashing animation.
                player.SetPlayerAnimation("SuperBlink");

                // Play the Super Transformation sound.
                player.Action_PlaySoundUninterruptable(player.sfxCarolAttack2);

                // Set the created sparks flag.
                createdSparks = true;

                // Shake the camera a bit.
                FPCamera.stageCamera.screenShake = Mathf.Max(FPCamera.stageCamera.screenShake, 10f);

                // Loop through four times.
                for (int sparkIndex = 0; sparkIndex < 4; sparkIndex++)
                {
                    // Create a spark.
                    Spark spark = (Spark)FPStage.CreateStageObject(Spark.classID, player.position.x, player.position.y);
                    spark.velocity.x = Mathf.Cos((float)Math.PI / 180f * ((float)sparkIndex * 90f + 45f)) * 20f;
                    spark.velocity.y = Mathf.Sin((float)Math.PI / 180f * ((float)sparkIndex * 90f + 45f)) * 20f;
                    spark.SetAngle();
                }

                // Play the Boost Breaker sound from Lilac's prefab.
                player.Action_PlaySoundUninterruptable(lilacPrefab.GetComponent<FPPlayer>().sfxBoostExplosion);

                // Create the Boost Breaker explosion.
                BoostExplosion boostExplosion = (BoostExplosion)FPStage.CreateStageObject(BoostExplosion.classID, player.position.x, player.position.y);
                boostExplosion.attackKnockback.x = player.attackKnockback.x * 0.5f;
                boostExplosion.attackKnockback.y = player.attackKnockback.y * 0.5f;
                boostExplosion.attackEnemyInvTime = player.attackEnemyInvTime;
                boostExplosion.parentObject = player;
                boostExplosion.faction = player.faction;

                // Create the Invincibility stars and set the flash timer.
                InvincibilityStar invincibilityStar = (InvincibilityStar)FPStage.CreateStageObject(InvincibilityStar.classID, -100f, -100f);
                invincibilityStar.parentObject = player;
                InvincibilityStar invincibilityStar2 = (InvincibilityStar)FPStage.CreateStageObject(InvincibilityStar.classID, -100f, -100f);
                invincibilityStar2.parentObject = player;
                invincibilityStar2.rotation = 180f;
                player.flashTime = 1200f;
            }

            // Increment our super timer by the stage's delta time.
            SuperStartTimer += FPStage.deltaTime;

            // Check if the start timer has gone above 48 and swap to the descending animation.
            if (SuperStartTimer >= 48)
            {
                createdSparks = false;

                // Set the isSuper flag.
                isSuper = true;

                // Reset the super time counters.
                superTimeCounter = 0f;
                SuperStartTimer = 0;

                // Set the player to the InAir state.
                player.state = player.State_InAir;

                // Set the player to the Jumping animation, specifically the looping part.
                player.SetPlayerAnimation("Jumping_Loop");
            }
        }

        /// <summary>
        /// Makes Tails jump with the rolling animation instead of the normal one.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPPlayer), "Action_Jump")]
        private static void MakeJumpRoll()
        {
            // Check if we're in the jumping animation from Action_Jump and the player is Tails.
            if (player.currentAnimation == "Jumping" && player.characterID == tailsCharacterID)
            {
                // Swap to the rolling animation.
                player.currentAnimation = "Rolling";

                // Set the player's attack stats to Tails' roll.
                player.attackStats = AttackStats_Tails_Roll;
            }
        }
        #endregion

        #region Misc. States/Actions
        /// <summary>
        /// Logic for collecting the Power Up item (the Jet Anklet).
        /// </summary>
        public static void Action_Tails_Fuel()
        {
            // Stop any playing jingles then play the SA1 Power Sneakers jingle.
            FPAudio.StopJingle();
            FPAudio.PlayJingle(tailsJetAnkletJingle);

            // Set our power up timer to 900 (roughly 15 seconds).
            player.powerupTimer = Mathf.Max(player.powerupTimer, 900f);
        }

        /// <summary>
        /// Logic for Tails' tail swipe state.
        /// </summary>
        public static void State_Tails_TailSwipe()
        {
            player.displayMoveJump = "Jump";
            player.displayMoveAttack = "Tail Swipe";
            player.displayMoveSpecial = "-";
            player.displayMoveGuard = "-";
            
            // Set the player's attack stats to Tails' tail swipe.
            player.attackStats = AttackStats_Tails_TailSwipe;

            // Check if we're on the ground to apply basic ground stuff and allow jumping.
            if (player.onGround)
            {
                if (player.input.jumpPress)
                {
                    player.Action_SoftJump();
                }
                else
                {
                    // Play the grounded Tail Swipe animation.
                    player.SetPlayerAnimation("TailSwipe", player.animator.GetCurrentAnimatorStateInfo(0).normalizedTime);

                    // Apply the ground forces.
                    applyGround.Invoke(player, new object[] { false });

                    // Set our angle to the ground angle so we rotate with the terrain.
                    player.angle = player.groundAngle;
                }
            }

            else
            {
                // Play the aerial Tail Swipe animation.
                player.SetPlayerAnimation("TailSwipe_Air", player.animator.GetCurrentAnimatorStateInfo(0).normalizedTime);

                // Apply the air and gravity forces onto the player.
                applyAir.Invoke(player, new object[] { false });
                applyGravity.Invoke(player, new object[] { });
            }

            // Check if we've reached the end of the tail swipe animation.
            if (player.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.95f)
            {
                // Check if we're not holding attack.
                if (!player.input.attackHold)
                {
                    // Check that we're on the ground still and swap to the grounded state if so.
                    if (player.onGround)
                    {
                        player.state = player.State_Ground;
                    }

                    // Swap to the in air state and the jump animation if we're in the air instead.
                    else
                    {
                        player.state = player.State_InAir;
                        player.SetPlayerAnimation("Jumping_Loop");
                    }
                }

                // Replay the attack sounds and reset the animation if we are still holding attack.
                else
                {
                    player.Action_PlayVoiceArray("HardAttack");
                    player.Action_PlaySound(player.sfxDivekick1);

                    if (player.onGround)
                        player.SetPlayerAnimation("TailSwipe", 0, null, true);
                    else
                        player.SetPlayerAnimation("TailSwipe_Air", 0, null, true);
                }
            }
        }

        /// <summary>
        /// Logic for Tails' wrench toss attack.
        /// </summary>
        public static void State_Tails_WrenchThrow()
        {
            player.displayMoveJump = "Jump";
            player.displayMoveAttack = "-";
            player.displayMoveSpecial = "Wrench Toss";
            player.displayMoveGuard = "-";

            // Check if we're on the ground.
            if (player.onGround)
            {
                // Jump if the player presses the jump button.
                if (player.input.jumpPress)
                    player.Action_SoftJump();

                // Apply the ground forces.
                applyGround.Invoke(player, new object[] { false });

                // Set our angle to the ground angle so we rotate with the terrain.
                player.angle = player.groundAngle;

                // Swap to the approriate wrench toss animation depending on our ground velocity.
                if (player.groundVel >= 3 || player.groundVel <= -3) player.SetPlayerAnimation("WrenchToss_Run", player.animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
                else player.SetPlayerAnimation("WrenchToss_Stand", player.animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            }

            // If we're in the air instead, then apply the air and gravity forces onto the player and swap to the aerial wrench toss animation.
            else
            {
                player.SetPlayerAnimation("WrenchToss_Air", player.animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
                applyAir.Invoke(player, new object[] { false });
                applyGravity.Invoke(player, new object[] { });
            }

            // Check if we've reached the end of the wrench toss animation.
            if (player.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.95f)
            {
                // Check if we're not holding special.
                if (!player.input.specialHold)
                {
                    // Check that we're on the ground still and swap to the grounded state if so.
                    if (player.onGround)
                    {
                        player.state = player.State_Ground;
                    }

                    // Swap to the in air state and the jump animation if we're in the air instead.
                    else
                    {
                        player.state = player.State_InAir;
                        player.SetPlayerAnimation("Jumping_Loop");
                    }
                }

                // Replay the attack sound and reset the animation if we are still holding special.
                else
                {
                    player.Action_PlaySound(player.sfxDivekick1);

                    if (player.onGround)
                    {
                        if (player.groundVel >= 3 || player.groundVel <= -3) player.SetPlayerAnimation("WrenchToss_Run", 0, null, true);
                        else player.SetPlayerAnimation("WrenchToss_Stand", 0, null, true);
                    }
                    else
                    {
                        player.SetPlayerAnimation("WrenchToss_Air", 0, null, true);
                    }
                }
            }
        }
        
        /// <summary>
        /// Hijack Neera's Freeze Ray for Tails' Wrench Toss, if only so it can be called from the animation directly.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FPPlayer), "Action_NeeraFreeze")]
        private static bool Action_Tails_WrenchThrow(FPPlayer __instance)
        {
            // Only do this if the the player that called Action_NeeraFreeze is using Tails' ID.
            if (__instance.characterID == tailsCharacterID)
            {
                // Play the Tail Swipe sound.
                player.Action_PlaySound(player.sfxDivekick1);

                // Create the projectile at the right offset with the right x velocity.
                ProjectileBasic projectileBasic;
                if (player.direction == FPDirection.FACING_RIGHT)
                {
                    projectileBasic = (ProjectileBasic)FPStage.CreateStageObject(ProjectileBasic.classID, player.position.x + 40, player.position.y);
                    projectileBasic.velocity.x = player.velocity.x + 12;
                }
                else
                {
                    projectileBasic = (ProjectileBasic)FPStage.CreateStageObject(ProjectileBasic.classID, player.position.x - 40, player.position.y);
                    projectileBasic.velocity.x = player.velocity.x - 12;
                }

                // Modify the velocity depending on if up or down are being held.
                if (player.input.up)
                {
                    projectileBasic.velocity.x /= 3f;
                    projectileBasic.velocity.y = 16f;
                }
                else if (player.input.down)
                {
                    projectileBasic.velocity.x /= 3f;
                    projectileBasic.velocity.y = -16f;
                }
                else
                {
                    projectileBasic.velocity.y = 1f;
                }

                // Set up the rest of the projectile's data.
                projectileBasic.animatorController = tailsAssetBundle.LoadAsset<RuntimeAnimatorController>("Wrench Animator");
                projectileBasic.animator = projectileBasic.GetComponent<Animator>();
                projectileBasic.animator.runtimeAnimatorController = projectileBasic.animatorController;
                projectileBasic.direction = player.direction;
                projectileBasic.explodeType = FPExplodeType.NONE;
                projectileBasic.parentObject = player;
                projectileBasic.faction = player.faction;
                projectileBasic.numberOfRebounds = 2;

                // Don't run the original function.
                return false;
            }

            // Run the original function if we're not Tails.
            return true;
        }

        /// <summary>
        /// Redirects any call to Carol's rolling attack stats (as the game sometimes calls it if the player is in the rolling animation) to Tails' if needed.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FPPlayer), "AttackStats_CarolRoll")]
        private static bool RedirectRollStats()
        {
            // If the player isn't Tails, then use the original Carol Roll stats.
            if (player.characterID != tailsCharacterID)
                return true;

            // If not, then redirect it to Tails' custom set.
            player.attackStats = AttackStats_Tails_Roll;
            return false;
        }
        
        /// <summary>
        /// Logic for detransforming out of Super Tails.
        /// </summary>
        private static void State_Tails_SuperDetransform()
        {
            // Kill the player velocity so the detransformation stops Tails in place.
            player.velocity = Vector2.zero;
            player.groundVel = 0;

            // Disable the Super flag.
            isSuper = false;

            // Check if the start timer is 0.
            if (SuperStartTimer == 0)
            {
                // Play the Super Detransformation sound.
                player.Action_PlaySoundUninterruptable(player.sfxCarolAttack3);

                // Set the player to the Jumping animation, specifically the looping part.
                player.SetPlayerAnimation("Jumping_Loop");

                // Shake the camera a bit.
                FPCamera.stageCamera.screenShake = Mathf.Max(FPCamera.stageCamera.screenShake, 10f);

                // Loop through four times.
                for (int sparkIndex = 0; sparkIndex < 4; sparkIndex++)
                {
                    // Create a spark.
                    Spark spark = (Spark)FPStage.CreateStageObject(Spark.classID, player.position.x, player.position.y);
                    spark.velocity.x = Mathf.Cos((float)Math.PI / 180f * ((float)sparkIndex * 90f + 45f)) * 20f;
                    spark.velocity.y = Mathf.Sin((float)Math.PI / 180f * ((float)sparkIndex * 90f + 45f)) * 20f;
                    spark.SetAngle();
                }

                // Play the Boost Breaker sound from Lilac's prefab.
                player.Action_PlaySoundUninterruptable(Plugin.lilacPrefab.GetComponent<FPPlayer>().sfxBoostExplosion);

                // Create the Boost Breaker explosion.
                BoostExplosion boostExplosion = (BoostExplosion)FPStage.CreateStageObject(BoostExplosion.classID, player.position.x, player.position.y);
                boostExplosion.attackKnockback.x = player.attackKnockback.x * 0.5f;
                boostExplosion.attackKnockback.y = player.attackKnockback.y * 0.5f;
                boostExplosion.attackEnemyInvTime = player.attackEnemyInvTime;
                boostExplosion.parentObject = player;
                boostExplosion.faction = player.faction;
            }

            // Increment our super timer by the stage's delta time.
            SuperStartTimer += FPStage.deltaTime;

            // Check if the timer has reached 60.
            if (SuperStartTimer >= 60)
            {
                // Reset the super start flags.
                createdSparks = false;

                // Remove the player's invincibility.
                player.invincibilityTime = 0f;
                player.flashTime = 0f;

                // Set the player to the InAir state.
                player.state = player.State_InAir;

                // Reset the player's top speed and jump strength.
                player.topSpeed = player.GetPlayerStat_Default_TopSpeed();
                player.jumpStrength = player.GetPlayerStat_Default_JumpStrength();

                // Reset the super start timer.
                SuperStartTimer = 0;

                player.topSpeed = player.GetPlayerStat_Default_TopSpeed();
                player.acceleration = player.GetPlayerStat_Default_Acceleration();
                player.airAceleration = player.GetPlayerStat_Default_AirAceleration();
                player.jumpStrength = player.GetPlayerStat_Default_JumpStrength();
                typeof(FPPlayer).GetField("speedMultiplier", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(player, 1f + (float)(int)player.potions[6] * 0.05f);
            }
        }

        /// <summary>
        /// Logic for when the player is Super Tails.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPPlayer), "Update")]
        private static void SuperTails()
        {
            // If the player isn't Tails, then don't do any of this.
            if (player.characterID != tailsCharacterID)
                return;

            // Don't proceed if the player isn't Super or is in the victory animation.
            if (!isSuper || player.state == player.State_Victory || player.state == State_Tails_SuperDetransform)
                return;

            // Increase the player's stats.
            player.topSpeed = player.GetPlayerStat_Default_TopSpeed() * 2f;
            player.acceleration = player.GetPlayerStat_Default_Acceleration() * 2f;
            player.airAceleration = player.GetPlayerStat_Default_AirAceleration() * 2f;
            player.jumpStrength = player.GetPlayerStat_Default_JumpStrength() * 1.2f;
            typeof(FPPlayer).GetField("speedMultiplier", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(player, 1f + (float)(int)player.potions[6] * 0.05f);
            player.attackStats = SetConstantAttackStats;

            // Reset the player's invincibility time to 200 so it can never expire.
            player.invincibilityTime = 200f;

            // Set the flash timer to 1200 if its reached 0 so the character flashes.
            if (player.flashTime <= 0)
                player.flashTime = 1200f;

            // Reset the player's heat and oxygen levels.
            player.heatLevel = 0f;
            player.oxygenLevel = 1f;

            // Force an attack hitbox to be active.
            player.hbAttack.enabled = true;
            player.hbAttack.visible = true;
            player.hbAttack.left = -32;
            player.hbAttack.right = 32;
            player.hbAttack.top = 32;
            player.hbAttack.bottom = -32;

            // Increment the super timer.
            superTimeCounter += Time.deltaTime;

            // Check if the super timer goes above 1.
            while (superTimeCounter >= 1f)
            {
                // Subtract 1 from the timer.
                superTimeCounter -= 1f;

                // Remove a crystal from the player.
                player.totalCrystals--;
            }

            // Check if the player has run out of crystals.
            if (player.totalCrystals <= 0)
            {
                // If the super music is playing, then play the last used audio we stored.
                if (FPAudio.GetCurrentMusic() == tailsSuperMusic)
                    FPAudio.PlayMusic(lastUsedAudio);

                // Reset the player's velocity.
                player.velocity = Vector2.zero;

                // Set the player's state to the Super Detransformation one.
                player.state = State_Tails_SuperDetransform;
            }
        }

        /// <summary>
        /// Force kills the player's velocity when they're in a scripted tube.
        /// </summary>
        public static void State_Scripted_Tube()
        {
            player.velocity.x = 0f;
            player.velocity.y = 0f;
        }

        public static void State_ScriptedTube_Dummy() => player.state = player.State_Ground;
        #endregion

        #region Attack Stats
        private static void AttackStats_Tails_Roll()
        {
            player.attackPower = 2f;
            player.attackHitstun = 1.5f;
            player.attackEnemyInvTime = 6f;
            SetConstantAttackStats();
        }

        private static void AttackStats_Tails_Flight()
        {
            player.attackPower = 2f;
            player.attackHitstun = 1.5f;
            player.attackEnemyInvTime = 6f;
            SetConstantAttackStats();
        }

        private static void AttackStats_Tails_TailSwipe()
        {
            player.attackPower = 4f;
            player.attackHitstun = 3f;
            player.attackEnemyInvTime = 6f;
            SetConstantAttackStats();
        }

        private static void SetConstantAttackStats()
        {
            player.attackKnockback.x = Mathf.Max(Mathf.Abs(player.prevVelocity.x * 1.5f), 6f);
            if (player.direction == FPDirection.FACING_LEFT) player.attackKnockback.x = 0f - player.attackKnockback.x;
            player.attackKnockback.y = player.prevVelocity.y * 0.5f;
            player.attackSfx = 7;
            player.attackPower *= player.GetAttackModifier();

            if (isSuper)
            {
                player.attackPower = 8f;
                player.attackHitstun = 2f;
                player.attackEnemyInvTime = 4f;
            }
        }
        #endregion

        /// <summary>
        /// Replaces the Airship in Bakunawa Chase with the Tornado 2.
        /// TODO: Put the custom sprite source https://www.spriters-resource.com/custom_edited/sonicthehedgehogcustoms/asset/18272/ into the README.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerShip), "Start")]
        private static void ModShip(PlayerShip __instance)
        {
            // If the player isn't Tails, then don't do any of this.
            if (FPSaveManager.character != tailsCharacterID)
                return;

            // Change the Airship's animator to the Tornado 2's.
            __instance.GetComponent<Animator>().runtimeAnimatorController = tailsAssetBundle.LoadAsset<RuntimeAnimatorController>("Tornado 2 Animator");

            // Shift the origin point for the bullets a bit.
            __instance.gameObject.transform.GetChild(0).gameObject.transform.localPosition = new(136f, 10.5f, 0f);

            // Scale down the sprite.
            __instance.gameObject.transform.GetChild(2).gameObject.transform.localScale = Vector3.one;

            // Hide the booster and tiny characters.
            __instance.gameObject.transform.GetChild(3).gameObject.SetActive(false);
            __instance.gameObject.transform.GetChild(4).gameObject.SetActive(false);
        }
    }
}
