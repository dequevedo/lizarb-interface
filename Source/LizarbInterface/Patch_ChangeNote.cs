using System;
using System.IO;
using HarmonyLib;
using Steamworks;
using Verse;
using Verse.Steam;

namespace LizarbInterface
{
    [HarmonyPatch(typeof(SteamUGC), nameof(SteamUGC.SubmitItemUpdate))]
    internal static class Patch_ChangeNote
    {
        private const string File = "ChangeNote.txt";

        private static void Prefix(ref string pchChangeNote)
        {
            try
            {
                WorkshopItemHook hook =
                    AccessTools.StaticFieldRefAccess<WorkshopItemHook>(typeof(Workshop), "uploadingHook");

                DirectoryInfo dir = hook?.Directory;
                if (dir == null)
                {
                    return;
                }

                string path = Path.Combine(Path.Combine(dir.FullName, "About"), File);
                if (!System.IO.File.Exists(path))
                {
                    return;
                }

                string note = System.IO.File.ReadAllText(path).Trim();
                if (note.Length > 0)
                {
                    pchChangeNote = note;
                }
            }
            catch (Exception e)
            {
                Log.Warning("[LizarbInterface] could not read the change note: " + e.Message);
            }
        }
    }
}
