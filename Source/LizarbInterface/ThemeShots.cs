using System.Collections.Generic;
using System.IO;
using LudeonTK;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    internal sealed class ThemeShotSequence : Window
    {
        private const int SettleFrames = 10;
        private const int WriteFrames = 6;
        private const string FolderName = "LizarbThemes";

        internal const string Vanilla = "Vanilla";

        private readonly List<string> queue;
        private readonly List<Window> hidden;
        private readonly string folder;

        private readonly bool hadEnabled;
        private readonly string hadTheme;
        private readonly string hadPattern;

        private int index;
        private int wait;
        private bool armed;
        private bool restored;

        internal ThemeShotSequence(List<string> themes, List<Window> devWindows, string dir)
        {
            queue = themes;
            hidden = devWindows;
            folder = dir;

            LizarbInterfaceSettings s = LizarbInterfaceMod.Settings;
            hadEnabled = s.enabled;
            hadTheme = s.theme;
            hadPattern = s.backgroundPattern;

            layer = WindowLayer.Super;
            doWindowBackground = false;
            drawShadow = false;
            focusWhenOpened = false;
            preventCameraMotion = false;
            closeOnAccept = false;
            closeOnCancel = false;
            onlyOneOfTypeAllowed = true;
        }

        public override Vector2 InitialSize => new Vector2(1f, 1f);

        protected override void SetInitialSizeAndPosition()
        {
            windowRect = new Rect(-64f, -64f, 1f, 1f);
        }

        public override void DoWindowContents(Rect inRect)
        {
        }

        public override void WindowUpdate()
        {
            if (wait > 0)
            {
                wait--;
                return;
            }

            if (armed)
            {
                Shoot(index, queue[index]);
                armed = false;
                index++;
                wait = WriteFrames;
                return;
            }

            if (index >= queue.Count)
            {
                Log.Message("[LizarbInterface] " + queue.Count + " theme shot(s) written to " + folder);
                Close(false);
                return;
            }

            Wear(queue[index]);
            armed = true;
            wait = SettleFrames;
        }

        public override void PreClose()
        {
            base.PreClose();
            Restore();
        }

        private void Shoot(int at, string theme)
        {
            string file = Path.Combine(folder, (at + 1).ToString("00") + "-" + theme + ".png");
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            ScreenCapture.CaptureScreenshot(file);
        }

        private static void Wear(string theme)
        {
            LizarbInterfaceSettings s = LizarbInterfaceMod.Settings;

            if (theme == Vanilla)
            {
                s.enabled = false;
                FontEngine.Apply();
                return;
            }

            s.enabled = true;
            s.theme = theme;

            foreach (var entry in LizarbInterfaceMod.AllThemes)
            {
                if (entry.Id == theme)
                {
                    s.backgroundPattern = entry.Pattern;
                    break;
                }
            }

            FontEngine.Apply();
        }

        private void Restore()
        {
            if (restored)
            {
                return;
            }

            restored = true;

            LizarbInterfaceSettings s = LizarbInterfaceMod.Settings;
            s.enabled = hadEnabled;
            s.theme = hadTheme;
            s.backgroundPattern = hadPattern;
            FontEngine.Apply();

            foreach (Window w in hidden)
            {
                Find.WindowStack.Add(w);
            }
        }

        internal static string Folder()
        {
            string dir = Path.Combine(GenFilePaths.ScreenshotFolderPath, FolderName);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return dir;
        }
    }

    internal static class ThemeShots
    {
        [DebugAction("Lizarb Interface", name = "Reload skin textures",
                     allowedGameStates = AllowedGameStates.Invalid)]
        private static void ReloadSkins()
        {
            AtlasSwap.Forget();
            FontEngine.Apply();
            Log.Message("[LizarbInterface] skin textures reloaded from disk");
        }

        [DebugAction("Lizarb Interface", name = "Shoot every theme",
                     allowedGameStates = AllowedGameStates.Invalid)]
        private static void ShootEveryTheme()
        {
            Run(false);
        }

        [DebugAction("Lizarb Interface", name = "Shoot every theme, vanilla first",
                     allowedGameStates = AllowedGameStates.Invalid)]
        private static void ShootWithVanilla()
        {
            Run(true);
        }

        private static void Run(bool withVanilla)
        {
            if (LizarbInterfaceMod.Settings == null)
            {
                return;
            }

            var themes = new List<string>();
            if (withVanilla)
            {
                themes.Add(ThemeShotSequence.Vanilla);
            }

            foreach (var entry in LizarbInterfaceMod.AllThemes)
            {
                themes.Add(entry.Id);
            }

            Find.WindowStack.Add(new ThemeShotSequence(themes, StowDevWindows(), ThemeShotSequence.Folder()));
        }

        private static List<Window> StowDevWindows()
        {
            var stowed = new List<Window>();

            foreach (Window w in Find.WindowStack.Windows)
            {
                if (w is Window_Dev)
                {
                    stowed.Add(w);
                }
            }

            foreach (Window w in stowed)
            {
                Find.WindowStack.TryRemove(w, false);
            }

            return stowed;
        }
    }
}
