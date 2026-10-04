using System.Collections.Generic;
using RimWorld;
using rjw;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Static helpers for breast size, lactation state, willingness, and target eligibility.
    /// </summary>
    public static class MilkHelper
    {
        private const string FeaturelessChestDefName = "FeaturelessChest";

        public static bool HasBreasts(Pawn pawn)
        {
            List<Hediff> breasts = pawn.GetBreastList();
            if (breasts == null)
            {
                return false;
            }

            for (int i = 0; i < breasts.Count; i++)
            {
                Hediff breast = breasts[i];
                if (breast != null && breast.def != null && breast.def.defName != FeaturelessChestDefName)
                {
                    return true;
                }
            }

            return false;
        }

        public static float GetBreastVolumeTotal(Pawn pawn)
        {
            List<Hediff> breasts = pawn.GetBreastList();
            if (breasts == null)
            {
                return 0f;
            }

            float total = 0f;
            for (int i = 0; i < breasts.Count; i++)
            {
                Hediff breast = breasts[i];
                if (breast == null || breast.def == null || breast.def.defName == FeaturelessChestDefName)
                {
                    continue;
                }

                if (PartSizeCalculator.TryGetBreastSize(breast, out BreastSize size))
                {
                    total += size.volume;
                }
            }

            return total;
        }

        public static bool IsLactating(Pawn pawn)
        {
            if (pawn.health == null || pawn.health.hediffSet == null)
            {
                return false;
            }

            if (MilkDefs.Lactating != null && pawn.health.hediffSet.HasHediff(MilkDefs.Lactating))
            {
                return true;
            }

            if (MilkDefs.AphrolactoneLactation != null && pawn.health.hediffSet.HasHediff(MilkDefs.AphrolactoneLactation))
            {
                return true;
            }

            return false;
        }

        public static bool IsAphrolactoneAddicted(Pawn pawn)
        {
            if (pawn.health == null || pawn.health.hediffSet == null)
            {
                return false;
            }

            return MilkDefs.AphrolactoneAddiction != null &&
                pawn.health.hediffSet.HasHediff(MilkDefs.AphrolactoneAddiction);
        }

        public static bool IsWillingToBeMilked(Pawn pawn)
        {
            if (MilkDefs.Hucow != null && pawn.story != null && pawn.story.traits != null &&
                pawn.story.traits.HasTrait(MilkDefs.Hucow))
            {
                return true;
            }

            if (MilkDefs.AphrolactoneLactation != null &&
                pawn.health != null && pawn.health.hediffSet != null &&
                pawn.health.hediffSet.HasHediff(MilkDefs.AphrolactoneLactation))
            {
                return true;
            }

            // Checked last: this reaches into the quirks assembly reflectively, so it is the
            // most expensive check here and runs on every candidate scan.
            return QuirkBridge.HasQuirk(pawn, MilkDefs.MilkingQuirk);
        }

        /// <summary>
        /// A pawn is a valid forced-milking target when they are a colonist, prisoner, or
        /// slave of the colony; are lactating and have breasts and milk available; and are
        /// either a willing pawn, or an unwilling pawn who is bound.
        /// </summary>
        public static bool IsValidMilkingTarget(Pawn milker, Pawn target)
        {
            if (!IsTargetEligible(milker, target))
            {
                return false;
            }

            return MilkReservoir.HasMilkAvailable(target);
        }

        /// <summary>
        /// A pawn is a valid automatic-milking target when they pass the same checks as
        /// <see cref="IsValidMilkingTarget"/> but are at full milk. One charge evaluation
        /// instead of two so the work scanner stays cheap.
        /// </summary>
        public static bool IsValidAutoMilkingTarget(Pawn milker, Pawn target)
        {
            if (!IsTargetEligible(milker, target))
            {
                return false;
            }

            return MilkReservoir.IsFull(target);
        }

        /// <summary>
        /// Context-free auto-milking candidate check (no milker, no map). Used by the
        /// interval-rebuilt <see cref="MilkCache"/> to pre-filter spawned pawns cheaply.
        /// </summary>
        public static bool IsMilkingCandidate(Pawn target)
        {
            if (!IsTargetSideCandidate(target))
            {
                return false;
            }

            return MilkReservoir.IsFull(target);
        }

        private static bool IsTargetEligible(Pawn milker, Pawn target)
        {
            if (milker == target)
            {
                return false;
            }

            if (milker.skills == null ||
                milker.skills.GetSkill(SkillDefOf.Animals).Level < HuMilkCoMod.Settings.RequiredAnimalsSkill)
            {
                return false;
            }

            if (!IsTargetSideCandidate(target))
            {
                return false;
            }

            return target.Map == milker.Map;
        }

        /// <summary>
        /// Target-side eligibility: alive, on a map, humanlike, of the colony (colonist /
        /// prisoner / slave), lactating with breasts, and either willing to be milked or bound.
        /// Kept free of charge evaluation and milker context so it can be shared by the forced
        /// path, the auto path, and the candidate cache.
        /// </summary>
        private static bool IsTargetSideCandidate(Pawn target)
        {
            if (target == null || !target.Spawned || target.Dead || !target.RaceProps.Humanlike)
            {
                return false;
            }

            if (!(target.IsColonist || target.IsPrisonerOfColony || target.IsSlaveOfColony))
            {
                return false;
            }

            if (!IsLactating(target) || !HasBreasts(target))
            {
                return false;
            }

            if (IsWillingToBeMilked(target))
            {
                return true;
            }

            return BoundHelper.IsBound(target);
        }
    }
}