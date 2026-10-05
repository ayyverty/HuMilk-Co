using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HuMilkCo
{
    [StaticConstructorOnStartup]
    public static class Mod_Init
    {
        static Mod_Init()
        {
            Harmony harmony = new Harmony("toasterbath.humilkco");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            RecipeMilkPatcher.PatchRecipes();
            AttachMilkCarrierDoers();
            AttachHumanMilkConsumptionDoers();
            AttachMoveSpeedLightStatPart();
        }

        /// <summary>
        /// Give every ingestible def that records its ingredients a milk-carrier doer,
        /// so meals crafted with any HuMilk Co milk grant that milk's effect on eating.
        /// Guarded against double-attachment on def reloads.
        /// </summary>
        private static void AttachMilkCarrierDoers()
        {
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < defs.Count; i++)
            {
                ThingDef def = defs[i];
                if (def?.ingestible == null || def.GetCompProperties<CompProperties_Ingredients>() == null)
                {
                    continue;
                }

                if (def.ingestible.outcomeDoers == null)
                {
                    def.ingestible.outcomeDoers = new List<IngestionOutcomeDoer>();
                }

                bool already = false;
                for (int j = 0; j < def.ingestible.outcomeDoers.Count; j++)
                {
                    if (def.ingestible.outcomeDoers[j] is IngestionOutcomeDoer_MilkCarrier)
                    {
                        already = true;
                        break;
                    }
                }

                if (!already)
                {
                    def.ingestible.outcomeDoers.Add(new IngestionOutcomeDoer_MilkCarrier());
                }
            }
        }

        /// <summary>
        /// Give every HuMilk Co milk ThingDef a consumption doer that records the
        /// ConsumedHumanMilk history event, so the Ideology "Human Milk Consumption"
        /// precept thoughts fire when a pawn drinks milk directly (meals are covered by
        /// the ingredient-scanning milk-carrier doer). Guarded against double-attachment
        /// on def reloads.
        /// </summary>
        private static void AttachHumanMilkConsumptionDoers()
        {
            List<ThingDef> milks = MilkDefs.AllMilkDefs();
            for (int i = 0; i < milks.Count; i++)
            {
                ThingDef def = milks[i];
                if (def?.ingestible == null)
                {
                    continue;
                }

                if (def.ingestible.outcomeDoers == null)
                {
                    def.ingestible.outcomeDoers = new List<IngestionOutcomeDoer>();
                }

                bool already = false;
                for (int j = 0; j < def.ingestible.outcomeDoers.Count; j++)
                {
                    if (def.ingestible.outcomeDoers[j] is IngestionOutcomeDoer_HumanMilkConsumption)
                    {
                        already = true;
                        break;
                    }
                }

                if (!already)
                {
                    def.ingestible.outcomeDoers.Add(new IngestionOutcomeDoer_HumanMilkConsumption());
                }
            }
        }

        /// <summary>
        /// Register the Dirtmole milk light-sensitivity stat part on MoveSpeed so it can
        /// apply its -10% penalty in daylight. Inert for everyone without the hediff.
        /// </summary>
        private static void AttachMoveSpeedLightStatPart()
        {
            StatDef moveSpeed = StatDefOf.MoveSpeed;
            if (moveSpeed == null)
            {
                return;
            }

            if (moveSpeed.parts == null)
            {
                moveSpeed.parts = new List<StatPart>();
            }

            for (int i = 0; i < moveSpeed.parts.Count; i++)
            {
                if (moveSpeed.parts[i] is StatPart_MilkLightPenalty)
                {
                    return;
                }
            }

            moveSpeed.parts.Add(StatPart_MilkLightPenalty.Instance);
        }
    }
}