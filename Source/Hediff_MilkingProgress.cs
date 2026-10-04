using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Invisible per-pawn tracker for the Hucow progression: how many times a pawn has
    /// been milked and the last tick it was milked. Persists with the pawn via ExposeData.
    /// </summary>
    public class Hediff_MilkingProgress : HediffWithComps
    {
        public int milkingCount;

        public int lastMilkedTick = -999999;

        public override bool Visible => false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref milkingCount, "milkingCount", 0);
            Scribe_Values.Look(ref lastMilkedTick, "lastMilkedTick", -999999);
        }
    }
}