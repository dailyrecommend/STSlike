using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace STSlike.Presentation
{
    /// <summary>
    /// Writes to an optional inspector slot.
    ///
    /// Every binder in this project follows the same rule: drag in the pieces
    /// you have built, leave the rest empty, and whatever is missing simply is
    /// not driven. That rule is a null check on each and every write, which was
    /// being spelled out again in each binder — four copies of the same text
    /// setter, four of the same activation guard.
    ///
    /// A few of these carry a decision rather than just a null check, and having
    /// that decision in one place is the point:
    /// <see cref="SetSprite"/> keeps whatever the prefab shipped with when no
    /// art was authored, and <see cref="SetActive"/> never touches a GameObject
    /// that is already in the state asked for.
    /// </summary>
    internal static class UiSlot
    {
        public static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value;
        }

        /// <summary>
        /// Replaces the art only when some was supplied. Blanking the image for
        /// a card or a node that has none yet would throw away the prefab's
        /// placeholder, which reads as broken rather than as unfinished.
        /// </summary>
        public static void SetSprite(Image target, Sprite sprite)
        {
            if (target != null && sprite != null)
                target.sprite = sprite;
        }

        /// <summary>
        /// Replaces the art even with nothing, for slots whose instances are
        /// pooled: leaving the image alone there does not show a placeholder, it
        /// shows whatever the last occupant put in it.
        /// </summary>
        public static void SetSpriteOrClear(Image target, Sprite sprite)
        {
            if (target != null)
                target.sprite = sprite;
        }

        public static void SetColor(Graphic target, Color color)
        {
            if (target != null)
                target.color = color;
        }

        public static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        public static void SetInteractable(Selectable target, bool interactable)
        {
            if (target != null)
                target.interactable = interactable;
        }

        public static void SetAlpha(CanvasGroup target, float alpha)
        {
            if (target != null)
                target.alpha = alpha;
        }

        // ---- bars ----

        public static float Fraction(int value, int max) =>
            max <= 0 ? 0f : (float)value / max;

        /// <summary>
        /// Eases a fill toward its target. A negative current means "no value
        /// yet", which lands on the target directly rather than sweeping up from
        /// empty on the first frame.
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
        public static void SetFill(Image target, float fraction)
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
