using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Mood is a need, not a stat, so a HediffStage cannot carry a mood bonus through
    /// statOffsets. This comp holds the mood value for a hediff instead and keeps a
    /// single memory thought alive on the pawn while the hediff lasts. The thought
    /// reads its value back off whichever mood comps are running, so one shared
    /// thought def covers every milk effect and no per-effect def is needed.
    ///
    /// A hediff may supply either a flat <see cref="offset"/> or a per-stage
    /// <see cref="stageOffsets"/> list. The xenotype-split effects (waster,
    /// sanguophage) pick their stage by xenotype rather than severity, so they need
    /// the per-stage form.
    /// </summary>
    public class HediffCompProperties_MoodOffset : HediffCompProperties
    {
        /// <summary>Flat mood offset, used when no stageOffsets are supplied.</summary>
        public float offset;

        /// <summary>
        /// Per-stage mood offsets aligned with the hediff's stage list. When present
        /// this takes precedence over <see cref="offset"/>.
        /// </summary>
        public List<float> stageOffsets;

        public HediffCompProperties_MoodOffset()
        {
            compClass = typeof(HediffComp_MoodOffset);
        }
    }

    public class HediffComp_MoodOffset : HediffComp
    {
        /// <summary>How often (in ticks) to re-assert the thought while it is active.</summary>
        private const int RefreshIntervalTicks = 2500;

        public HediffCompProperties_MoodOffset Props => (HediffCompProperties_MoodOffset)props;

        /// <summary>
        /// The mood this comp is worth right now, resolved against the parent hediff's
        /// current stage so the xenotype-split effects read the correct branch.
        /// </summary>
        public float CurrentOffset
        {
            get
            {
                HediffCompProperties_MoodOffset p = Props;
                if (p == null)
                {
                    return 0f;
                }

                List<float> stageOffsets = p.stageOffsets;
                if (stageOffsets == null || stageOffsets.Count == 0)
                {
                    return p.offset;
                }

                int index = parent?.CurStageIndex ?? 0;
                if (index < 0 || index >= stageOffsets.Count)
                {
                    return 0f;
                }

                return stageOffsets[index];
            }
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            RefreshMoodThought();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            // The thought carries its own duration, but re-asserting on an interval
            // covers any case where it was purged (for example by a mood reset).
            Pawn pawn = parent?.pawn;
            if (pawn != null && pawn.IsHashIntervalTick(RefreshIntervalTicks))
            {
                RefreshMoodThought();
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            RemoveMoodThoughtIfIdle();
        }

        /// <summary>
        /// True when the pawn carries a live milk-effect mood comp other than
        /// <paramref name="exclude"/>, so a shared thought is only torn down once the
        /// last one is gone. The comp being removed is excluded explicitly because
        /// the engine may still have its hediff in the set at this point.
        /// </summary>
        internal static bool HasActiveMoodComp(Pawn pawn, Hediff exclude)
        {
            List<Hediff> hediffs = pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null)
            {
                return false;
            }

            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (hediff == exclude)
                {
                    continue;
                }

                if (hediff.TryGetComp<HediffComp_MoodOffset>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sums the current mood of every active mood comp on the pawn. Read live by
        /// <see cref="Thought_MilkEffectMood"/> so a single thought reflects however
        /// many effects happen to be running.
        /// </summary>
        internal static float TotalMoodOffset(Pawn pawn)
        {
            List<Hediff> hediffs = pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null)
            {
                return 0f;
            }

            float total = 0f;

            for (int i = 0; i < hediffs.Count; i++)
            {
                HediffComp_MoodOffset moodComp = hediffs[i].TryGetComp<HediffComp_MoodOffset>();
                if (moodComp != null)
                {
                    total += moodComp.CurrentOffset;
                }
            }

            return total;
        }

        /// <summary>
        /// Gains the shared mood thought if it isn't already live. A no-op when the
        /// pawn has no mood, the thought def is missing, or this comp contributes
        /// nothing, so a null/optional setup degrades quietly.
        /// </summary>
        private void RefreshMoodThought()
        {
            Pawn pawn = parent?.pawn;
            Need_Mood mood = pawn?.needs?.mood;
            if (mood?.thoughts?.memories == null)
            {
                return;
            }

            ThoughtDef thoughtDef = MilkDefs.MilkEffectMood;
            if (thoughtDef == null || CurrentOffset == 0f)
            {
                return;
            }

            if (mood.thoughts.memories.Memories.Count > 0)
            {
                for (int i = 0; i < mood.thoughts.memories.Memories.Count; i++)
                {
                    if (mood.thoughts.memories.Memories[i].def == thoughtDef)
                    {
                        // Already live; its duration outlasts the refresh interval.
                        return;
                    }
                }
            }

            mood.thoughts.memories.TryGainMemory(thoughtDef, null, null);
        }

        /// <summary>
        /// Drops the shared mood thought once no milk-effect mood comp remains.
        /// </summary>
        private void RemoveMoodThoughtIfIdle()
        {
            Pawn pawn = parent?.pawn;
            Need_Mood mood = pawn?.needs?.mood;
            if (mood?.thoughts?.memories == null)
            {
                return;
            }

            if (HasActiveMoodComp(pawn, parent))
            {
                return;
            }

            ThoughtDef thoughtDef = MilkDefs.MilkEffectMood;
            if (thoughtDef != null)
            {
                mood.thoughts.memories.RemoveMemoriesOfDef(thoughtDef);
            }
        }
    }

    /// <summary>
    /// The single mood thought shared by every milk effect. Its value is read live
    /// from the pawn's active <see cref="HediffComp_MoodOffset"/> comps rather than
    /// taken from the def, so one def covers positive and negative, single-stage and
    /// xenotype-split effects at once.
    /// </summary>
    public class Thought_MilkEffectMood : Thought_Memory
    {
        public override float MoodOffset()
        {
            return HediffComp_MoodOffset.TotalMoodOffset(pawn);
        }

        /// <summary>
        /// Merges into an existing instance rather than stacking, so the shared
        /// thought can never double-count by having several copies alive at once.
        /// </summary>
        public override bool TryMergeWithExistingMemory(out bool showBubble)
        {
            showBubble = false;
            return true;
        }
    }
}