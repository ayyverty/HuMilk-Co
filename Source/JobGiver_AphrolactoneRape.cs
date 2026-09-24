using RimWorld;
using rjw;
using Verse;
using Verse.AI;

namespace HuMilkCo
{
    /// <summary>
    /// Job giver for the AphrolactoneRape mental state. Drives the addict to rape
    /// the nearest non-hostile humanlike pawn on the map.
    ///
    /// The break is forced by withdrawal, so unlike RJW's own rape job givers it
    /// deliberately ignores the optional "vulnerability" / "rape mood" gates
    /// (xxx.can_rape / xxx.can_get_raped) that would otherwise filter out most
    /// colonists and leave the pawn wandering. Only physical capability, adulthood,
    /// hostility and pathing are checked.
    /// </summary>
    public class JobGiver_AphrolactoneRape : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.Spawned || !CanRapeForced(pawn))
            {
                return null;
            }

            Pawn victim = FindNearestVictim(pawn);
            if (victim == null)
            {
                return null;
            }

            return JobMaker.MakeJob(xxx.RapeRandom, victim);
        }

        /// <summary>
        /// Any pawn that is sexually capable at all can be compelled to rape in
        /// this state, regardless of RJW's vulnerability or sex-need requirements.
        /// </summary>
        private static bool CanRapeForced(Pawn pawn)
        {
            return xxx.can_fuck(pawn) || xxx.can_be_fucked(pawn);
        }

        private static Pawn FindNearestVictim(Pawn pawn)
        {
            Pawn best = null;
            int bestDist = int.MaxValue;

            foreach (Pawn victim in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (victim == pawn || !victim.RaceProps.Humanlike)
                {
                    continue;
                }

                if (victim.Suspended || victim.Drafted || victim.Downed)
                {
                    continue;
                }

                if (victim.IsForbidden(pawn) || victim.HostileTo(pawn))
                {
                    continue;
                }

                if (!xxx.is_not_dying(victim) || !xxx.can_be_fucked(victim))
                {
                    continue;
                }

                if (xxx.is_human(victim) && victim.ageTracker.Growth < 1 && !victim.ageTracker.CurLifeStage.reproductive)
                {
                    continue;
                }

                int dist = pawn.Position.DistanceToSquared(victim.Position);
                if (dist >= bestDist)
                {
                    continue;
                }

                if (!Pather_Utility.can_path_to_target(pawn, victim))
                {
                    continue;
                }

                if (!pawn.CanReserve(victim, xxx.max_rapists_per_prisoner, 0))
                {
                    continue;
                }

                best = victim;
                bestDist = dist;
            }

            return best;
        }
    }
}