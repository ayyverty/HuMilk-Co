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
            GetBreastInfo(pawn, out bool hasBreasts, out _);
            return hasBreasts;
        }

        public static float GetBreastVolumeTotal(Pawn pawn)
        {
            GetBreastInfo(pawn, out _, out float volume);
            return volume;
        }

        /// <summary>
        /// Single pass over the breast hediffs returning both "has any non-featureless
        /// chest" and the total breast volume. Centralizing this avoids building the
        /// RJW breast list more than once when a caller needs both results.
        /// </summary>
        public static void GetBreastInfo(Pawn pawn, out bool hasBreasts, out float volume)
        {
            hasBreasts = false;
            volume = 0f;

            List<Hediff> breasts = pawn.GetBreastList();
            if (breasts == null)
            {
                return;
            }

            for (int i = 0; i < breasts.Count; i++)
            {
                Hediff breast = breasts[i];
                if (breast == null || breast.def == null || breast.def.defName == FeaturelessChestDefName)
                {
                    continue;
                }

                hasBreasts = true;
                if (PartSizeCalculator.TryGetBreastSize(breast, out BreastSize size))
                {
                    volume += size.volume;
                }
            }
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

        public static bool IsHucow(Pawn pawn)
        {
            if (MilkDefs.Hucow == null || pawn?.story?.traits == null)
            {
                return false;
            }

            return pawn.story.traits.HasTrait(MilkDefs.Hucow);
        }

        public static bool IsWillingToBeMilked(Pawn pawn)
        {
            if (IsHucow(pawn))
            {
                return true;
            }

            if (MilkDefs.AphrolactoneLactation != null &&
                pawn.health != null && pawn.health.hediffSet != null &&
                pawn.health.hediffSet.HasHediff(MilkDefs.AphrolactoneLactation))
            {
                return true;
            }

            // Checked last: this reaches into the quirks assembly through a cached
            // compiled delegate, so it stays cheap enough for the candidate scan.
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
        /// Context-free auto-milking candidate evaluation: computes, in a single pass,
        /// whether the pawn is a valid target (colony member, lactating with breasts,
        /// willing or bound, and a usable milk charge) plus the milk it has available
        /// and whether it is full. Used by the interval-rebuilt <see cref="MilkCache"/>,
        /// which stores the result so per-milker scans don't repeat this work.
        /// </summary>
        internal static bool TryGetMilkCandidate(Pawn target, out float availableMilk, out bool full)
        {
            availableMilk = 0f;
            full = false;

            if (target == null || !target.Spawned || target.Dead || !target.RaceProps.Humanlike)
            {
                return false;
            }

            if (!(target.IsColonist || target.IsPrisonerOfColony || target.IsSlaveOfColony))
            {
                return false;
            }

            if (!IsLactating(target))
            {
                return false;
            }

            GetBreastInfo(target, out bool hasBreasts, out float breastVolume);
            if (!hasBreasts)
            {
                return false;
            }

            if (!(IsWillingToBeMilked(target) || BoundHelper.IsBound(target)))
            {
                return false;
            }

            if (!MilkReservoir.TryGetChargeState(target, breastVolume, out float capacity, out float chargeFactor))
            {
                return false;
            }

            availableMilk = capacity * chargeFactor;
            full = chargeFactor >= 0.999f;
            return true;
        }

        /// <summary>
        /// Cheap per-milker filter for the auto-milking scan. Only the bits that vary
        /// per milker (map, Animals skill, self, target alive/spawned). Full target-side
        /// eligibility is established by the MilkCache rebuild.
        /// </summary>
        public static bool IsMilkingCandidateFor(Pawn milker, Pawn target)
        {
            if (milker == target || target == null || !target.Spawned || target.Dead)
            {
                return false;
            }

            if (milker.skills == null ||
                milker.skills.GetSkill(SkillDefOf.Animals).Level < HuMilkCoMod.Settings.RequiredAnimalsSkill)
            {
                return false;
            }

            return target.Map == milker.Map;
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