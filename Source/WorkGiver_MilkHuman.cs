using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace HuMilkCo
{
    public class WorkGiver_MilkHuman : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (pawn.Map == null)
            {
                yield break;
            }

            foreach (Pawn target in MilkCache.Candidates)
            {
                if (MilkHelper.IsMilkingCandidateFor(pawn, target))
                {
                    yield return target;
                }
            }
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Pawn target) || !pawn.CanReserve(target))
            {
                return null;
            }

            // Cheap per-milker gate (self, spawned/alive, same map, Animals skill).
            if (!MilkHelper.IsMilkingCandidateFor(pawn, target))
            {
                return null;
            }

            if (MilkDefs.MilkHumanJob == null)
            {
                return null;
            }

            // The cache already established target-side eligibility and milk state in one
            // pass, so the auto path only needs the cached fullness. The forced path uses
            // the cached available milk when present, falling back to a full recompute for
            // targets that were not cached (e.g. became lactating after the last rebuild).
            bool valid;
            if (MilkCache.TryGetEntry(target, out MilkCache.MilkEntry entry))
            {
                valid = forced
                    ? entry.availableMilk >= HuMilkCoMod.Settings.MinMilkToMilk
                    : entry.full;
            }
            else
            {
                valid = forced && MilkHelper.IsValidMilkingTarget(pawn, target);
            }

            if (!valid)
            {
                return null;
            }

            return JobMaker.MakeJob(MilkDefs.MilkHumanJob, target);
        }
    }
}