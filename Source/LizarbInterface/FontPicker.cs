using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    /// <summary>
    /// Font grid where every name is drawn in its own font, so a face can be judged
    /// without selecting it first.
    ///
    /// A plain FloatMenu cannot do this: it draws its own label with the current
    /// style, and there is no hook between the row and that draw. A window of our
    /// own costs one class and no Harmony patch.
    /// </summary>
    internal class Dialog_FontPicker : Window
    {
        private const float CellHeight = 38f;
        private const float MinCellWidth = 240f;
        private const int PreviewSize = 19;

        private readonly List<string> shipped = new List<string>();
        private readonly List<string> local = new List<string>();
        private readonly string selected;
        private readonly Action<string> onPick;
        private Vector2 scroll;

        public Dialog_FontPicker(List<string> names, string selected, Action<string> onPick)
        {
            this.selected = selected;
            this.onPick = onPick;

            // Split so the answer to "will every player have this one?" is visible
            // without asking. The bundled faces are identical for everyone; the rest
            // are whatever this machine happens to have.
            foreach (string name in names)
            {
                if (FontBundle.Get(name) != null)
                {
                    shipped.Add(name);
                }
                else
                {
                    local.Add(name);
                }
            }

            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            draggable = true;
        }

        public override Vector2 InitialSize => new Vector2(740f, 640f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 28f),
                          "LizarbInterface.FontSection".Translate());

            Rect body = new Rect(inRect.x, inRect.y + 32f, inRect.width, inRect.height - 32f);
            float width = body.width - 20f;
            int columns = Mathf.Max(1, Mathf.FloorToInt(width / MinCellWidth));

            float height = CellHeight
                         + Section(shipped, columns, measureOnly: true)
                         + Section(local, columns, measureOnly: true);

            var view = new Rect(0f, 0f, width, height);
            Widgets.BeginScrollView(body, ref scroll, view);

            cursorY = 0f;
            viewWidth = width;
            this.columns = columns;

            Cell(new Rect(0f, cursorY, width, CellHeight), "", "LizarbInterface.FontVanilla".Translate(), null);
            cursorY += CellHeight;

            Draw(shipped, "LizarbInterface.FontGroup.Shipped");
            Draw(local, "LizarbInterface.FontGroup.Local");

            Widgets.EndScrollView();
        }

        private float cursorY;
        private float viewWidth;
        private int columns;

        /// <summary>Height a group needs, headers included. Nothing is drawn.</summary>
        private float Section(List<string> names, int cols, bool measureOnly)
        {
            if (names.Count == 0)
            {
                return 0f;
            }

            return CellHeight + Mathf.CeilToInt(names.Count / (float)cols) * CellHeight;
        }

        private void Draw(List<string> names, string headerKey)
        {
            if (names.Count == 0)
            {
                return;
            }

            Header(new Rect(0f, cursorY, viewWidth, CellHeight), headerKey.Translate());
            cursorY += CellHeight;

            float cellWidth = viewWidth / columns;
            for (int i = 0; i < names.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                var rect = new Rect(col * cellWidth, cursorY + row * CellHeight, cellWidth, CellHeight);
                Cell(rect, names[i], names[i], names[i]);
            }

            cursorY += Mathf.CeilToInt(names.Count / (float)columns) * CellHeight;
        }

        private void Header(Rect rect, string label)
        {
            if (Offscreen(rect))
            {
                return;
            }

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            Widgets.Label(new Rect(rect.x + 6f, rect.y + 10f, rect.width - 12f, rect.height - 10f), label);
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            Widgets.DrawLineHorizontal(rect.x + 6f, rect.yMax - 4f, rect.width - 12f);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        /// <summary>
        /// Rows outside the scroll viewport are skipped before any font is resolved.
        /// Without that, opening the grid with "show every installed font" on would
        /// build a dynamic font for every family on the machine at once.
        /// </summary>
        private bool Offscreen(Rect rect)
        {
            return rect.yMax < scroll.y || rect.yMin > scroll.y + InitialSize.y;
        }

        /// <summary>previewName null draws with whatever font is already active.</summary>
        private void Cell(Rect rect, string value, string label, string previewName)
        {
            if (Offscreen(rect))
            {
                return;
            }

            Rect inner = rect.ContractedBy(2f);

            if (value == selected)
            {
                Widgets.DrawHighlightSelected(inner);
            }
            else if (Mouse.IsOver(inner))
            {
                Widgets.DrawHighlight(inner);
            }

            Rect text = inner;
            text.xMin += 8f;

            Font font = previewName == null ? null : FontEngine.Preview(previewName, PreviewSize);
            if (font == null)
            {
                Widgets.Label(text, label);
            }
            else
            {
                // Text.fontStyles holds shared GUIStyle objects; swapping the font on
                // one for a single draw is the trick FontEngine uses globally. It MUST
                // be restored, or every later label in this frame inherits it.
                GUIStyle style = Text.CurFontStyle;
                Font previousFont = style.font;
                int previousSize = style.fontSize;

                style.font = font;
                style.fontSize = PreviewSize;
                try
                {
                    Widgets.Label(text, label);
                }
                finally
                {
                    style.font = previousFont;
                    style.fontSize = previousSize;
                }
            }

            if (Widgets.ButtonInvisible(inner))
            {
                onPick(value);
                Close();
            }
        }
    }
}
