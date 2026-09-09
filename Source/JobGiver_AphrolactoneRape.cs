using System.Collections.Generic;
using System.Linq;
using rjw;
using Verse;
using Verse.AI;

namespace HuMilkCo
{
    /// <summary>
    /// Job giver for the AphrolactoneRape mental state. Picks a random non-hostile
    /// humanlike pawn on the map and makes the pawn rape it, using RJW's own rape
    /// eligibility filtering restricted to humanlike victims.
    /// </summary>
    public class JobGiver_AphrolactoneRape : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!xxx.can_rape(pawn, forced: true))
            {
                return null;
            }

            List<Pawn> victims = JobGiver_RandomRape.PotentialVictimsFor(pawn)
                .Where(v => v.RaceProps.Humanlike)
                .ToList();
            if (victims.Count == 0)
            {
                return null;
            }

            victims.Shuffle();
            for (int i = 0; i < victims.Count; i++)
            {
                Pawn victim = victims[i];
                if (!Pather_Utility.can_path_to_target(pawn, victim))
                {
                    continue;
                }

                return JobMaker.MakeJob(xxx.RapeRandom, victim);
            }

            return null;
        }
    }
}