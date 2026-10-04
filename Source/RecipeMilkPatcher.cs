using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Makes every HuMilk Co milk usable in any recipe that currently requires the
    /// vanilla 'Milk' defName. Runs once after all defs are resolved. Category-based
    /// recipes (FoodRaw/AnimalProductRaw) already accept the milks automatically.
    /// </summary>
    public static class RecipeMilkPatcher
    {
        public static void PatchRecipes()
        {
            ThingDef vanillaMilk = DefDatabase<ThingDef>.GetNamedSilentFail("Milk");
            if (vanillaMilk == null)
            {
                return;
            }

            List<ThingDef> milks = MilkDefs.AllMilkDefs();
            if (milks.Count == 0)
            {
                return;
            }

            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe.ingredients == null)
                {
                    continue;
                }

                foreach (IngredientCount ingredient in recipe.ingredients)
                {
                    if (ingredient?.filter == null)
                    {
                        continue;
                    }

                    IEnumerable<ThingDef> allowed = ingredient.filter.AllowedThingDefs;
                    if (allowed == null || !allowed.Contains(vanillaMilk))
                    {
                        continue;
                    }

                    for (int i = 0; i < milks.Count; i++)
                    {
                        // Category-based filters (AnimalProductRaw etc.) already accept the
                        // milks, so only patch what the filter would otherwise reject.
                        if (milks[i] != null && !ingredient.filter.Allows(milks[i]))
                        {
                            ingredient.filter.SetAllow(milks[i], true);
                        }
                    }
                }
            }
        }
    }
}