using System;
using System.Linq.Expressions;
using System.Reflection;
using HarmonyLib;
using rjw;
using RimWorld;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Need-side effects of the Hucow trait. The recreation (joy) need is disabled - it is
    /// pinned full and never falls, so the pawn never needs recreation - and the RJW sex
    /// need decays faster. Both read the trait directly so they apply to any pawn carrying
    /// the trait.
    /// </summary>
    [HarmonyPatch(typeof(Need_Joy), nameof(Need_Joy.NeedInterval))]
    public static class Patch_NeedJoy_Hucow
    {
        private static Func<Need, Pawn> needPawnGetter;

        public static bool Prefix(Need_Joy __instance)
        {
            Pawn pawn = GetPawn(__instance);
            if (pawn != null && MilkHelper.IsHucow(pawn))
            {
                __instance.CurLevel = 1f;
                return false;
            }

            return true;
        }

        private static Pawn GetPawn(Need need)
        {
            if (needPawnGetter == null)
            {
                FieldInfo field = AccessTools.Field(typeof(Need), "pawn");
                if (field == null)
                {
                    return null;
                }

                ParameterExpression instance = Expression.Parameter(typeof(Need), "need");
                needPawnGetter = Expression.Lambda<Func<Need, Pawn>>(Expression.Field(instance, field), instance).Compile();
            }

            return needPawnGetter(need);
        }
    }

    [HarmonyPatch(typeof(Need_Sex), nameof(Need_Sex.GetFallFactorFor))]
    public static class Patch_NeedSex_Hucow
    {
        /// <summary>
        /// Multiplier applied to the RJW sex need's fall factor for Hucows. 2f = twice the
        /// normal decay rate (the need empties twice as fast).
        /// </summary>
        public const float HucowSexNeedDecayFactor = 2f;

        public static void Postfix(Pawn pawn, ref float __result)
        {
            if (MilkHelper.IsHucow(pawn))
            {
                __result *= HucowSexNeedDecayFactor;
            }
        }
    }
}