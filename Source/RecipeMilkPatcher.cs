using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Makes Human Milk usable in any recipe that currently requires the vanilla 'Milk' defName.
    /// Runs once after all defs are resolved. Category-based recipes (FoodRaw/AnimalProductRaw)
    /// already accept it automatically.
    /// </summary>
    public static class RecipeMilkPatcher
    {
        public static void PatchRecipes()
        {
            ThingDef vanillaMilk = DefDatabase<ThingDef>.GetNamedSilentFail("Milk");
            ThingDef humanMilk = DefDatabase<ThingDef>.GetNamedSilentFail("MilkHuman");

            if (vanillaMilk == null || humanMilk == null)
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
                    if (allowed != null && allowed.Count() == 1 && allowed.Contains(vanillaMilk))
                    {
                        ingredient.filter.SetAllow(humanMilk, true);
                    }
                }
            }
        }
    }
}