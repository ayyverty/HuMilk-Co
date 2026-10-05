using System.Collections.Generic;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Interval-rebuilt cache of pawns that are currently automatic-milking candidates
    /// (target-side only, no milker context). Each map is re-scanned on a round-robin
    /// basis, at most once per <see cref="RebuildInterval"/> ticks, which catches spawn,
    /// despawn, hediff, designation, and fullness changes without event hooks while
    /// spreading the scan cost across ticks instead of doing every map every interval.
    /// Consumers must still re-validate against the milker (map, skill, reservations).
    /// </summary>
    public static class MilkCache
    {
        private const int RebuildInterval = 120;
        private const int PruneInterval = RebuildInterval * 8;

        public struct MilkEntry
        {
            public Pawn pawn;
            public bool full;
            public float availableMilk;
        }

        private static readonly Dictionary<int, List<MilkEntry>> mapEntries = new Dictionary<int, List<MilkEntry>>();
        private static readonly Dictionary<int, int> mapLastRebuildTick = new Dictionary<int, int>();
        private static int nextMapIndex;

        /// <summary>
        /// All currently-full target-eligible pawns across every map. Cheap per-milker
        /// filtering (map, skill, self) is still required before use.
        /// </summary>
        public static IEnumerable<Pawn> Candidates
        {
            get
            {
                RebuildIfNeeded();
                foreach (KeyValuePair<int, List<MilkEntry>> pair in mapEntries)
                {
                    List<MilkEntry> list = pair.Value;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].full)
                        {
                            yield return list[i].pawn;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the cached target-side milk data for <paramref name="pawn"/> (may be
        /// up to <see cref="RebuildInterval"/> ticks stale; consumers that need a fresh
        /// answer, e.g. the forced-milk path, must fall back to a full recompute).
        /// </summary>
        public static bool TryGetEntry(Pawn pawn, out MilkEntry entry)
        {
            RebuildIfNeeded();
            foreach (KeyValuePair<int, List<MilkEntry>> pair in mapEntries)
            {
                List<MilkEntry> list = pair.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].pawn == pawn)
                    {
                        entry = list[i];
                        return true;
                    }
                }
            }

            entry = default;
            return false;
        }

        private static void RebuildIfNeeded()
        {
            if (Find.TickManager == null)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            List<Map> maps = Find.Maps;
            if (maps.Count == 0)
            {
                mapEntries.Clear();
                return;
            }

            if (now % PruneInterval == 0)
            {
                PruneDestroyedMaps(maps);
            }

            // Rebuild at most one stale map per call. The work scanner touches this cache
            // every tick, so round-robining spreads each map's re-scan over time and maps
            // that are never worked stay stale instead of being re-scanned constantly.
            for (int attempt = 0; attempt < maps.Count; attempt++)
            {
                if (nextMapIndex >= maps.Count)
                {
                    nextMapIndex = 0;
                }

                Map map = maps[nextMapIndex];
                nextMapIndex = (nextMapIndex + 1) % maps.Count;

                if (map == null || map.mapPawns == null)
                {
                    continue;
                }

                int last = mapLastRebuildTick.TryGetValue(map.uniqueID, out int stored) ? stored : -1;
                if (last >= 0 && now - last < RebuildInterval)
                {
                    continue;
                }

                mapLastRebuildTick[map.uniqueID] = now;
                RebuildMap(map);
                return;
            }
        }

        private static void RebuildMap(Map map)
        {
            if (!mapEntries.TryGetValue(map.uniqueID, out List<MilkEntry> list))
            {
                list = new List<MilkEntry>();
                mapEntries[map.uniqueID] = list;
            }

            list.Clear();

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!MilkHelper.TryGetMilkCandidate(pawn, out float availableMilk, out bool full))
                {
                    continue;
                }

                list.Add(new MilkEntry
                {
                    pawn = pawn,
                    full = full,
                    availableMilk = availableMilk,
                });
            }
        }

        private static void PruneDestroyedMaps(List<Map> maps)
        {
            HashSet<int> alive = new HashSet<int>(maps.Count);
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null)
                {
                    alive.Add(maps[i].uniqueID);
                }
            }

            if (mapEntries.Count > 0)
            {
                List<int> staleKeys = new List<int>();
                foreach (int key in mapEntries.Keys)
                {
                    if (!alive.Contains(key))
                    {
                        staleKeys.Add(key);
                    }
                }

                for (int i = 0; i < staleKeys.Count; i++)
                {
                    mapEntries.Remove(staleKeys[i]);
                    mapLastRebuildTick.Remove(staleKeys[i]);
                }
            }
        }
    }
}