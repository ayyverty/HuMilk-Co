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
    /// xenotype, toxic buildup for non-targets.
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

        public override int CurStageIndex => IsTargetXenotype ? 1 : 0;

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
        }
    }
}