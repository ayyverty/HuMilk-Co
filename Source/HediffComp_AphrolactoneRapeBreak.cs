using RimWorld;
using Verse;

namespace HuMilkCo
{
    public class HediffCompProperties_AphrolactoneRapeBreak : HediffCompProperties
    {
        public HediffCompProperties_AphrolactoneRapeBreak()
        {
            this.compClass = typeof(HediffComp_AphrolactoneRapeBreak);
        }
    }

    /// <summary>
    /// On the AphrolactoneAddiction hediff. While the addict's chemical need is empty
    /// (withdrawal), periodically starts the AphrolactoneRape mental break so the pawn
    /// rapes a random non-hostile humanlike pawn. Repeats at most once per day while
    /// the need stays empty; the break only runs again after it has ended.
    /// </summary>
    public class HediffComp_AphrolactoneRapeBreak : HediffComp
    {
        private const int CheckIntervalTicks = 150;
        private const int BreakCooldownDays = 1;
        private const float EmptyThreshold = 0.001f;

        private int lastBreakTick = -999999;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastBreakTick, "lastBreakTick", -999999);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Awake() || !pawn.Spawned)
            {
                return;
            }

            if (pawn.mindState?.mentalStateHandler == null)
            {
                return;
            }

            if (!pawn.IsHashIntervalTick(CheckIntervalTicks))
            {
                return;
            }

            if (pawn.InMentalState)
            {
                return;
            }

            if (MilkDefs.ChemicalAphrolactone == null || MilkDefs.AphrolactoneRape == null)
            {
                return;
            }

            Need_Chemical need = pawn.needs?.TryGetNeed(MilkDefs.ChemicalAphrolactone) as Need_Chemical;
            if (need == null || need.CurLevel > EmptyThreshold)
            {
                return;
            }

            if (GenTicks.TicksGame - lastBreakTick >= GenDate.TicksPerDay * BreakCooldownDays)
            {
                lastBreakTick = GenTicks.TicksGame;
                pawn.mindState.mentalStateHandler.TryStartMentalState(MilkDefs.AphrolactoneRape, "aphrolactone withdrawal");
            }
        }
    }
}