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
            // Set this spike set to the idle state.
            state = State_Idle;

            // Handle the FPBaseEnemy stuff.
            base.Start();
            classID = FPStage.RegisterObjectType(this, GetType(), 256);
            objectID = classID;

            // Set this spike set's target Y value.
            targetY = position.y - 64;
        }

        // Invoke the current state if it isn't null.
        private void Update() => state?.Invoke();

        /// <summary>
        /// Swaps to the moving state after 60 frames have passed.
        /// </summary>
        private void State_Idle()
        {
            genericTimer += FPStage.deltaTime;

            if (genericTimer >= 60f)
                state = State_Moving;
        }

        private void State_Moving()
        {
            // Move this spike set up or down if its not at the target Y position.
            if (position.y != targetY)
            {
                if (isUp) position.y -= 16f * FPStage.deltaTime;
                else position.y += 16f * FPStage.deltaTime;
            }

            // Update the target Y position, flip the isUp movement flag, reset the timer and return to the idle state.
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
