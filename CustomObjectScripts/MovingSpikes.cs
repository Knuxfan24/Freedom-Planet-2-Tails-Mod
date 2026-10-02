// TODO: Document this.
// TODO: Add the sound.
// TODO: Accurate timing.
namespace Freedom_Planet_2_Tails_Mod.CustomObjectScripts
{
    internal class MovingSpikes : FPBaseObject
    {
        private static int classID = -1;
        private FPObjectState state;
        private float genericTimer = 0f;
        private bool isUp = true;
        private float targetY;

        private new void Start()
        {
            state = State_Idle;
            base.Start();
            classID = FPStage.RegisterObjectType(this, GetType(), 256);
            objectID = classID;
            targetY = position.y - 64;
        }

        private void Update()
        {
            // Invoke the current state if it isn't null.
            state?.Invoke();
        }

        private void State_Idle()
        {
            genericTimer += FPStage.deltaTime;

            if (genericTimer >= 60f)
            {
                state = State_Moving;
            }
        }

        private void State_Moving()
        {
            if (position.y != targetY)
            {
                if (isUp) position.y -= 16f;
                else position.y += 16f;
            }
            else
            {
                if (isUp) targetY = position.y + 64;
                else targetY = position.y - 64;

                isUp = !isUp;
                genericTimer = 0;
                state = State_Idle;
            }
        }
    }
}
