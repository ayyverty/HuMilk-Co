using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Invisible per-pawn tracker for human milk consumption: when tracking began (so a
    /// freshly tracked pawn gets a full grace window before the deprivation thought can
    /// appear) and the last tick human milk was consumed. Persists with the pawn via
    /// ExposeData.
    /// </summary>
    public class Hediff_HumanMilkTracker : HediffWithComps
    {
        public int trackingStartTick;

        public int lastConsumedTick = -999999;

        public override bool Visible => false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref trackingStartTick, "trackingStartTick", 0);
            Scribe_Values.Look(ref lastConsumedTick, "lastConsumedTick", -999999);
        }
    }
}