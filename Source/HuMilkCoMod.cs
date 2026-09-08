using Verse;

namespace HuMilkCo
{
    public class HuMilkCoMod : Mod
    {
        public static MilkSettings Settings;

        public HuMilkCoMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<MilkSettings>();
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "HuMilk Co";
        }
    }
}