using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Attached at load time to every ingestible def that carries vanilla
    /// <see cref="CompIngredients"/>. On eating, scans the meal's recorded ingredients
    /// for any HuMilk Co milk and applies each distinct milk's effect, so crafted meals
    /// inherit the effects of the milk used in them.
    /// </summary>
    public class IngestionOutcomeDoer_MilkCarrier : IngestionOutcomeDoer
    {
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            if (ingested == null)
            {
                return;
            }

            CompIngredients comp = ingested.TryGetComp<CompIngredients>();
            if (comp == null || comp.ingredients.NullOrEmpty())
            {
                return;
            }

            for (int i = 0; i < comp.ingredients.Count; i++)
            {
                MilkEffectUtility.ApplyEffect(pawn, MilkDefs.GetMilkEffect(comp.ingredients[i]));
            }
        }
    }
}