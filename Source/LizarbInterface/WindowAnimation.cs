using UnityEngine;

namespace LizarbInterface
{
    /// <summary>
    /// The shapes a window opening can take. Every one of these is an affine
    /// transform on GUI.matrix plus an alpha, which is the whole reason they are
    /// cheap to add: the machinery to save, apply and restore the matrix already
    /// exists in Patch_WindowMotion.
    ///
    /// The same caveat governs all of them. A transformed matrix moves the text
    /// caret and the click areas relative to what is drawn, which is harmless for a
    /// third of a second and wrong for a window someone is typing in. That is why
    /// these are opening animations and never idle ones.
    /// </summary>
    internal static class WindowAnimation
    {
        internal static readonly string[] Styles =
        {
            "Scale", "Pop", "Zoom", "Rise", "Slide", "Unfold", "Flip", "Tilt", "Fade",
        };

        private static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        /// <summary>Overshoots past 1 and settles back, which is what reads as a pop.</summary>
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        /// <summary>
        /// Builds the transform for a style at a given progress and reports the alpha
        /// it wants. Returns false when the style needs no matrix at all.
        /// </summary>
        internal static bool Transform(string style, float progress, Vector2 pivot,
                                       out Matrix4x4 matrix, out float alpha)
        {
            float eased = EaseOutCubic(progress);
            alpha = eased;
            matrix = Matrix4x4.identity;

            float scaleX = 1f;
            float scaleY = 1f;
            float angle = 0f;
            Vector2 offset = Vector2.zero;

            switch (style)
            {
                case "Fade":
                    return false;

                case "Pop":
                    scaleX = scaleY = Mathf.Lerp(0.86f, 1f, EaseOutBack(progress));
                    break;

                case "Zoom":
                    scaleX = scaleY = Mathf.Lerp(1.08f, 1f, eased);
                    break;

                case "Rise":
                    offset.y = Mathf.Lerp(28f, 0f, eased);
                    break;

                case "Slide":
                    offset.x = Mathf.Lerp(-36f, 0f, eased);
                    break;

                case "Unfold":
                    scaleY = Mathf.Lerp(0.55f, 1f, eased);
                    break;

                case "Flip":
                    scaleX = Mathf.Lerp(0.62f, 1f, eased);
                    break;

                case "Tilt":
                    angle = Mathf.Lerp(-5f, 0f, eased);
                    scaleX = scaleY = Mathf.Lerp(0.96f, 1f, eased);
                    break;

                default:
                    scaleX = scaleY = Mathf.Lerp(0.94f, 1f, eased);
                    break;
            }

            // Around the window's own centre, so nothing drifts across the screen.
            matrix = Matrix4x4.TRS(pivot + offset, Quaternion.Euler(0f, 0f, angle),
                                   new Vector3(scaleX, scaleY, 1f))
                     * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);

            return true;
        }
    }
}
