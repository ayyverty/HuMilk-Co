using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace HuMilkCo
{
    public class JobDriver_MilkHuman : JobDriver
    {
        protected Pawn Victim => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A);

            int workTicks = HuMilkCoMod.Settings.MilkWorkTicks;
            Toil milkToil = Toils_General.Wait(workTicks, TargetIndex.A)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOn(() => Victim == null || !Victim.Spawned ||
                              Victim.Position.DistanceTo(pawn.Position) > 3f)
                .WithProgressBarToilDelay(TargetIndex.A);
            milkToil.handlingFacing = true;
            milkToil.initAction += delegate
            {
                MakeVictimWait();
            };
            yield return milkToil;

            yield return new Toil
            {
                initAction = delegate
                {
                    MilkReservoir.MilkPawn(Victim, pawn);
                },
                defaultCompleteMode = ToilCompleteMode.Instant,
            };
        }

        private void MakeVictimWait()
        {
            if (Victim == null || Victim == pawn || Victim.jobs == null ||
                Victim.Dead || Victim.Downed)
            {
                return;
            }

            Job waitJob = JobMaker.MakeJob(JobDefOf.Wait, HuMilkCoMod.Settings.MilkWorkTicks);
            Victim.jobs.StartJob(waitJob, JobCondition.InterruptForced);
        }
    }
}