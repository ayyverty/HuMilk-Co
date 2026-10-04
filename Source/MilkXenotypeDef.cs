using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Links a xenotype to the milk its pawns produce and the timed effect that milk
    /// grants when drunk. One entry per supported xenotype; custom xenotypes from other
    /// mods can be mapped by adding their own entries.
    /// </summary>
    public class MilkXenotypeDef : Def
    {
        public XenotypeDef xenotype;

        public ThingDef milk;

        public HediffDef effect;
    }
}