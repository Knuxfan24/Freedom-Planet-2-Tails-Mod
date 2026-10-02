using System;

namespace Freedom_Planet_2_Tails_Mod.Patchers
{
    internal class RestoreTailsEnergy
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPPlayer), "LateUpdate")]
        private static void RestoreEnergyOnSpring(FPPlayer __instance)
        {
            if (__instance.characterID == tailsCharacterID && __instance.currentAnimation == "Spring" && __instance.energy < 100f)
                __instance.energy = 100f;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPPlayer), "State_LadderClimb")]
        [HarmonyPatch(typeof(FPPlayer), "State_Hanging")]
        [HarmonyPatch(typeof(FPPlayer), "State_GrindRail")]
        [HarmonyPatch(typeof(FPPlayer), "PseudoGrindRail")]
        private static void RestoreEnergyGeneric(FPPlayer __instance)
        {
            if (__instance.characterID == tailsCharacterID)
                __instance.energy = 100f;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPPlayer), "State_Guard")]
        private static void RestoreEnergyOnGuard(FPPlayer __instance)
        {
            if (__instance.characterID == tailsCharacterID && __instance.onGround)
                __instance.energy = 100f;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BarLift), "Mode_DV")]
        [HarmonyPatch(typeof(BFTeleporter), "State_Teleport")]
        [HarmonyPatch(typeof(GOMirror), "State_Teleport")]
        [HarmonyPatch(typeof(HamsterBall), "State_Closing")]
        [HarmonyPatch(typeof(PinballFlipper), "Update")]
        [HarmonyPatch(typeof(SAWaterWheel), "Update")]
        [HarmonyPatch(typeof(TFGrapple), "Update")]
        private static void RestoreEnergyOnObjects(ref FPPlayer ___targetPlayer)
        {
            if (___targetPlayer != null)
            {
                if (___targetPlayer.characterID == tailsCharacterID)
                    ___targetPlayer.energy = 100f;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GBBoosterTrail), "Update")]
        private static void GravityBubbleBoosters(GBBoosterTrail __instance, ref FPHitBox ___hbTouch, ref SpriteRenderer[] ___segments)
        {
            FPBaseObject objRef = null;
            while (FPStage.ForEach(FPPlayer.classID, ref objRef))
            {
                FPPlayer fPPlayer = (FPPlayer)objRef;
                for (int j = 0; j < __instance.totalSegments; j += 4)
                {
                    float num = __instance.angle + (float)j * __instance.tilt;
                    float num2 = Mathf.Abs(Mathf.Cos((float)Math.PI / 180f * num) * __instance.segmentLength * 0.25f) + __instance.segmentLength * 0.25f + 8f;
                    float num3 = Mathf.Abs(Mathf.Sin((float)Math.PI / 180f * num) * __instance.segmentLength * 0.25f) + __instance.segmentLength * 0.25f + 8f;
                    ___hbTouch.left = ___segments[j].transform.localPosition.x - num2;
                    ___hbTouch.right = ___segments[j].transform.localPosition.x + num2;
                    ___hbTouch.bottom = ___segments[j].transform.localPosition.y - num3;
                    ___hbTouch.top = ___segments[j].transform.localPosition.y + num3;
                    if (FPCollision.CheckOOBB(__instance, ___hbTouch, objRef, fPPlayer.hbTouch))
                    {
                        fPPlayer.energy = 100;
                    }
                }
            }
        }
    }
}
