using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Verse;

namespace LizarbInterface
{
    /// <summary>
    /// Logs every other mod that patches a method this mod also patches, so a conflict
    /// report is one line in the log instead of guesswork.
    ///
    /// Reading it: a transpiler alongside our prefix/postfix composes fine. Another
    /// PREFIX on TabRecord.Draw, DrawWindowBackground or DrawMenuSection is the one to
    /// watch, since ours returns false and the first to do so suppresses the rest.
    ///
    /// Runs at StaticConstructorOnStartup, so it misses mods that patch later.
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class PatchAudit
    {
        private const string OurId = "lizarb.interface";

        static PatchAudit()
        {
            try
            {
                Report();
            }
            catch (Exception e)
            {
                // A diagnostic must never be what breaks startup.
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
                Log.Message("[LizarbInterface] patch audit: no other mod patches anything this mod patches.");
                return;
            }

            var text = new StringBuilder();
            text.AppendLine("[LizarbInterface] patch audit: " + shared.Count +
                            " method(s) also patched by other mods.");
            foreach (string line in shared)
            {
                text.AppendLine(line);
            }

            // Message, not Warning: sharing a method is normal and usually harmless.
            Log.Message(text.ToString().TrimEnd());
        }

        /// <summary>Owners other than us, tagged with the kind of patch each one is.</summary>
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
