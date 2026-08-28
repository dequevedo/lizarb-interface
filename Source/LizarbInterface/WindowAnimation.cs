using UnityEngine;

namespace LizarbInterface
{
    internal static class WindowAnimation
    {
        internal static readonly string[] Styles =
        {
            "Scale", "Zoom", "Rise", "Slide", "Unfold", "Flip", "Tilt",
        };

        private static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        internal static bool Transform(string style, float progress, Vector2 pivot,
                                       out Matrix4x4 matrix)
        {
            float eased = EaseOutCubic(progress);
            matrix = Matrix4x4.identity;

            float scaleX = 1f;
            float scaleY = 1f;
            float angle = 0f;
            Vector2 offset = Vector2.zero;

            switch (style)
            {
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

            matrix = Matrix4x4.TRS(pivot + offset, Quaternion.Euler(0f, 0f, angle),
                                   new Vector3(scaleX, scaleY, 1f))
                     * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);

            return true;
        }
    }
}
