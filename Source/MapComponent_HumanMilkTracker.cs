using System.Collections.Generic;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Seeds the invisible HumanMilkTracker hediff onto every colonist and slave shortly
    /// after they appear on the map, so the Essential "No Human Milk" deprivation thought
    /// measures its seven-day grace window from when each pawn started being tracked
    /// rather than penalising brand-new pawns. Runs on a coarse hash interval; a hediff
    /// is only ever added once per pawn.
    /// </summary>
    public class MapComponent_HumanMilkTracker : MapComponent
    {
        private const int SeedIntervalTicks = 250;

        public MapComponent_HumanMilkTracker(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (MilkDefs.HumanMilkTracker == null)
            {
                return;
            }

            if (Find.TickManager.TicksGame % SeedIntervalTicks != 0)
            {
                return;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || !(pawn.IsColonist || pawn.IsSlave))
                {
                    continue;
                }

                HumanMilkConsumption.EnsureTracker(pawn);
            }
        }
    }
}