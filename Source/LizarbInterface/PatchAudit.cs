using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Verse;

namespace LizarbInterface
{
    [StaticConstructorOnStartup]
    internal static class PatchAudit
    {
        private const string OurId = "lizarb.interface";

        static PatchAudit()
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            try
            {
                Report();
            }
            catch (Exception e)
            {
                Log.Warning("[LizarbInterface] patch audit failed: " + e.Message);
            }
        }

        private static void Report()
        {
            var harmony = new Harmony(OurId);
            var shared = new List<string>();

            foreach (MethodBase method in harmony.GetPatchedMethods())
            {
                Patches info = Harmony.GetPatchInfo(method);
                if (info == null)
                {
                    continue;
                }

                string others = Describe(info);
                if (others.Length == 0)
                {
                    continue;
                }

                shared.Add("  " + method.DeclaringType?.Name + "." + method.Name + "  <-  " + others);
            }

            if (shared.Count == 0)
            {
                return;
            }

            var text = new StringBuilder();
            text.AppendLine("[LizarbInterface] patch audit: " + shared.Count +
                            " method(s) also patched by other mods.");
            foreach (string line in shared)
            {
                text.AppendLine(line);
            }

            Log.Message(text.ToString().TrimEnd());
        }

        private static string Describe(Patches info)
        {
            var owners = new Dictionary<string, HashSet<string>>();

            Collect(owners, info.Prefixes, "prefix");
            Collect(owners, info.Postfixes, "postfix");
            Collect(owners, info.Transpilers, "transpiler");
            Collect(owners, info.Finalizers, "finalizer");

            if (owners.Count == 0)
            {
                return "";
            }

            return string.Join(", ", owners
                .OrderBy(kv => kv.Key)
                .Select(kv => kv.Key + " (" + string.Join("+", kv.Value.OrderBy(v => v).ToArray()) + ")")
                .ToArray());
        }

        private static void Collect(
            Dictionary<string, HashSet<string>> owners,
            IEnumerable<Patch> patches,
            string kind)
        {
            if (patches == null)
            {
                return;
            }

            foreach (Patch patch in patches)
            {
                if (patch.owner == OurId)
                {
                    continue;
                }

                if (!owners.TryGetValue(patch.owner, out HashSet<string> kinds))
                {
                    kinds = new HashSet<string>();
                    owners[patch.owner] = kinds;
                }

                kinds.Add(kind);
            }
        }
    }
}
