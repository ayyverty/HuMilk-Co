using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Cached references to the defs this mod defines or depends on. Lookups are lazy
    /// and each def resolves exactly once (even to null), so missing optional defs
    /// (e.g. a not-loaded quirk fork) degrade gracefully without throwing during load,
    /// and the hot scan paths never re-hit the def database for a def that is absent.
    /// All defs are fully loaded before gameplay, so a single resolution is safe.
    /// </summary>
    public static class MilkDefs
    {
        /// <summary>
        /// Def reference that resolves once on first access and caches the result
        /// permanently (including a null result for absent optional defs).
        /// </summary>
        private struct LazyDef<T> where T : Def
        {
            private bool resolved;
            private T value;

            public T Get(string defName)
            {
                if (!resolved)
                {
                    resolved = true;
                    value = DefDatabase<T>.GetNamedSilentFail(defName);
                }

                return value;
            }
        }

        private static LazyDef<ThingDef> milkHuman;
        public static ThingDef MilkHuman => milkHuman.Get("MilkHuman");

        private static LazyDef<HediffDef> lactating;
        public static HediffDef Lactating => lactating.Get("Lactating");

        private static LazyDef<HediffDef> aphrolactoneLactation;
        public static HediffDef AphrolactoneLactation => aphrolactoneLactation.Get("AphrolactoneLactation");

        private static LazyDef<HediffDef> aphrolactoneAddiction;
        public static HediffDef AphrolactoneAddiction => aphrolactoneAddiction.Get("AphrolactoneAddiction");

        private static LazyDef<NeedDef> chemicalAphrolactone;
        public static NeedDef ChemicalAphrolactone => chemicalAphrolactone.Get("Chemical_Aphrolactone");

        private static LazyDef<MentalStateDef> aphrolactoneRape;
        public static MentalStateDef AphrolactoneRape => aphrolactoneRape.Get("AphrolactoneRape");

        private static LazyDef<StatDef> milkProduction;
        public static StatDef MilkProduction => milkProduction.Get("RJW_MilkProduction");

        private static LazyDef<TraitDef> hucow;
        public static TraitDef Hucow => hucow.Get("Hucow");

        private static LazyDef<ThoughtDef> forcedToBeMilked;
        public static ThoughtDef ForcedToBeMilked => forcedToBeMilked.Get("ForcedToBeMilked");

        private static LazyDef<ThoughtDef> forcedToBeMilkedMood;
        public static ThoughtDef ForcedToBeMilkedMood => forcedToBeMilkedMood.Get("ForcedToBeMilkedMood");

        private static LazyDef<HediffDef> milkingProgress;
        public static HediffDef MilkingProgress => milkingProgress.Get("MilkingProgress");

        private static LazyDef<HediffDef> humanMilkTracker;
        public static HediffDef HumanMilkTracker => humanMilkTracker.Get("HumanMilkTracker");

        private static LazyDef<HistoryEventDef> consumedHumanMilk;
        public static HistoryEventDef ConsumedHumanMilk => consumedHumanMilk.Get("ConsumedHumanMilk");

        private static LazyDef<ThoughtDef> freshlyMilkedMood;
        public static ThoughtDef FreshlyMilkedMood => freshlyMilkedMood.Get("FreshlyMilkedMood");

        private static LazyDef<ThoughtDef> soreUdders;
        public static ThoughtDef SoreUdders => soreUdders.Get("SoreUdders");

        private static LazyDef<ThoughtDef> pleasureFromMilking;
        public static ThoughtDef PleasureFromMilking => pleasureFromMilking.Get("PleasureFromMilking");

        private static LazyDef<JobDef> milkHumanJob;
        public static JobDef MilkHumanJob => milkHumanJob.Get("MilkHuman");

        private static bool milkingQuirkResolved;
        private static Def milkingQuirk;
        public static Def MilkingQuirk
        {
            get
            {
                if (milkingQuirkResolved)
                {
                    return milkingQuirk;
                }

                milkingQuirkResolved = true;

                // The 'Milking' quirk is defined by this mod but its class (RJWQuirks.QuirkDef or
                // RJWQuirksFork.QuirkDef) only exists when a quirks mod is loaded. Resolve the def
                // type through QuirkBridge so a missing quirks mod degrades gracefully instead of
                // failing to load the assembly, and either quirks mod is accepted.
                System.Type quirkDefType = QuirkBridge.QuirkDefType;
                if (quirkDefType != null)
                {
                    milkingQuirk = GenDefDatabase.GetDef(quirkDefType, "Milking", false);
                }

                return milkingQuirk;
            }
        }

        private static LazyDef<HediffDef> dirtmoleEffect;
        public static HediffDef DirtmoleEffect => dirtmoleEffect.Get("MilkEffectDirtmole");

        private static Dictionary<XenotypeDef, MilkXenotypeDef> xenotypeToMilk;
        private static Dictionary<ThingDef, HediffDef> milkToEffect;
        private static List<MilkXenotypeDef> observedRegistryDefs;

        private static void EnsureRegistry()
        {
            // AllDefsListForReading is a stable instance between def reloads and a new
            // instance after one. Comparing references invalidates the cache exactly then
            // (dev-mode reloads, late def sets) without paying per-call cost.
            List<MilkXenotypeDef> defs = DefDatabase<MilkXenotypeDef>.AllDefsListForReading;
            if (observedRegistryDefs == defs)
            {
                return;
            }

            observedRegistryDefs = defs;
            xenotypeToMilk = new Dictionary<XenotypeDef, MilkXenotypeDef>();
            milkToEffect = new Dictionary<ThingDef, HediffDef>();

            for (int i = 0; i < defs.Count; i++)
            {
                MilkXenotypeDef link = defs[i];
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