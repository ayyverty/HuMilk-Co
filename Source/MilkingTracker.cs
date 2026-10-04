using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Records successful milkings for Hucow progression. Every milking bumps the pawn's
    /// counter and refreshes its last-milked tick; once the counter passes 20, each further
    /// milking rolls a 20% chance to gain the Hucow trait. Hucows also receive the Freshly
    /// Milked mood when milked.
    /// </summary>
    public static class MilkingTracker
    {
        public const int MilkingsBeforeTraitRoll = 20;

        public const float HucowTraitChancePerMilking = 0.2f;

        public static void RecordMilking(Pawn victim)
        {
            if (victim == null)
            {
                return;
            }

            Hediff_MilkingProgress progress = EnsureProgress(victim);
            if (progress == null)
            {
                return;
            }

            progress.milkingCount++;
            progress.lastMilkedTick = GenTicks.TicksGame;

            TryGainHucowTrait(victim, progress);

            if (MilkHelper.IsHucow(victim))
            {
                ApplyFreshlyMilked(victim);
            }
        }

        private static Hediff_MilkingProgress EnsureProgress(Pawn pawn)
        {
            if (pawn.health == null || pawn.health.hediffSet == null || MilkDefs.MilkingProgress == null)
            {
                return null;
            }

            Hediff_MilkingProgress progress =
                pawn.health.hediffSet.GetFirstHediffOfDef(MilkDefs.MilkingProgress) as Hediff_MilkingProgress;
            if (progress != null)
            {
                return progress;
            }

            return pawn.health.AddHediff(MilkDefs.MilkingProgress) as Hediff_MilkingProgress;
        }

        private static void TryGainHucowTrait(Pawn pawn, Hediff_MilkingProgress progress)
        {
            if (MilkDefs.Hucow == null || pawn.story?.traits == null)
            {
                return;
            }

            if (pawn.story.traits.HasTrait(MilkDefs.Hucow))
            {
                return;
            }

            if (progress.milkingCount < MilkingsBeforeTraitRoll)
            {
                return;
            }

            if (Rand.Value >= HucowTraitChancePerMilking)
            {
                return;
            }

            pawn.story.traits.GainTrait(new Trait(MilkDefs.Hucow));
        }

        private static void ApplyFreshlyMilked(Pawn pawn)
        {
            if (MilkDefs.FreshlyMilkedMood == null || pawn.needs?.mood?.thoughts?.memories == null)
            {
                return;
            }

            MemoryThoughtHandler memories = pawn.needs.mood.thoughts.memories;
            memories.RemoveMemoriesOfDef(MilkDefs.FreshlyMilkedMood);
            memories.TryGainMemory(MilkDefs.FreshlyMilkedMood, null, null);
        }
    }
}