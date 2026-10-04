using System.Collections.Generic;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Applies a milk effect hediff non-stacking: any existing instance of the same
    /// hediff is removed first, then a fresh one is added at full severity. Re-drinking
    /// resets the timer instead of intensifying; different milks' effects coexist.
    /// </summary>
    public static class MilkEffectUtility
    {
        public static void ApplyEffect(Pawn pawn, HediffDef effect)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null || effect == null)
            {
                return;
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                if (hediffs[i].def == effect)
                {
                    pawn.health.RemoveHediff(hediffs[i]);
                }
            }

            Hediff hediff = HediffMaker.MakeHediff(effect, pawn);
            hediff.Severity = 1f;
            pawn.health.AddHediff(hediff);
        }
    }
}