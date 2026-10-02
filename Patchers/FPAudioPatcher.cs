using System;

namespace Freedom_Planet_2_Tails_Mod.Patchers
{
    internal class FPAudioPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPAudio), nameof(FPAudio.PlayMusic), new Type[] { typeof(AudioClip), typeof(float) })]
        private static void GetLastTrack(ref AudioClip bgmMusic)
        {
            // If there isn't actually any audio being made to play, then return.
            if (bgmMusic == null)
                return;

            // Check that the audio being made to play doesn't match what we've previously saved and isn't the super theme then lastUsedAudio to the audio being made to play.
            if (bgmMusic != lastUsedAudio && bgmMusic != tailsSuperMusic)
                lastUsedAudio = bgmMusic;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPAudio), nameof(FPAudio.PlayMusic), new Type[] { typeof(AudioClip), typeof(float) })]
        private static void CustomMusicLoopSet(ref AudioClip bgmMusic, ref float ___loopStart, ref float ___loopEnd)
        {
            // Check that the play music function has a bgm assigned.
            if (bgmMusic != null)
            {
                // Check that the assigned bgm is our results theme.
                if (bgmMusic == tailsResultsMusic)
                {
                    // Set the loop points for the results theme.
                    ___loopStart = 2.427f;
                    ___loopEnd = 35.923f;
                }

                // Check that the assigned bgm is Emerald Hill's map theme.
                if (bgmMusic == tailsEHZMapMusic)
                {
                    // Set the loop points for Emerald Hill's map theme.
                    ___loopStart = 3.702f;
                    ___loopEnd = 39.745f;
                }

                // Check that the assigned bgm is Emerald Hill's theme.
                if (bgmMusic == tailsEHZMusic)
                {
                    // Set the loop points for Emerald Hill's theme.
                    ___loopStart = 6.964f;
                    ___loopEnd = 48.498f;
                }

                // Check that the assigned bgm is Super Tails' theme.
                if (bgmMusic == tailsSuperMusic)
                {
                    // Set the loop points for Super Tails' theme.
                    ___loopStart = 1.792f;
                    ___loopEnd = 29.619f;
                }
            }
        }
    }
}
