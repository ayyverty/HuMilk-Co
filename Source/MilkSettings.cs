using RimWorld;
using UnityEngine;
using Verse;

namespace HuMilkCo
{
    public class MilkSettings : ModSettings
    {
        public float LitresToMilkUnits = 2.5f;
        public float MinMilkToMilk = 1f;
        public int RequiredAnimalsSkill = 3;
        public int MilkWorkTicks = 1800;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref LitresToMilkUnits, "litresToMilkUnits", 2.5f);
            Scribe_Values.Look(ref MinMilkToMilk, "minMilkToMilk", 1f);
            Scribe_Values.Look(ref RequiredAnimalsSkill, "requiredAnimalsSkill", 3);
            Scribe_Values.Look(ref MilkWorkTicks, "milkWorkTicks", 1800);
        }

        public void DoWindowContents(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            listing.Label("HuMilk Co settings");
            listing.Gap();

            LitresToMilkUnits = listing.SliderLabeled(
                "Milk units per litre of breast volume: " + LitresToMilkUnits.ToString("0.0"),
                LitresToMilkUnits, 0.5f, 10f);

            MinMilkToMilk = listing.SliderLabeled(
                "Minimum milk required before a pawn can be milked: " + MinMilkToMilk.ToString("0.0"),
                MinMilkToMilk, 0.5f, 10f);
            listing.Gap();

            RequiredAnimalsSkill = (int)listing.SliderLabeled(
                "Required Animals skill to milk: " + RequiredAnimalsSkill,
                RequiredAnimalsSkill, 0f, 20f);

            MilkWorkTicks = (int)listing.SliderLabeled(
                "Ticks spent milking per session: " + MilkWorkTicks,
                MilkWorkTicks, 100f, 10000f);

            listing.End();
        }
    }
}