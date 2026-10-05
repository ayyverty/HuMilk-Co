using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Attached at load time to every HuMilk Co milk ThingDef (the raw MilkHuman and all
    /// xenotype milks). Records the <c>ConsumedHumanMilk</c> history event when a pawn
    /// drinks milk directly, so the "Human Milk Consumption" precept thoughts fire for
    /// raw consumption as well as milk cooked into meals.
    /// </summary>
    public class IngestionOutcomeDoer_HumanMilkConsumption : IngestionOutcomeDoer
    {
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            if (ingested == null)
            {
                return;
            }

            HumanMilkConsumption.RecordConsumed(pawn);
        }
    }
}