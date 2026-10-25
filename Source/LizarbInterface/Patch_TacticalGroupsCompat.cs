using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace LizarbInterface
{
    [StaticConstructorOnStartup]
    internal static class TacticalGroupsCompat
    {
        private const string TargetType = "TacticalGroups.TieredFloatMenuOption";

        static TacticalGroupsCompat()
        {
            try
            {
                Harmony harmony = LizarbInterfaceMod.Harmony;
                if (harmony == null)
                {
                    return;
                }

                Type type = AccessTools.TypeByName(TargetType);
                MethodInfo target = type == null ? null : AccessTools.Method(type, "DoGUI");
                if (target == null)
                {
                    return;
                }

                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(typeof(TacticalGroupsCompat), nameof(Prefix)),
                    finalizer: new HarmonyMethod(typeof(TacticalGroupsCompat), nameof(Finalizer)));
            }
            catch (Exception e)
            {
                Log.Warning("[LizarbInterface] could not defer to Colony Groups: " + e.Message);
            }
        }

        private static void Prefix(out bool __state)
        {
            __state = AtlasSwap.Bypass;
            AtlasSwap.Bypass = true;
        }

        private static void Finalizer(bool __state)
        {
            AtlasSwap.Bypass = __state;
        }
    }
}
