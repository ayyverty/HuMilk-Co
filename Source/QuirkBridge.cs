using System;
using System.Linq.Expressions;
using System.Reflection;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Reflection bridge into the rjw-quirks-fork assembly. HuMilk Co does not compile
    /// against the fork (it is a soft dependency), so any call into its quirk system goes
    /// through here. All reflection handles are resolved lazily and cached; every method
    /// returns safely (false / null / no-op) when the fork is absent.
    /// </summary>
    public static class QuirkBridge
    {
        private static MethodInfo hasQuirkMethod;
        private static Func<Pawn, Def, bool> hasQuirkFunc;
        private static bool forkActiveChecked;
        private static bool forkActive;

        private static bool ForkActive
        {
            get
            {
                if (!forkActiveChecked)
                {
                    forkActiveChecked = true;
                    forkActive = ModsConfig.IsActive("rjw.quirks.fork");
                }

                return forkActive;
            }
        }

        /// <summary>
        /// Returns true if <paramref name="pawn"/> has the given quirk def. Returns false
        /// when the fork isn't loaded, the def's class is unknown, or <paramref name="def"/>
        /// is null.
        /// </summary>
        public static bool HasQuirk(Pawn pawn, Def def)
        {
            if (pawn == null || def == null || !ForkActive)
            {
                return false;
            }

            if (hasQuirkFunc == null)
            {
                if (hasQuirkMethod == null)
                {
                    Type pawnExtType = Type.GetType("RJWQuirksFork.PawnExtensions, RJWQuirksFork");
                    if (pawnExtType == null)
                    {
                        return false;
                    }

                    hasQuirkMethod = pawnExtType.GetMethod(
                        "HasQuirk",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new[] { typeof(Pawn), def.GetType() },
                        null);
                }

                if (hasQuirkMethod == null)
                {
                    return false;
                }

                // Compile a direct call (with an internal cast to the quirk def type) so the
                // hot candidate scan never pays for reflection Invoke or per-call array boxing.
                ParameterExpression pawnParam = Expression.Parameter(typeof(Pawn), "pawn");
                ParameterExpression defParam = Expression.Parameter(typeof(Def), "def");
                MethodCallExpression call = Expression.Call(
                    hasQuirkMethod, pawnParam, Expression.Convert(defParam, def.GetType()));
                hasQuirkFunc = Expression.Lambda<Func<Pawn, Def, bool>>(call, pawnParam, defParam).Compile();
            }

            return hasQuirkFunc(pawn, def);
        }
    }
}
