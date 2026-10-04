using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Attached to each xenotype milk. Looks up the milk's effect hediff from the
    /// <see cref="MilkXenotypeDef"/> registry and applies it non-stacking. Also reports
    /// the effect's stats on the milk's info card.
    /// </summary>
    public class IngestionOutcomeDoer_MilkEffect : IngestionOutcomeDoer
    {
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            if (ingested == null)
            {
                return;
            }

            MilkEffectUtility.ApplyEffect(pawn, MilkDefs.GetMilkEffect(ingested.def));
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(ThingDef parentDef)
        {
            HediffDef effect = parentDef != null ? MilkDefs.GetMilkEffect(parentDef) : null;
            if (effect == null)
            {
                yield break;
            }

            foreach (StatDrawEntry entry in effect.SpecialDisplayStats(StatRequest.ForEmpty()))
            {
                yield return entry;
            }
        }
    }
}