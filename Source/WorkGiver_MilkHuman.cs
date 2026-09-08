using System.Collections.Generic;
using System.Linq;
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
                if (MilkHelper.IsValidAutoMilkingTarget(pawn, target))
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

            bool valid = forced ? MilkHelper.IsValidMilkingTarget(pawn, target)
                                : MilkHelper.IsValidAutoMilkingTarget(pawn, target);
            if (!valid)
            {
                return null;
            }

            if (MilkDefs.MilkHumanJob == null)
            {
                return null;
            }

            return JobMaker.MakeJob(MilkDefs.MilkHumanJob, target);
        }
    }
}