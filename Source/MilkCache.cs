using System.Collections.Generic;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Interval-rebuilt cache of pawns that are currently automatic-milking candidates
    /// (target-side only, no milker context). Rebuilding every few ticks catches spawn,
    /// despawn, hediff, designation, and fullness changes without event hooks. Consumers
    /// must still re-validate against the milker (map, skill, reservations) before use.
    /// </summary>
    public static class MilkCache
    {
        private const int RebuildInterval = 120;

        private static readonly List<Pawn> cachedCandidates = new List<Pawn>();
        private static int lastRebuildTick = -1;

        public static List<Pawn> Candidates
        {
            get
            {
                RebuildIfNeeded();
                return cachedCandidates;
            }
        }

        private static void RebuildIfNeeded()
        {
            if (Find.TickManager == null)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (lastRebuildTick >= 0 && now - lastRebuildTick < RebuildInterval)
            {
                return;
            }

            lastRebuildTick = now;
            cachedCandidates.Clear();

            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                Map map = maps[m];
                if (map == null || map.mapPawns == null)
                {
                    continue;
                }

                IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (MilkHelper.IsMilkingCandidate(pawn))
                    {
                        cachedCandidates.Add(pawn);
                    }
                }
            }
        }
    }
}