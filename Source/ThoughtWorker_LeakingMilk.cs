using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Active for aphrolactone addicts whose milk is more than 80% full and dripping.
    /// </summary>
    public class ThoughtWorker_LeakingMilk : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!MilkHelper.IsAphrolactoneAddicted(p))
            {
                return ThoughtState.Inactive;
            }

            if (MilkReservoir.TryGetCharge(p, out float factor, out _) && factor > 0.8f)
            {
                return ThoughtState.ActiveDefault;
            }

            return ThoughtState.Inactive;
        }
    }
}