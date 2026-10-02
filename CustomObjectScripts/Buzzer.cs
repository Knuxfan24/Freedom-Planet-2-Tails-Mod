public class Buzzer : FPBaseEnemy
{
    private static int classID = -1;
    private float flashTime;
    private bool flashing;
    private FPHitBox hbAttack;
    private Animator animator;
    private AudioClip sfxKO;
    private Renderer render;

    private float targetX;
    private float genericTimer;
    private bool hasShot;
    private RuntimeAnimatorController projectileAnimator;

    private new void Start()
    {
        // Set this Buzzer's health to 1.
        health = 1;

        // Enable scaling so this Buzzer can be flipped if moving right.
        useScaling = true;

        // Set this Buzzer's hitboxes.
        hbWeakpoint.left = -64;
        hbWeakpoint.top = 32;
        hbWeakpoint.right = 64;
        hbWeakpoint.bottom = -32;
        hbWeakpoint.enabled = true;
        hbWeakpoint.visible = true;
        hbAttack.left = -48;
        hbAttack.top = 16;
        hbAttack.right = 48;
        hbAttack.bottom = -16;
        hbAttack.enabled = true;
        hbAttack.visible = true;

        // Increase this Buzzer's activation range.
        activationRange.x = 320;

        // Set this Buzzer's death sound and projectile animator.
        sfxKO = tailsAssetBundle.LoadAsset<AudioClip>("classic_pop");
        projectileAnimator = tailsAssetBundle.LoadAsset<RuntimeAnimatorController>("buzzer bullet animator");

        // Set this Buzzer to the default state.
        state = State_Default;

        // Get this Buzzer's animator and renderer.
        animator = GetComponent<Animator>();
        render = GetComponentInParent<Renderer>();

        // Handle the FPBaseEnemy stuff.
        base.Start();
        classID = FPStage.RegisterObjectType(this, GetType(), 0);
        objectID = classID;

        // Set this Buzzer's target position. They all spawn facing left so hardcoding this is fine.
        targetX = position.x - 512;
    }

    public override void ResetStaticVars()
    {
        base.ResetStaticVars();
        classID = -1;
    }

    private void Update()
    {
        if (invincibility > 0f)
            invincibility -= FPStage.deltaTime;

        state?.Invoke();

        // These sprites face left by default, so invert the usual scaling.
        if (direction == FPDirection.FACING_LEFT) scale.x = 1f;
        else scale.x = -1f;
    }

    // Based on the original code for the Turretus.
    private void InteractWithObjects()
    {
        FPBaseObject objRef = null;
        while (FPStage.ForEach(FPPlayer.classID, ref objRef))
        {
            FPPlayer fPPlayer = (FPPlayer)objRef;
            if (fPPlayer.invincibilityTime <= 0f && FPCollision.CheckOOBB(this, hbAttack, objRef, fPPlayer.hbTouch))
            {
                fPPlayer.healthDamage += 1f;
                fPPlayer.damageType = 4;
                fPPlayer.hurtKnockbackX = velocity.x * 0.7f;
                fPPlayer.hurtKnockbackY = 5f;
                fPPlayer.Action_HitSpark(this);
            }
        }

        switch (DamageCheck())
        {
            case 1:
                flashTime = 2f;
                break;
            case 2:
                flashTime = 2f;
                FPAudio.PlaySfx(sfxKO);
                state = State_Death;
                activationMode = FPActivationMode.ALWAYS_ACTIVE;
                invincibility = 20f;
                break;
            case 4:
                health = 1f;
                state = State_Frozen;
                animator.SetSpeed(0f);
                break;
        }
    }

    // Copied from the original code for the Turretus.
    private void ShaderUpdate()
    {
        if (!(this != null))
        {
            return;
        }
        if (flashTime > 0f && GetComponentInParent<Renderer>() != null)
        {
            flashTime -= FPStage.deltaTime;
            if (!flashing)
            {
                render.material = FPResources.material[1];
                flashing = true;
            }
        }
        else if (flashing && GetComponentInParent<Renderer>() != null)
        {
            render.material = FPResources.material[0];
            flashing = false;
        }
    }

    /// <summary>
    /// Handle this Buzzer's movement based on its direction.
    /// </summary>
    private void State_Default()
    {
        if (direction == FPDirection.FACING_LEFT)
        {
            // If we haven't reached our X target yet, then set our velocity to -2.
            if (position.x > targetX)
                velocity.x = -2f;

            // Check that we have reached our target.
            else
            {
                // Kill our velocity.
                velocity.x = 0;

                // Reset our timer.
                genericTimer = 0;

                // Swap to the idle animation (one without the thruster going off).
                animator.Play("Idle");

                // Sawp to the waiting state.
                state = State_WaitingToTurn;
            }

            // Loop through and find a player in range to shoot at.
            FPBaseObject objRef = null;
            while (FPStage.ForEach(FPPlayer.classID, ref objRef))
            {
                FPPlayer fPPlayer = (FPPlayer)objRef;
                if (fPPlayer.position.x > position.x - 128 && fPPlayer.position.x < position.x && !hasShot)
                {
                    velocity.x = 0;
                    genericTimer = 0;
                    animator.Play("Shoot");
                    state = State_WaitToShoot;
                }
            }
        }
        else // The comments for the Facing Left block apply here, just with the X values flipped around a bit.
        {
            if (position.x < targetX)
                velocity.x = 2f;
            else
            {
                velocity.x = 0;
                genericTimer = 0;
                animator.Play("Idle");
                state = State_WaitingToTurn;
            }

            FPBaseObject objRef = null;
            while (FPStage.ForEach(FPPlayer.classID, ref objRef))
            {
                FPPlayer fPPlayer = (FPPlayer)objRef;
                if (fPPlayer.position.x < position.x + 128 && fPPlayer.position.x > position.x && !hasShot)
                {
                    velocity.x = 0;
                    genericTimer = 0;
                    animator.Play("Shoot");
                    state = State_WaitToShoot;
                }
            }
        }

        InteractWithObjects();
        ShaderUpdate();
        Process360Movement();
    }

    private void State_WaitToShoot()
    {
        // Change the hitbox to match the different shape of the shooting sprite.
        hbWeakpoint.left = -64;
        hbWeakpoint.top = 32;
        hbWeakpoint.right = 32;
        hbWeakpoint.bottom = -64;
        hbAttack.left = -48;
        hbAttack.top = 16;
        hbAttack.right = 32;
        hbAttack.bottom = -48;

        // Increment this Buzzer's generic timer.
        genericTimer += FPStage.deltaTime;

        // Check if this Buzzer's timer has reached 30.
        if (genericTimer >= 30)
        {
            // Create a projectile.
            ProjectileBasic projectileBasic;
            if (direction == FPDirection.FACING_LEFT)
            {
                projectileBasic = (ProjectileBasic)FPStage.CreateStageObject(ProjectileBasic.classID, position.x + 16, position.y - 48);
                projectileBasic.velocity.x = -2f;
                projectileBasic.direction = FPDirection.FACING_RIGHT;
            }
            else
            {
                projectileBasic = (ProjectileBasic)FPStage.CreateStageObject(ProjectileBasic.classID, position.x - 16, position.y - 48);
                projectileBasic.velocity.x = 2f;
                projectileBasic.direction = FPDirection.FACING_LEFT;
            }
            projectileBasic.velocity.y = -2;
            projectileBasic.animatorController = projectileAnimator;
            projectileBasic.animator = projectileBasic.GetComponent<Animator>();
            projectileBasic.animator.runtimeAnimatorController = projectileBasic.animatorController;
            projectileBasic.explodeType = FPExplodeType.NONE;
            projectileBasic.parentObject = this;
            projectileBasic.faction = faction;
            projectileBasic.ignoreTerrain = true;

            // Reset our timer.
            genericTimer = 0;

            // Swap to the Has Shot state.
            state = State_HasShot;
        }
    }

    private void State_HasShot()
    {
        // Increment this Buzzer's generic timer.
        genericTimer += FPStage.deltaTime;

        // Check if this Buzzer's timer has reached 20.
        if (genericTimer >= 20)
        {
            // Reset the hitboxes to normal.
            hbWeakpoint.left = -64;
            hbWeakpoint.top = 32;
            hbWeakpoint.right = 64;
            hbWeakpoint.bottom = -32;
            hbAttack.left = -48;
            hbAttack.top = 16;
            hbAttack.right = 48;
            hbAttack.bottom = -16;

            // Set the hasShot flag.
            hasShot = true;

            // Return to the moving animation and default state.
            animator.Play("Moving");
            state = State_Default;
        }
    }

    private void State_WaitingToTurn()
    {
        // Increment this Buzzer's generic timer.
        genericTimer += FPStage.deltaTime;

        // Check if this Buzzer's timer has reached 15.
        if (genericTimer >= 15)
        {
            // Swap our direction and target X position.
            if (direction == FPDirection.FACING_LEFT)
            {
                direction = FPDirection.FACING_RIGHT;
                targetX = position.x + 512;
            }
            else
            {
                direction = FPDirection.FACING_LEFT;
                targetX = position.x - 512;
            }

            // Reset the hasShot flag so we can shoot again on the next pass.
            hasShot = false;

            // Return to the moving animation and default state.
            animator.Play("Moving");
            state = State_Default;
        }
    }

    // Copied from the original code for the Turretus.
    public void State_Frozen()
    {
        if (!frozen && freezeTimer > 0f)
        {
            iceBlockBack = Object.Instantiate(FPResources.childSprite[1]);
            iceBlockBack.parentObject = this;
            iceBlockBack.yOffset = 6f;
            iceBlock = Object.Instantiate(FPResources.childSprite[0]);
            iceBlock.parentObject = this;
            iceBlock.yOffset = 6f;
            iceBlock.GetComponent<SpriteRenderer>().sortingOrder = 3;
            frozen = true;
        }
        InteractWithObjects();
        ShaderUpdate();
    }

    private void State_Death()
    {
        FPStage.CreateStageObject(Explosion.classID, position.x, position.y);
        FPStage.DestroyStageObject(this);
    }
}
