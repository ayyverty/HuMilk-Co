using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Central entry point for the "Human Milk Consumption" ideology precept. Detects
    /// whether a food def is any kind of HuMilk Co human milk, records the
    /// <c>ConsumedHumanMilk</c> history event (which drives the precept's memory
    /// thoughts), and seeds the invisible tracker hediff that backs the Essential
    /// option's "No Human Milk" deprivation thought.
    /// </summary>
    public static class HumanMilkConsumption
    {
        private static List<ThingDef> knownMilkDefs;
        private static List<ThingDef> observedThingDefs;

        /// <summary>
        /// True if the def is any human milk this mod produces: the default MilkHuman
        /// plus every mapped xenotype milk.
        /// </summary>
        public static bool IsHumanMilk(ThingDef def)
        {
            if (def == null)
            {
                return false;
            }

            // AllDefsListForReading is a stable instance between def reloads and a new
            // instance after one, so reference identity invalidates the cache exactly
            // then (dev-mode reloads) without paying per-call cost.
            List<ThingDef> thingDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            if (observedThingDefs != thingDefs)
            {
                observedThingDefs = thingDefs;
                knownMilkDefs = MilkDefs.AllMilkDefs();
            }

            return knownMilkDefs.Contains(def);
        }

        /// <summary>
        /// Records that the pawn consumed human milk: stamps the tracker hediff and
        /// fires the <c>ConsumedHumanMilk</c> history event so the precept's memory
        /// thoughts are granted to believers. Safe to call every time an ingredient is
        /// scanned; the memory thoughts stack-limit to one.
        /// </summary>
        public static void RecordConsumed(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            Hediff_HumanMilkTracker tracker = EnsureTracker(pawn);
            if (tracker != null)
            {
                tracker.lastConsumedTick = GenTicks.TicksGame;
            }

            if (MilkDefs.ConsumedHumanMilk != null && Find.HistoryEventsManager != null)
            {
                Find.HistoryEventsManager.RecordEvent(
                    new HistoryEvent(MilkDefs.ConsumedHumanMilk, pawn.Named(HistoryEventArgsNames.Doer)));
            }
        }

        /// <summary>
        /// Returns the pawn's human milk tracker hediff, creating it if missing (and
        /// available). The created hediff stamps <c>trackingStartTick</c> as now, which
        /// is what gives never-consumed pawns their grace window.
        /// </summary>
        public static Hediff_HumanMilkTracker EnsureTracker(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null || MilkDefs.HumanMilkTracker == null)
            {
                return null;
            }

            Hediff_HumanMilkTracker tracker =
                pawn.health.hediffSet.GetFirstHediffOfDef(MilkDefs.HumanMilkTracker) as Hediff_HumanMilkTracker;
            if (tracker != null)
            {
                // Backfill trackers from old saves (or created before trackingStartTick was
                // stamped) so the deprivation grace window runs from now, not from tick 0.
                if (tracker.trackingStartTick <= 0)
                {
                    tracker.trackingStartTick = GenTicks.TicksGame;
                }

                return tracker;
            }

            tracker = pawn.health.AddHediff(MilkDefs.HumanMilkTracker) as Hediff_HumanMilkTracker;
            if (tracker != null)
            {
                tracker.trackingStartTick = GenTicks.TicksGame;
            }

            return tracker;
        }
    }
}