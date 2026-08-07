using UnityEngine;
using UnityEngine.UI;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Shared behaviour for health bars, wherever they are drawn.
    ///
    /// The bar in the corner and the bar over a figure's head must drain at the
    /// same rate — two bars easing differently for the same blow looks like one
    /// of them is broken — so the maths lives in one place rather than being
    /// copied into each binder.
    /// </summary>
    internal static class BarFill
    {
        public static float Fraction(int value, int max) =>
            max <= 0 ? 0f : (float)value / max;

        /// <summary>
        /// Eases a fill toward its target. A negative current means "no value
        /// yet", which lands on the target directly rather than sweeping up
        /// from empty on the first frame.
        /// </summary>
        public static float Approach(float current, float target, float smoothing)
        {
            if (current < 0f || smoothing <= 0f)
                return target;

            // Refilling (a new fight, a heal) should not crawl upward.
            if (target > current)
                return target;

            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));
        }

        /// <summary>Works with a filled Image, or with a plain sprite via its anchor.</summary>
        public static void Set(Image target, float fraction)
        {
            if (target == null)
                return;

            fraction = Mathf.Clamp01(fraction);
            if (target.type == Image.Type.Filled)
            {
                target.fillAmount = fraction;
            }
            else
            {
                RectTransform rt = target.rectTransform;
                rt.anchorMax = new Vector2(fraction, rt.anchorMax.y);
            }
        }
    }
}
