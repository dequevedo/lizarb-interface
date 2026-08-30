using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    [StaticConstructorOnStartup]
    internal static class ColourOwner
    {
        internal static bool Hijacked { get; private set; }

        static ColourOwner()
        {
            try
            {
                MethodInfo draw = AccessTools.Method(
                    typeof(GUI), "DrawTexture", new[] { typeof(Rect), typeof(Texture) });
                if (draw == null)
                {
                    return;
                }

                Patches info = Harmony.GetPatchInfo(draw);
                if (info == null)
                {
                    return;
                }

                var others = new List<string>();
                foreach (Patch p in info.Prefixes)
                {
                    if (p.owner != LizarbInterfaceMod.HarmonyId)
                    {
                        others.Add(p.owner);
                    }
                }

                if (others.Count == 0)
                {
                    return;
                }

                Hijacked = true;

                if (Prefs.DevMode)
                {
                    Log.Message("[LizarbInterface] another mod owns GUI.DrawTexture colour (" +
                                string.Join(", ", others.ToArray()) +
                                "); the plate behind icon buttons stands down.");
                }
            }
            catch (Exception e)
            {
                Log.Warning("[LizarbInterface] could not read who owns GUI.DrawTexture: " + e.Message);
            }
        }
    }
}
