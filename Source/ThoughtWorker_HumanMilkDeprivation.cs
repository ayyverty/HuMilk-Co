using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Active for pawns holding the Essential "Human Milk Consumption" precept who have
    /// not consumed any human milk in the past seven days. For pawns that have never
    /// consumed any, the clock runs from when tracking began for that pawn, so fresh
    /// colonists get a full grace window before the thought appears. Clears as soon as
    /// the pawn drinks or eats human milk: the worker re-reads the tracker every
    /// evaluation.
    /// </summary>
    public class ThoughtWorker_HumanMilkDeprivation : ThoughtWorker_Precept
    {
        private const int DeprivationGraceDays = 7;

        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            if (p == null || p.health == null || p.health.hediffSet == null || MilkDefs.HumanMilkTracker == null)
            {
                return ThoughtState.Inactive;
            }

            Hediff_HumanMilkTracker tracker =
                p.health.hediffSet.GetFirstHediffOfDef(MilkDefs.HumanMilkTracker) as Hediff_HumanMilkTracker;
            if (tracker == null)
            {
                return ThoughtState.Inactive;
            }

            // Anchor from the last consumption, or from when tracking began. A tracker with
            // no stamp (e.g. loaded from an old save) counts as freshly tracked so it gets
            // the full grace window instead of appearing immediately.
            int trackingStart = tracker.trackingStartTick > 0 ? tracker.trackingStartTick : GenTicks.TicksGame;
            int anchorTick = tracker.lastConsumedTick >= 0 ? tracker.lastConsumedTick : trackingStart;
            if (GenTicks.TicksGame - anchorTick < GenDate.TicksPerDay * DeprivationGraceDays)
            {
                return ThoughtState.Inactive;
            }

            return ThoughtState.ActiveDefault;
        }
    }
}