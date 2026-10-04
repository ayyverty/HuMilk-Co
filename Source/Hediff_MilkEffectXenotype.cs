using RimWorld;
using Verse;

namespace HuMilkCo
{
    public class HediffCompProperties_MilkEffectXenotype : HediffCompProperties
    {
        /// <summary>
        /// Xenotype that receives the "target" variant of the milk effect (stage 1).
        /// Everyone else gets the "non-target" variant (stage 0).
        /// </summary>
        public XenotypeDef xenotype;

        /// <summary>
        /// Instant toxic buildup severity applied to non-target drinkers on ingestion.
        /// </summary>
        public float toxicBuildupOnNonXenotype;

        /// <summary>
        /// Instant hemogen (fraction of max) restored to target drinkers on ingestion.
        /// </summary>
        public float hemogenRestoreOnXenotype;

        /// <summary>
        /// Whether ingestion immediately ends any mental break the drinker is in.
        /// </summary>
        public bool endMentalBreakOnIngest;

        public HediffCompProperties_MilkEffectXenotype()
        {
            compClass = typeof(HediffComp_MilkEffectXenotype);
        }
    }

    public class HediffComp_MilkEffectXenotype : HediffComp
    {
        public HediffCompProperties_MilkEffectXenotype Props => (HediffCompProperties_MilkEffectXenotype)props;
    }

    /// <summary>
    /// Effect hediff whose stage depends on the drinker's xenotype (used by Toxic Milk
    /// and Blood Milk). Stage 0 is the non-target variant, stage 1 the target variant.
    /// Also runs any instant ingestion side effects: hemogen restore for the target
    /// xenotype, toxic buildup for non-targets, ending mental breaks.
    /// </summary>
    public class Hediff_MilkEffectXenotype : HediffWithComps
    {
        private HediffCompProperties_MilkEffectXenotype Props => GetComp<HediffComp_MilkEffectXenotype>()?.Props;

        private bool IsTargetXenotype
        {
            get
            {
                HediffCompProperties_MilkEffectXenotype props = Props;
                if (pawn == null || pawn.genes == null || props?.xenotype == null)
                {
                    return false;
                }

                return pawn.genes.Xenotype == props.xenotype;
            }
        }

        /// <summary>
        /// Stage 1 for the target xenotype, stage 0 otherwise. Clamped to the def's stage
        /// count so a single-stage def (e.g. Molong, which sets no target xenotype) can
        /// never index past its own stage list.
        /// </summary>
        public override int CurStageIndex
        {
            get
            {
                int index = IsTargetXenotype ? 1 : 0;
                return def.stages != null && index < def.stages.Count ? index : 0;
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            HediffCompProperties_MilkEffectXenotype props = Props;
            if (props == null || pawn == null || pawn.health == null || pawn.health.hediffSet == null)
            {
                return;
            }

            if (IsTargetXenotype && props.hemogenRestoreOnXenotype > 0f)
            {
                GeneUtility.OffsetHemogen(pawn, props.hemogenRestoreOnXenotype);
            }
            else if (!IsTargetXenotype && props.toxicBuildupOnNonXenotype > 0f)
            {
                HealthUtility.AdjustSeverity(pawn, HediffDefOf.ToxicBuildup, props.toxicBuildupOnNonXenotype);
            }

            if (props.endMentalBreakOnIngest && pawn.mindState?.mentalStateHandler?.CurState != null)
            {
                pawn.mindState.mentalStateHandler.CurState.RecoverFromState();
            }
        }
    }
}