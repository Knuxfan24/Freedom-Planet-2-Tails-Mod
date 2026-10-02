// TODO: Document this.
public class Coconuts : FPBaseEnemy
{
    public static int classID = -1;

    private float flashTime;

    private bool flashing;

    public FPHitBox hbAttack;
    private Renderer render;
    [HideInInspector]
    public Animator animator;
    [Header("Sfx Settings")]
    public AudioClip sfxKO;

    public int targetPoint;
    public float[] yTargets =
    [
        32,
        -48,
        16,
        -8,
        48,
        0
    ];
    private bool movingUp = true;
    private float genericTimer;

    public RuntimeAnimatorController projectileAnimator;

    private new void Start()
    {
        health = 1;

        hbWeakpoint.left = -32;
        hbWeakpoint.top = 56;
        hbWeakpoint.right = 48;
        hbWeakpoint.bottom = -32;
        hbWeakpoint.enabled = true;
        hbWeakpoint.visible = true;

        hbAttack.left = -16;
        hbAttack.top = 28;
        hbAttack.right = 24;
        hbAttack.bottom = -16;
        hbAttack.enabled = true;
        hbAttack.visible = true;

        activationRange.x = 320;
        useScaling = true;

        // Set this Coconuts' death sound and projectile animator.
        sfxKO = tailsAssetBundle.LoadAsset<AudioClip>("classic_pop");
        projectileAnimator = tailsAssetBundle.LoadAsset<RuntimeAnimatorController>("coconut projectile animator");

        state = State_Default;
        animator = GetComponent<Animator>();
        render = GetComponentInParent<Renderer>();
        base.Start();
        classID = FPStage.RegisterObjectType(this, GetType(), 0);
        objectID = classID;

        for (int i = 0; i < yTargets.Length; i++)
        {
            yTargets[i] += position.y;
        }

    }

    public override void ResetStaticVars()
    {
        base.ResetStaticVars();
        classID = -1;
    }

    private void Update()
    {
        if (invincibility > 0f)
        {
            invincibility -= FPStage.deltaTime;
        }
        if (state != null)
        {
            state();
        }
        if (direction == FPDirection.FACING_LEFT)
        {
            scale.x = 1f;
        }
        else
        {
            scale.x = -1f;
        }
    }

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

    private void State_Default()
    {
        FPBaseObject objRef = null;
        while (FPStage.ForEach(FPPlayer.classID, ref objRef))
        {
            FPPlayer fPPlayer = (FPPlayer)objRef;
            if (fPPlayer.position.x < position.x)
            {
                direction = FPDirection.FACING_LEFT;
            }
            if (fPPlayer.position.x > position.x)
            {
                direction = FPDirection.FACING_RIGHT;
            }
        }

        if (movingUp)
        {
            if (yTargets[targetPoint] > position.y)
            {
                animator.SetSpeed(1);
                velocity.y = 2;
            }
            else
            {
                if (targetPoint % 2 == 0)
                {
                    objRef = null;
                    while (FPStage.ForEach(FPPlayer.classID, ref objRef))
                    {
                        FPPlayer fPPlayer = (FPPlayer)objRef;
                        if (fPPlayer.position.x < position.x + 176 && fPPlayer.position.x > position.x - 176)
                        {
                            ThrowCoconut();
                        }
                    }
                }
                animator.SetSpeed(0);
                velocity.y = 0;
                genericTimer = 0;
                state = State_WaitToMove;
            }
        }
        else
        {
            if (yTargets[targetPoint] < position.y)
            {
                animator.SetSpeed(1);
                velocity.y = -2;
            }
            else
            {
                if (targetPoint % 2 == 0)
                {
                    objRef = null;
                    while (FPStage.ForEach(FPPlayer.classID, ref objRef))
                    {
                        FPPlayer fPPlayer = (FPPlayer)objRef;
                        if (fPPlayer.position.x < position.x + 176 && fPPlayer.position.x > position.x - 176)
                        {
                            ThrowCoconut();
                        }
                    }
                }
                animator.SetSpeed(0);
                velocity.y = 0;
                genericTimer = 0;
                state = State_WaitToMove;
            }
        }

        InteractWithObjects();
        Process360Movement();
    }

    private void ThrowCoconut()
    {
        animator.Play("Throw");
        ProjectileBasic projectileBasic;
        if (direction == FPDirection.FACING_LEFT)
        {
            projectileBasic = (ProjectileBasic)FPStage.CreateStageObject(ProjectileBasic.classID, position.x + 16, position.y + 16);
            projectileBasic.velocity.x = -2f;
            projectileBasic.direction = FPDirection.FACING_RIGHT;
        }
        else
        {
            projectileBasic = (ProjectileBasic)FPStage.CreateStageObject(ProjectileBasic.classID, position.x - 16, position.y + 16);
            projectileBasic.velocity.x = 2f;
            projectileBasic.direction = FPDirection.FACING_LEFT;
        }
        projectileBasic.velocity.y = 8;
        projectileBasic.animatorController = projectileAnimator;
        projectileBasic.animator = projectileBasic.GetComponent<Animator>();
        projectileBasic.animator.runtimeAnimatorController = projectileBasic.animatorController;
        projectileBasic.explodeType = FPExplodeType.NONE;
        projectileBasic.parentObject = this;
        projectileBasic.faction = faction;
    }

    private void State_WaitToMove()
    {
        genericTimer += FPStage.deltaTime;

        if (genericTimer >= 18)
        {
            animator.Play("Move");

            if (targetPoint == yTargets.Length - 1)
                targetPoint = 0;
            else
                targetPoint++;

            if (yTargets[targetPoint] > position.y)
                movingUp = true;
            else
                movingUp = false;

            state = State_Default;
        }
    }

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
