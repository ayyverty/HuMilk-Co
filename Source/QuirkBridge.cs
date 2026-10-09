using System;
using System.Linq.Expressions;
using System.Reflection;
using Verse;

namespace HuMilkCo
{
    /// <summary>
    /// Reflection bridge into whichever RJW Quirks assembly is loaded: "RJWQuirks" for the
    /// original mod (rjw.quirks) or "RJWQuirksFork" for the fork (rjw.quirks.fork). HuMilk Co
    /// does not compile against either (they are soft dependencies), so every call into the
    /// quirk system goes through here. Handles are resolved lazily and cached; all methods
    /// return safely (false / null / no-op) when no quirks mod is present.
    /// </summary>
    public static class QuirkBridge
    {
        private static readonly string[] AssemblyNames = { "RJWQuirksFork", "RJWQuirks" };

        private static bool quirkDefTypeResolved;
        private static Type quirkDefType;

        private static Func<Pawn, Def, bool> hasQuirkFunc;
        private static Type hasQuirkDefType;

        /// <summary>
        /// The QuirkDef type of the loaded quirks mod (the fork is preferred when both are
        /// present), or null when neither is loaded.
        /// </summary>
        public static Type QuirkDefType
        {
            get
            {
                if (!quirkDefTypeResolved)
                {
                    quirkDefTypeResolved = true;
                    quirkDefType = FindQuirkDefType();
                }

                return quirkDefType;
            }
        }

        private static Type FindQuirkDefType()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                for (int i = 0; i < AssemblyNames.Length; i++)
                {
                    if (name == AssemblyNames[i])
                    {
                        Type type = assembly.GetType(name + ".QuirkDef");
                        if (type != null)
                        {
                            return type;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true if <paramref name="pawn"/> has the given quirk def. Returns false
        /// when no quirks mod is loaded, the def's class is unknown, or <paramref name="def"/>
        /// is null.
        /// </summary>
        public static bool HasQuirk(Pawn pawn, Def def)
        {
            if (pawn == null || def == null)
            {
                return false;
            }

            Type defType = def.GetType();
            if (hasQuirkFunc == null || hasQuirkDefType != defType)
            {
                if (!TryBuildHasQuirk(defType))
                {
                    return false;
                }
            }

            return hasQuirkFunc(pawn, def);
        }

        private static bool TryBuildHasQuirk(Type defType)
        {
            // Resolve PawnExtensions from the same assembly that defines the quirk def, so the
            // compiled call always matches the def type. Original and fork def types are
            // unrelated, so the wrong assembly's method could never be called.
            Assembly assembly = defType.Assembly;
            string assemblyName = assembly.GetName().Name;
            Type pawnExtType = assembly.GetType(assemblyName + ".PawnExtensions");
            if (pawnExtType == null)
            {
                return false;
            }

            MethodInfo hasQuirkMethod = pawnExtType.GetMethod(
                "HasQuirk",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Pawn), defType },
                null);
            if (hasQuirkMethod == null)
            {
                return false;
            }

            // Compile a direct call (with an internal cast to the quirk def type) so the
            // hot candidate scan never pays for reflection Invoke or per-call array boxing.
            ParameterExpression pawnParam = Expression.Parameter(typeof(Pawn), "pawn");
            ParameterExpression defParam = Expression.Parameter(typeof(Def), "def");
            MethodCallExpression call = Expression.Call(
                hasQuirkMethod, pawnParam, Expression.Convert(defParam, defType));
            hasQuirkFunc = Expression.Lambda<Func<Pawn, Def, bool>>(call, pawnParam, defParam).Compile();
            hasQuirkDefType = defType;
            return true;
        }
    }
}
