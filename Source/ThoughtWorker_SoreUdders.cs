using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Active while a Hucow has not been milked in the past day. Clears as soon as the
    /// pawn is milked again: the worker re-reads lastMilkedTick on every evaluation.
    /// </summary>
    public class ThoughtWorker_SoreUdders : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!MilkHelper.IsHucow(p))
            {
                return ThoughtState.Inactive;
            }

            if (p.health?.hediffSet == null || MilkDefs.MilkingProgress == null)
            {
                return ThoughtState.Inactive;
            }

            Hediff_MilkingProgress progress =
                p.health.hediffSet.GetFirstHediffOfDef(MilkDefs.MilkingProgress) as Hediff_MilkingProgress;

            // No recorded milking history (e.g. trait granted outside the mod) means the
            // pawn has not been milked recently enough, so it counts as sore.
            if (progress == null || GenTicks.TicksGame - progress.lastMilkedTick >= GenDate.TicksPerDay)
            {
                return ThoughtState.ActiveDefault;
            }

            return ThoughtState.Inactive;
        }
    }
}