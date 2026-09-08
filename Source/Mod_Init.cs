using System.Reflection;
using HarmonyLib;
using Verse;

namespace HuMilkCo
{
    [StaticConstructorOnStartup]
    public static class Mod_Init
    {
        static Mod_Init()
        {
            Harmony harmony = new Harmony("toasterbath.humilkco");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            RecipeMilkPatcher.PatchRecipes();
        }
    }
}