using System;

namespace Freedom_Planet_2_Tails_Mod.CustomObjectScripts
{
    internal class DummyRingCapsule : FPBaseObject
    {
        private FPObjectState state;
        private FPHitBox hbItem;
        private RuntimeAnimatorController projectileAnimator;

        private new void Start()
        {
            // Set up the Hitbox for this Dummy Ring Bomb.
            hbItem.enabled = true;
            hbItem.visible = true;
            hbItem.left = -16;
            hbItem.top = 16;
            hbItem.right = 16;
            hbItem.bottom = -16;

            // Switch to the idle state.
            state = State_Idle;

            // Set the needed values in the FPBaseObject for collision checking.
            enablePhysics = true;
            terrainCollision = true;

            // Load the animator for the Rings themselves.
            projectileAnimator = tailsAssetBundle.LoadAsset<RuntimeAnimatorController>("Ring Bomb Animator");

            // Run the base object setup.
            base.Start();
        }

        private void Update()
        {
            // Invoke the current state if it isn't null.
            state?.Invoke();
        }

        private void State_Idle()
        {
            // Handle moving this Dummy Ring Bomb.
            Process360Movement();
            if (!onGround)
            {
                position.y += velocity.y * FPStage.deltaTime;
                velocity.y -= 0.375f * FPStage.deltaTime;
            }

            // Check if this Dummy Ring Bomb has hit an enemy.
            foreach (FPBaseEnemy fpbaseEnemy in FPStage.GetActiveEnemies(false, false))
            {
                if (FPCollision.CheckAABB(this, hbItem, fpbaseEnemy, fpbaseEnemy.hbWeakpoint) || FPCollision.CheckTerrainCircleThroughPlatforms(this, 12f, false))
                {
                    Hit();
                    return;
                }
            }

            // Check if this Dummy Ring Bomb has hit any terrain.
            if (FPCollision.CheckTerrainCircleThroughPlatforms(this, 12f, false))
            {
                Hit();
                return;
            }

            // Handle spawning the Dummy Rings.
            void Hit()
            {
                // Play the ring loss sound.
                FPAudio.PlaySfx(tailsAssetBundle.LoadAsset<AudioClip>("00_bomb_ring"));

                // Create 7 Dummy Rings. Based on the Item Box releasing Crystal Shards.
                float num;
                for (num = 22.5f; num <= 157.5f; num += 135f / (float)(7 - 1))
                {
                    ProjectileBasic projectileBasic;
                    projectileBasic = (ProjectileBasic)FPStage.CreateStageObject(ProjectileBasic.classID, position.x, position.y + 16);
                    projectileBasic.velocity.x = Mathf.Cos((base.transform.eulerAngles.z + num) * ((float)Math.PI / 180f)) * 2;
                    projectileBasic.velocity.y = Mathf.Sin((base.transform.eulerAngles.z + num) * ((float)Math.PI / 180f)) * 2;
                    projectileBasic.animatorController = projectileAnimator;
                    projectileBasic.animator = projectileBasic.GetComponent<Animator>();
                    projectileBasic.animator.runtimeAnimatorController = projectileBasic.animatorController;
                    projectileBasic.direction = direction;
                    projectileBasic.explodeType = FPExplodeType.NONE;
                    projectileBasic.parentObject = this;
                    projectileBasic.faction = FPPlayerPatcher.player.faction;
                    projectileBasic.numberOfRebounds = 999;
                }

                // Destroy this Dummy Ring Bomb.
                FPStage.CreateStageObject(Explosion.classID, position.x, position.y);
                FPStage.DestroyStageObject(this);
            }
        }
    }
}
