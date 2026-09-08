using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Decides whether a pawn counts as "bound" for the forced-milking path:
    /// any RJW bondage hediff, any Onahole binding hediff, or being effectively
    /// unconscious (downed / anesthetized / no consciousness).
    /// </summary>
    public static class BoundHelper
    {
        private static readonly string[] RjwBondageDefs =
        {
            "RJW_Restraints",
            "BoundHands",
            "BoundLegs",
            "Chains",
            "RJW_Cocoon",
        };

        private static readonly string[] OnaholeBindingDefs =
        {
            "OnaholeBond",
            "OnaholeMilkingMachine",
            "OnaholeVaginalSexMachine",
            "OnaholeAnalSexMachine",
            "OnaholeSexMachine",
            "OnaholeBreastElectrodes",
        };

        public static bool IsBound(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
            {
                return false;
            }

            HediffSet hediffSet = pawn.health.hediffSet;

            foreach (Hediff hediff in hediffSet.hediffs)
            {
                if (hediff == null || hediff.def == null)
                {
                    continue;
                }

                string defName = hediff.def.defName;
                if (Contains(RjwBondageDefs, defName) || Contains(OnaholeBindingDefs, defName))
                {
                    return true;
                }
            }

            return IsUnconscious(pawn);
        }

        private static bool IsUnconscious(Pawn pawn)
        {
            if (pawn.Downed)
            {
                return true;
            }

            if (pawn.health.hediffSet.HasHediff(HediffDefOf.Anesthetic, false))
            {
                return true;
            }

            PawnCapacitiesHandler capacities = pawn.health.capacities;
            if (capacities != null)
            {
                return capacities.GetLevel(PawnCapacityDefOf.Consciousness) <= 0f;
            }

            return false;
        }

        private static bool Contains(string[] array, string value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == value)
                {
                    return true;
                }
            }

            return false;
        }
    }
}