using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Cached references to the defs this mod defines or depends on. Lookups are
    /// lazy (resolved on first use, re-resolved if null) so that missing optional
    /// defs (e.g. Onahole binding hediffs) degrade gracefully instead of throwing
    /// during load, and so the hot scan paths don't re-hit the def database.
    /// </summary>
    public static class MilkDefs
    {
        private static ThingDef milkHuman;
        public static ThingDef MilkHuman
        {
            get
            {
                if (milkHuman == null)
                {
                    milkHuman = DefDatabase<ThingDef>.GetNamedSilentFail("MilkHuman");
                }

                return milkHuman;
            }
        }

        private static HediffDef lactating;
        public static HediffDef Lactating
        {
            get
            {
                if (lactating == null)
                {
                    lactating = DefDatabase<HediffDef>.GetNamedSilentFail("Lactating");
                }

                return lactating;
            }
        }

        private static HediffDef aphrolactoneLactation;
        public static HediffDef AphrolactoneLactation
        {
            get
            {
                if (aphrolactoneLactation == null)
                {
                    aphrolactoneLactation = DefDatabase<HediffDef>.GetNamedSilentFail("AphrolactoneLactation");
                }

                return aphrolactoneLactation;
            }
        }

        private static HediffDef aphrolactoneAddiction;
        public static HediffDef AphrolactoneAddiction
        {
            get
            {
                if (aphrolactoneAddiction == null)
                {
                    aphrolactoneAddiction = DefDatabase<HediffDef>.GetNamedSilentFail("AphrolactoneAddiction");
                }

                return aphrolactoneAddiction;
            }
        }

        private static NeedDef chemicalAphrolactone;
        public static NeedDef ChemicalAphrolactone
        {
            get
            {
                if (chemicalAphrolactone == null)
                {
                    chemicalAphrolactone = DefDatabase<NeedDef>.GetNamedSilentFail("Chemical_Aphrolactone");
                }

                return chemicalAphrolactone;
            }
        }

        private static MentalStateDef aphrolactoneRape;
        public static MentalStateDef AphrolactoneRape
        {
            get
            {
                if (aphrolactoneRape == null)
                {
                    aphrolactoneRape = DefDatabase<MentalStateDef>.GetNamedSilentFail("AphrolactoneRape");
                }

                return aphrolactoneRape;
            }
        }

        private static StatDef milkProduction;
        public static StatDef MilkProduction
        {
            get
            {
                if (milkProduction == null)
                {
                    milkProduction = DefDatabase<StatDef>.GetNamedSilentFail("RJW_MilkProduction");
                }

                return milkProduction;
            }
        }

        private static TraitDef hucow;
        public static TraitDef Hucow
        {
            get
            {
                if (hucow == null)
                {
                    hucow = DefDatabase<TraitDef>.GetNamedSilentFail("Hucow");
                }

                return hucow;
            }
        }

        private static ThoughtDef forcedToBeMilked;
        public static ThoughtDef ForcedToBeMilked
        {
            get
            {
                if (forcedToBeMilked == null)
                {
                    forcedToBeMilked = DefDatabase<ThoughtDef>.GetNamedSilentFail("ForcedToBeMilked");
                }

                return forcedToBeMilked;
            }
        }

        private static ThoughtDef forcedToBeMilkedMood;
        public static ThoughtDef ForcedToBeMilkedMood
        {
            get
            {
                if (forcedToBeMilkedMood == null)
                {
                    forcedToBeMilkedMood = DefDatabase<ThoughtDef>.GetNamedSilentFail("ForcedToBeMilkedMood");
                }

                return forcedToBeMilkedMood;
            }
        }

        private static ThoughtDef pleasureFromMilking;
        public static ThoughtDef PleasureFromMilking
        {
            get
            {
                if (pleasureFromMilking == null)
                {
                    pleasureFromMilking = DefDatabase<ThoughtDef>.GetNamedSilentFail("PleasureFromMilking");
                }

                return pleasureFromMilking;
            }
        }

        private static JobDef milkHumanJob;
        public static JobDef MilkHumanJob
        {
            get
            {
                if (milkHumanJob == null)
                {
                    milkHumanJob = DefDatabase<JobDef>.GetNamedSilentFail("MilkHuman");
                }

                return milkHumanJob;
            }
        }

        private static Def milkingQuirk;
        public static Def MilkingQuirk
        {
            get
            {
                if (milkingQuirk == null)
                {
                    // The 'Milking' quirk is defined by this mod but its class (RJWQuirksFork.QuirkDef)
                    // only exists when rjw-quirks-fork is loaded. Resolve the def type via reflection
                    // so a missing fork degrades gracefully instead of failing to load the assembly.
                    if (ModsConfig.IsActive("rjw.quirks.fork"))
                    {
                        System.Type quirkDefType = System.Type.GetType("RJWQuirksFork.QuirkDef, RJWQuirksFork");
                        if (quirkDefType != null)
                        {
                            milkingQuirk = GenDefDatabase.GetDef(quirkDefType, "Milking", false);
                        }
                    }
                }

                return milkingQuirk;
            }
        }

        private static HediffDef dirtmoleEffect;
        public static HediffDef DirtmoleEffect
        {
            get
            {
                if (dirtmoleEffect == null)
                {
                    dirtmoleEffect = DefDatabase<HediffDef>.GetNamedSilentFail("MilkEffectDirtmole");
                }

                return dirtmoleEffect;
            }
        }

        private static Dictionary<XenotypeDef, MilkXenotypeDef> xenotypeToMilk;
        private static Dictionary<ThingDef, HediffDef> milkToEffect;
        private static bool registryResolved;

        private static void EnsureRegistry()
        {
            if (registryResolved)
            {
                return;
            }

            registryResolved = true;
            xenotypeToMilk = new Dictionary<XenotypeDef, MilkXenotypeDef>();
            milkToEffect = new Dictionary<ThingDef, HediffDef>();

            List<MilkXenotypeDef> links = DefDatabase<MilkXenotypeDef>.AllDefsListForReading;
            for (int i = 0; i < links.Count; i++)
            {
                MilkXenotypeDef link = links[i];
                if (link == null)
                {
                    continue;
                }

                if (link.xenotype != null && link.milk != null && !xenotypeToMilk.ContainsKey(link.xenotype))
                {
                    xenotypeToMilk.Add(link.xenotype, link);
                }

                if (link.milk != null && link.effect != null && !milkToEffect.ContainsKey(link.milk))
                {
                    milkToEffect.Add(link.milk, link.effect);
                }
            }
        }

        /// <summary>
        /// The milk def a pawn produces, determined by its xenotype. Falls back to the
        /// default <c>MilkHuman</c> for baseliners and unmapped xenotypes.
        /// </summary>
        public static ThingDef GetMilkForPawn(Pawn pawn)
        {
            if (pawn == null || pawn.genes == null || pawn.genes.Xenotype == null)
            {
                return MilkHuman;
            }

            EnsureRegistry();
            if (xenotypeToMilk.TryGetValue(pawn.genes.Xenotype, out MilkXenotypeDef link) && link.milk != null)
            {
                return link.milk;
            }

            return MilkHuman;
        }

        /// <summary>
        /// The timed effect hediff granted by drinking the given milk def, or null if
        /// the milk has no mapped effect (e.g. plain human milk).
        /// </summary>
        public static HediffDef GetMilkEffect(ThingDef milk)
        {
            if (milk == null)
            {
                return null;
            }

            EnsureRegistry();
            return milkToEffect.TryGetValue(milk, out HediffDef effect) ? effect : null;
        }

        /// <summary>
        /// Every milk def this mod knows about: the default human milk plus each mapped
        /// xenotype milk. Used to extend recipes that accept vanilla milk.
        /// </summary>
        public static List<ThingDef> AllMilkDefs()
        {
            List<ThingDef> result = new List<ThingDef>();
            if (MilkHuman != null)
            {
                result.Add(MilkHuman);
            }

            EnsureRegistry();
            foreach (KeyValuePair<ThingDef, HediffDef> pair in milkToEffect)
            {
                if (pair.Key != null && !result.Contains(pair.Key))
                {
                    result.Add(pair.Key);
                }
            }

            return result;
        }
    }
}