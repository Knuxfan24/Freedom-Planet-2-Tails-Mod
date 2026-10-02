using Freedom_Planet_2_Tails_Mod.CustomObjectScripts;
using System.Linq;
using UnityEngine.SceneManagement;

namespace Freedom_Planet_2_Tails_Mod.Patchers
{
    internal class FPStagePatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPStage), "Start")]
        private static void EmeraldHillScripts()
        {
            // Only do this if we're in Emerald Hill or the Tutorial.
            if (SceneManager.GetActiveScene().name != "EmeraldHill" && SceneManager.GetActiveScene().name != "EmeraldHillTutorial")
                return;

            // Add the signpost.
            var sign = UnityEngine.GameObject.Find("ehz_signpost_0").AddComponent<SignPost>();
            sign.signSound = tailsAssetBundle.LoadAsset<AudioClip>("classic_signpost");

            // Add the moving spikes script to that one set.
            if (SceneManager.GetActiveScene().name == "EmeraldHill")
                UnityEngine.GameObject.Find("Spikes Up (1)").AddComponent<MovingSpikes>();

            // Add the Badniks.
            GameObject[] mashers = [.. UnityEngine.Object.FindObjectsOfType<GameObject>().Where(x => x.name.StartsWith("Masher"))];
            GameObject[] buzzers = [.. UnityEngine.Object.FindObjectsOfType<GameObject>().Where(x => x.name.StartsWith("Buzzer"))];
            GameObject[] coconuts = [.. UnityEngine.Object.FindObjectsOfType<GameObject>().Where(x => x.name.StartsWith("Coconuts"))];

            foreach (var masher in mashers) masher.AddComponent<Masher>();
            foreach (var buzzer in buzzers) buzzer.AddComponent<Buzzer>();
            foreach (var coconut in coconuts) coconut.AddComponent<Coconuts>();

            // Add the Zoom Tubes that act as the Corkscrews.
            int tubeIndex = 0;
            CreateTube
            (
                new(9216, -1344, 0),
                [
                    new(9312, -1326),
                    new(9408, -1280),
                    new(9504, -1234),
                    new(9600, -1216),
                    new(9696, -1234),
                    new(9792, -1280),
                    new(9888, -1326),
                    new(9984, -1344)
                ],
                7.5f
            );
            CreateTube
            (
                new(9984, -1344, 0),
                [
                    new(9888, -1326),
                    new(9792, -1280),
                    new(9696, -1234),
                    new(9600, -1216),
                    new(9504, -1234),
                    new(9408, -1280),
                    new(9312, -1326),
                    new(9216, -1344)
                ],
                -7.5f,
                ZoomTubeExitDirection.Left
            );
            CreateTube
            (
                new(18432, -1344, 0),
                [
                    new(18528, -1326),
                    new(18624, -1280),
                    new(18720, -1234),
                    new(18816, -1216),
                    new(18912, -1234),
                    new(19008, -1280),
                    new(19104, -1326),
                    new(19200, -1344),
                    new(19296, -1326),
                    new(19392, -1280),
                    new(19488, -1234),
                    new(19584, -1216),
                    new(19680, -1234),
                    new(19776, -1280),
                    new(19872, -1326),
                    new(19968, -1344)
                ],
                7.5f
            );
            CreateTube
            (
                new(19968, -1344, 0),
                [
                    new(19872, -1326),
                    new(19776, -1280),
                    new(19680, -1234),
                    new(19584, -1216),
                    new(19488, -1234),
                    new(19392, -1280),
                    new(19296, -1326),
                    new(19200, -1344),
                    new(19104, -1326),
                    new(19008, -1280),
                    new(18912, -1234),
                    new(18816, -1216),
                    new(18720, -1234),
                    new(18624, -1280),
                    new(18528, -1326),
                    new(18432, -1344)
                ],
                -7.5f,
                ZoomTubeExitDirection.Left
            );

            void CreateTube(Vector3 position, Vector2[] points, float requiredVelocity = 0, ZoomTubeExitDirection exitDirection = ZoomTubeExitDirection.Right)
            {
                GameObject tube = new($"ScriptedTube{tubeIndex}");
                tubeIndex++;
                tube.transform.position = position;
                ScriptedTube script = tube.AddComponent<ScriptedTube>();
                script.activationMode = FPActivationMode.ALWAYS_ACTIVE;
                script.points = points;
                script.requiredXVelocity = requiredVelocity;
                script.exitDirection = exitDirection;
            }
        }
    }
}
