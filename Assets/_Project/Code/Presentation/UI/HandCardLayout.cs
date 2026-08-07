using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Lays out the cards in hand, squeezing them together once there are more
    /// than the container can show side by side, and lifting whichever card the
    /// pointer is over.
    ///
    /// A HorizontalLayoutGroup keeps a fixed gap, so a large hand simply spills
    /// out of the container or gets clipped. Here the gap shrinks with the card
    /// count instead: the hand stays centered and always fits, the way physical
    /// cards overlap in a real hand.
    ///
    /// Put this on the card container (the same object the HandViewBinder
    /// spawns into) and remove any LayoutGroup from it.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class HandCardLayout : MonoBehaviour
    {
        [Header("Spacing")]
        [Tooltip("Gap between card centers when there is plenty of room.")]
        [SerializeField] private float maxSpacing = 180f;

        [Tooltip("Width of one card. Keeps the outermost cards inside the container.")]
        [SerializeField] private float cardWidth = 160f;

        [Tooltip("Cards never get closer than this, even with a huge hand.")]
        [SerializeField] private float minSpacing = 40f;

        [Header("Fan (optional)")]
        [Tooltip("How high the middle of the hand arcs. 0 = flat row.")]
        [SerializeField] private float arcHeight = 0f;

        [Tooltip("Tilt of the outermost cards, in degrees. 0 = no rotation.")]
        [SerializeField] private float maxTilt = 0f;

        [Header("Hover")]
        [Tooltip("Height the hovered card rises TO, measured from the container " +
                 "center. Every card reaches the same height, so a fanned hand " +
                 "does not raise edge cards less than middle ones.")]
        [SerializeField] private float hoverHeight = 90f;

        [Tooltip("How much the hovered card grows. 1 = no growth.")]
        [SerializeField] private float hoverScale = 1.15f;

        [Tooltip("Straighten the hovered card so it is easy to read.")]
        [SerializeField] private bool straightenOnHover = true;

        [Header("Aiming")]
        [Tooltip("Where a card waits while it is looking for a target, measured " +
                 "from the container center. Put it clear of the row: the point " +
                 "is that the card has visibly left the hand.")]
        [SerializeField] private Vector2 aimPosition = new Vector2(0f, 260f);

        [Tooltip("How much the aimed card grows.")]
        [SerializeField] private float aimScale = 1.25f;

        [Header("Motion")]
        [Tooltip("How quickly cards slide to their place. 0 = snap instantly.")]
        [SerializeField] private float smoothing = 14f;

        private RectTransform _rect;
        private CardHoverChannel _hover;
        private CardSelection _selection;
        private readonly List<RectTransform> _ordered = new List<RectTransform>();
        private readonly List<RectTransform> _row = new List<RectTransform>();

        [Inject]
        public void Construct(CardHoverChannel hover, CardSelection selection)
        {
            _hover = hover;
            _selection = selection;
        }

        private void Awake()
        {
            _rect = (RectTransform)transform;

            // A layout group would fight this script for the same positions.
            if (TryGetComponent(out LayoutGroup group))
            {
                Debug.LogWarning(
                    $"[HandCardLayout] Disabling {group.GetType().Name} on '{name}': " +
                    "it would override the hand layout.", this);
                group.enabled = false;
            }

            // A size fitter is worse than a rival: the spacing is worked out
            // from this rect's width, so a fitter that resizes the rect from its
            // children closes a loop — cards move, the container resizes, the
            // spacing changes, the cards move again. That reads as a permanent
            // shiver rather than as a layout being wrong.
            if (TryGetComponent(out ContentSizeFitter fitter))
            {
                Debug.LogWarning(
                    $"[HandCardLayout] Disabling ContentSizeFitter on '{name}': " +
                    "sizing this rect from the cards it positions makes them jitter.", this);
                fitter.enabled = false;
            }
        }

        // LateUpdate so it runs after the hand has been rebuilt this frame.
        private void LateUpdate() => Arrange();

        private void Arrange()
        {
            CollectCards();
            if (_ordered.Count == 0)
                return;

            RectTransform hovered = HoveredCard();
            RectTransform aimed = AimedCard();

            // A card that has been picked up is no longer part of the row, so
            // the hand closes over the gap it left instead of holding its place
            // open. Laid out from this shorter list, the rest re-space at once.
            _row.Clear();
            foreach (RectTransform card in _ordered)
            {
                if (card != aimed)
                    _row.Add(card);
            }

            int count = _row.Count;
            float spacing = ResolveSpacing(count);
            float startX = -spacing * (count - 1) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                RectTransform card = _row[i];
                bool isHovered = card == hovered;

                // -1 at the left edge, 0 in the middle, +1 at the right edge.
                float t = count > 1 ? i / (float)(count - 1) : 0.5f;
                float centered = (t - 0.5f) * 2f;

                float restY = arcHeight * (1f - centered * centered);   // peak in the middle
                var slot = new Vector2(startX + spacing * i, restY);
                float tilt = -maxTilt * centered;

                // Raising to a shared height rather than by a fixed amount, so
                // cards low in the arc do not end up lower when hovered.
                float lift = isHovered ? hoverHeight - restY : 0f;
                float untilt = isHovered && straightenOnHover ? -tilt : 0f;
                float scale = isHovered ? hoverScale : 1f;

                if (HasSeparateVisual(card))
                {
                    // The slot never reacts to hover. Because the root carries
                    // the raycast target, keeping it still is what stops a
                    // raised card from sliding out from under the pointer and
                    // oscillating between two overlapping cards.
                    Apply(card, slot, Quaternion.Euler(0f, 0f, tilt), 1f);
                    ApplyHover(card, lift, untilt, scale);
                }
                else
                {
                    // No visual child assigned: move the whole card. Simpler to
                    // set up, but the hit area moves with it and hovering
                    // between two overlapping cards can flicker.
                    Apply(card, slot + new Vector2(0f, lift),
                        Quaternion.Euler(0f, 0f, tilt + untilt), scale);
                }

                // Draw order is rebuilt every frame from the layout order, then
                // the hovered card is pushed to the front. Doing it this way —
                // rather than reordering to raise a card — keeps the sibling
                // index out of the position maths.
                card.SetSiblingIndex(i);
            }

            if (hovered != null)
                hovered.SetAsLastSibling();

            PlaceAimed(aimed, aimed != null && aimed == hovered);
        }

        /// <summary>
        /// Parks the picked-up card in the middle. It ignores the row, the fan
        /// and the hover lift — it has left the hand — but still answers to the
        /// pointer with a little more size, so it stays clear that clicking it
        /// again does something. The whole card moves rather than just its
        /// visual child, so clicking it means clicking where it appears to be.
        /// </summary>
        private void PlaceAimed(RectTransform aimed, bool hovered)
        {
            if (aimed == null)
                return;

            Apply(aimed, aimPosition, Quaternion.identity,
                hovered ? aimScale * hoverScale : aimScale);

            if (HasSeparateVisual(aimed))
                ApplyHover(aimed, 0f, 0f, 1f);

            // Above even the hovered card: it is the one being acted on.
            aimed.SetAsLastSibling();
        }

        private RectTransform AimedCard()
        {
            if (_selection == null || !_selection.IsAiming)
                return null;

            foreach (RectTransform card in _ordered)
            {
                if (card.TryGetComponent(out CardViewBinder binder)
                    && binder.View.HandIndex == _selection.HandIndex)
                    return card;
            }
            return null;
        }

        /// <summary>
        /// Gathers the active cards in hand order. The card's own HandIndex is
        /// used rather than the sibling index so that reordering for draw order
        /// cannot disturb the layout.
        /// </summary>
        private void CollectCards()
        {
            _ordered.Clear();

            for (int i = 0; i < _rect.childCount; i++)
            {
                var child = _rect.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeSelf)
                    continue;
                _ordered.Add(child);
            }

            _ordered.Sort(CompareByHandIndex);
        }

        private static int CompareByHandIndex(RectTransform a, RectTransform b)
        {
            int indexA = a.TryGetComponent(out CardViewBinder binderA) ? binderA.View.HandIndex : int.MaxValue;
            int indexB = b.TryGetComponent(out CardViewBinder binderB) ? binderB.View.HandIndex : int.MaxValue;
            return indexA.CompareTo(indexB);
        }

        private RectTransform HoveredCard()
        {
            if (_hover == null || !_hover.IsShowing)
                return null;

            RectTransform source = _hover.Source;
            return source != null && source.parent == _rect ? source : null;
        }

        private float ResolveSpacing(int count)
        {
            if (count <= 1)
                return maxSpacing;

            // The outermost card centers sit half a card in from each edge, so
            // this is the width actually available for the gaps between them.
            float usable = _rect.rect.width - cardWidth;
            float fitted = usable / (count - 1);

            return Mathf.Clamp(fitted, minSpacing, maxSpacing);
        }

        private static bool HasSeparateVisual(RectTransform card)
        {
            return card.TryGetComponent(out CardViewBinder binder) && !binder.MovesHitArea;
        }

        /// <summary>
        /// Hover motion lives on the card's visual child, so the root — and with
        /// it the clickable area — stays exactly where the layout put it.
        /// </summary>
        private void ApplyHover(RectTransform card, float lift, float untilt, float scale)
        {
            if (!card.TryGetComponent(out CardViewBinder binder))
                return;

            RectTransform visual = binder.Visual;

            var targetPosition = new Vector2(0f, lift);
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, untilt);
            var targetScale = new Vector3(scale, scale, 1f);

            if (smoothing <= 0f)
            {
                visual.anchoredPosition = targetPosition;
                visual.localRotation = targetRotation;
                visual.localScale = targetScale;
                return;
            }

            float step = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            visual.anchoredPosition = Vector2.Lerp(visual.anchoredPosition, targetPosition, step);
            visual.localRotation = Quaternion.Slerp(visual.localRotation, targetRotation, step);
            visual.localScale = Vector3.Lerp(visual.localScale, targetScale, step);
        }

        private void Apply(RectTransform card, Vector2 position, Quaternion rotation, float scale)
        {
            // Positions are measured from the container's center, so the cards
            // are anchored there regardless of how the prefab was authored.
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);

            var targetScale = new Vector3(scale, scale, 1f);

            if (smoothing <= 0f)
            {
                card.anchoredPosition = position;
                card.localRotation = rotation;
                card.localScale = targetScale;
                return;
            }

            float step = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            card.anchoredPosition = Vector2.Lerp(card.anchoredPosition, position, step);
            card.localRotation = Quaternion.Slerp(card.localRotation, rotation, step);
            card.localScale = Vector3.Lerp(card.localScale, targetScale, step);
        }
    }
}
