using System.Collections.Generic;
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
        private static readonly HashSet<string> RjwBondageDefs = new HashSet<string>
        {
            "RJW_Restraints",
            "BoundHands",
            "BoundLegs",
            "Chains",
            "RJW_Cocoon",
        };

        private static readonly HashSet<string> OnaholeBindingDefs = new HashSet<string>
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

            for (int i = 0; i < hediffSet.hediffs.Count; i++)
            {
                Hediff hediff = hediffSet.hediffs[i];
                if (hediff == null || hediff.def == null)
                {
                    continue;
                }

                string defName = hediff.def.defName;
                if (RjwBondageDefs.Contains(defName) || OnaholeBindingDefs.Contains(defName))
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
    }
}