using RimWorld;
using UnityEngine;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Milk reservoir based on vanilla Biotech's chargeable lactation comp. The vanilla
    /// <c>HediffComp_Chargeable</c> on the lactating hediff is the single source of truth for
    /// fullness (it refills automatically over ~6 hours and is drained by breastfeeding), so
    /// this class only reads that charge, scales it to breast volume and settings, and drains
    /// it when a colonist hand-milks the pawn.
    /// </summary>
    public static class MilkReservoir
    {
        /// <summary>
        /// Capacity in milk units. Scales with RJW breast volume (larger breasts hold more) and
        /// any source of the RJW_MilkProduction stat (e.g. the 2.0x Aphrolactone boost).
        /// </summary>
        public static float GetCapacity(Pawn pawn)
        {
            MilkHelper.GetBreastInfo(pawn, out _, out float breastVolume);
            float capacity = breastVolume * HuMilkCoMod.Settings.LitresToMilkUnits;

            if (MilkDefs.MilkProduction != null)
            {
                float statValue = pawn.GetStatValue(MilkDefs.MilkProduction, true);
                if (statValue > 1f)
                {
                    capacity *= statValue;
                }
            }

            return capacity;
        }

        public static float GetAvailableMilk(Pawn pawn)
        {
            return TryGetCharge(pawn, out float factor, out float capacity) ? factor * capacity : 0f;
        }

        public static bool HasMilkAvailable(Pawn pawn)
        {
            return GetAvailableMilk(pawn) >= HuMilkCoMod.Settings.MinMilkToMilk;
        }

        public static bool IsFull(Pawn pawn)
        {
            return TryGetCharge(pawn, out float factor, out _) && factor >= 0.999f;
        }

        /// <summary>
        /// Fullness check that reuses an already-computed breast volume, so the hot
        /// auto-milking candidate scan doesn't pay for a second breast-list build.
        /// </summary>
        internal static bool IsFullGivenVolume(Pawn pawn, float breastVolume)
        {
            if (breastVolume <= 0f || pawn == null || pawn.Dead || pawn.Discarded ||
                !MilkHelper.IsLactating(pawn))
            {
                return false;
            }

            float capacity = breastVolume * HuMilkCoMod.Settings.LitresToMilkUnits;
            if (MilkDefs.MilkProduction != null)
            {
                float statValue = pawn.GetStatValue(MilkDefs.MilkProduction, true);
                if (statValue > 1f)
                {
                    capacity *= statValue;
                }
            }

            HediffComp_Chargeable comp = GetChargeComp(pawn);
            if (comp == null || comp.Props.fullChargeAmount <= 0f)
            {
                return false;
            }

            return Mathf.Clamp01(comp.Charge / comp.Props.fullChargeAmount) >= 0.999f;
        }

        /// <summary>
        /// Current fullness of the lactation charge as a 0..1 factor, plus the milk capacity.
        /// </summary>
        public static bool TryGetCharge(Pawn pawn, out float factor, out float capacity)
        {
            factor = 0f;
            capacity = 0f;

            if (pawn == null || pawn.Dead || pawn.Discarded || !MilkHelper.IsLactating(pawn))
            {
                return false;
            }

            capacity = GetCapacity(pawn);
            if (capacity <= 0f)
            {
                return false;
            }

            HediffComp_Chargeable comp = GetChargeComp(pawn);
            if (comp == null || comp.Props.fullChargeAmount <= 0f)
            {
                return false;
            }

            factor = Mathf.Clamp01(comp.Charge / comp.Props.fullChargeAmount);
            return true;
        }

        /// <summary>
        /// Extract all currently available milk into the milk item for the victim's xenotype,
        /// drain the shared lactation charge, and apply the forced milking social consequences
        /// when the victim is not willing. Draining the charge means any baby will have to wait
        /// for it to refill.
        /// </summary>
        public static void MilkPawn(Pawn victim, Pawn milker)
        {
            if (victim == null || victim.Map == null)
            {
                return;
            }

            if (!TryGetCharge(victim, out float factor, out float capacity))
            {
                return;
            }

            // Guard against producing milk below the configured minimum after the wait toil:
            // a baby or another milker may have drained the victim during those ticks.
            float available = capacity * factor;
            if (available < HuMilkCoMod.Settings.MinMilkToMilk)
            {
                return;
            }

            // Resolve the product before draining so a missing milk def can never destroy milk.
            ThingDef milkDef = MilkDefs.GetMilkForPawn(victim);
            if (milkDef == null)
            {
                return;
            }

            int count = Mathf.Max(1, Mathf.RoundToInt(available));

            HediffComp_Chargeable comp = GetChargeComp(victim);
            if (comp != null)
            {
                comp.TryCharge(-comp.Charge);
            }

            Thing milk = ThingMaker.MakeThing(milkDef);
            milk.stackCount = count;
            GenPlace.TryPlaceThing(milk, victim.Position, victim.Map, ThingPlaceMode.Near);

            if (!MilkHelper.IsWillingToBeMilked(victim))
            {
                ApplyForcedConsequences(victim, milker);
            }
            else if (OnAphrolactone(victim))
            {
                ApplyPleasureConsequences(victim);
            }
        }

        private static HediffComp_Chargeable GetChargeComp(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return null;
            }

            Hediff hediff = null;
            if (MilkDefs.AphrolactoneLactation != null)
            {
                hediff = pawn.health.hediffSet.GetFirstHediffOfDef(MilkDefs.AphrolactoneLactation);
            }

            if (hediff == null && MilkDefs.Lactating != null)
            {
                hediff = pawn.health.hediffSet.GetFirstHediffOfDef(MilkDefs.Lactating);
            }

            return hediff?.TryGetComp<HediffComp_Chargeable>();
        }

        private static bool OnAphrolactone(Pawn pawn)
        {
            return MilkDefs.AphrolactoneLactation != null &&
                   pawn.health != null && pawn.health.hediffSet != null &&
                   pawn.health.hediffSet.HasHediff(MilkDefs.AphrolactoneLactation);
        }

        private static void ApplyPleasureConsequences(Pawn victim)
        {
            if (victim.needs == null || victim.needs.mood == null || victim.needs.mood.thoughts == null)
            {
                return;
            }

            MemoryThoughtHandler memories = victim.needs.mood.thoughts.memories;
            if (memories == null)
            {
                return;
            }

            if (MilkDefs.PleasureFromMilking != null)
            {
                memories.RemoveMemoriesOfDef(MilkDefs.PleasureFromMilking);
                memories.TryGainMemory(MilkDefs.PleasureFromMilking, null, null);
            }
        }

        private static void ApplyForcedConsequences(Pawn victim, Pawn milker)
        {
            if (victim.needs == null || victim.needs.mood == null || victim.needs.mood.thoughts == null)
            {
                return;
            }

            MemoryThoughtHandler memories = victim.needs.mood.thoughts.memories;
            if (memories == null)
            {
                return;
            }

            if (MilkDefs.ForcedToBeMilked != null)
            {
                memories.RemoveMemoriesOfDef(MilkDefs.ForcedToBeMilked);
            }

            if (MilkDefs.ForcedToBeMilkedMood != null)
            {
                memories.RemoveMemoriesOfDef(MilkDefs.ForcedToBeMilkedMood);
            }

            if (MilkDefs.ForcedToBeMilked != null && milker != null)
            {
                memories.TryGainMemory(MilkDefs.ForcedToBeMilked, milker, null);
            }

            if (MilkDefs.ForcedToBeMilkedMood != null)
            {
                memories.TryGainMemory(MilkDefs.ForcedToBeMilkedMood, null, null);
            }
        }
    }
}