using System;
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

        /// <summary>
        /// Returns true if <paramref name="pawn"/> has the given quirk def. Returns false
        /// when the fork isn't loaded, the def's class is unknown, or <paramref name="def"/>
        /// is null.
        /// </summary>
        public static bool HasQuirk(Pawn pawn, Def def)
        {
            if (pawn == null || def == null || !ModsConfig.IsActive("rjw.quirks.fork"))
            {
                return false;
            }

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

            return (bool)hasQuirkMethod.Invoke(null, new object[] { pawn, def });
        }
    }
}
