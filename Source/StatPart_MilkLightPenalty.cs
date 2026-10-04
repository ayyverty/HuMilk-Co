using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Registered on the MoveSpeed stat at load time. Applies a -10% move speed factor
    /// while the drinker is affected by Dirtmole milk and stands unroofed in daylight,
    /// mirroring the dirtmoles' lore of being slow in the open.
    /// </summary>
    public class StatPart_MilkLightPenalty : StatPart
    {
        public const float MoveSpeedFactor = 0.9f;

        public const float MinSkyGlowForDaylight = 0.5f;

        public static readonly StatPart_MilkLightPenalty Instance = new StatPart_MilkLightPenalty();

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing && req.Thing is Pawn pawn && ActiveFor(pawn))
            {
                val *= MoveSpeedFactor;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Pawn pawn && ActiveFor(pawn))
            {
                return "Dirtmole milk light sensitivity: x" + MoveSpeedFactor.ToStringPercent();
            }

            return null;
        }

        private static bool ActiveFor(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null || !pawn.RaceProps.Humanlike)
            {
                return false;
            }

            if (pawn.Position.Roofed(pawn.Map) || pawn.Map.skyManager.CurSkyGlow < MinSkyGlowForDaylight)
            {
                return false;
            }

            return MilkDefs.DirtmoleEffect != null &&
                pawn.health != null && pawn.health.hediffSet != null &&
                pawn.health.hediffSet.HasHediff(MilkDefs.DirtmoleEffect);
        }
    }
}