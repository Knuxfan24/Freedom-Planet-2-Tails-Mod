namespace Freedom_Planet_2_Tails_Mod.CustomObjectScripts
{
    internal class JetAnkletTrail : MonoBehaviour
    {
        private SpriteRenderer sprite;

        // Get the Sprite Renderer.
        private void Start() => sprite = GetComponent<SpriteRenderer>();

        private void Update()
        {
            // Remove some of the alpha value.
            sprite.color = new(0.56f, 0.76f, 1f, (float)(sprite.color.a - (0.075 * FPStage.deltaTime)));

            // Destroy the trail once its alpha value has reached 0.
            if (sprite.color.a <= 0)
                UnityEngine.GameObject.Destroy(this);
        }
    }
}
