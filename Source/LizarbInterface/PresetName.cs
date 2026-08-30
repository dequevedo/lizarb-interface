using System;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    internal sealed class Dialog_PresetName : Window
    {
        private readonly Action<string> accept;
        private string text;
        private bool focused;

        internal Dialog_PresetName(string start, Action<string> onAccept)
        {
            text = start;
            accept = onAccept;

            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnAccept = false;
            closeOnCancel = true;
            doCloseX = true;
        }

        public override Vector2 InitialSize => new Vector2(360f, 150f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 24f),
                          "LizarbInterface.PresetNamePrompt".Translate());

            var field = new Rect(inRect.x, inRect.y + 30f, inRect.width, 30f);
            GUI.SetNextControlName("LizarbPresetName");
            text = Widgets.TextField(field, text);

            if (!focused)
            {
                focused = true;
                UI.FocusControl("LizarbPresetName", this);
            }

            bool entered = Event.current.type == EventType.KeyDown &&
                           (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);

            var ok = new Rect(inRect.xMax - 150f, inRect.yMax - 34f, 150f, 32f);
            if (Widgets.ButtonText(ok, "OK".Translate(), active: !text.NullOrEmpty()) ||
                (entered && !text.NullOrEmpty()))
            {
                if (entered)
                {
                    Event.current.Use();
                }

                accept(text.Trim());
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - 34f, 150f, 32f),
                                   "CancelButton".Translate()))
            {
                Close();
            }
        }
    }
}
