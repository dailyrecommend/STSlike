using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// A ScrollRect that only responds to the scroll wheel, and stops cleanly at
    /// both ends.
    ///
    /// Two problems with the stock component here:
    ///
    /// 1. The contents are clickable cards, and a normal ScrollRect claims the
    ///    drag as soon as the pointer moves a few pixels — so a slightly shaky
    ///    click scrolls the list instead of selecting the card.
    ///
    /// 2. A wheel notch always moves a fixed distance. When less than that is
    ///    left at the end of the list, the step overshoots into the elastic
    ///    zone and springs back, which reads as the list lurching. The step is
    ///    clamped to what actually remains instead.
    /// </summary>
    [AddComponentMenu("UI/Wheel Only Scroll Rect")]
    public class WheelOnlyScrollRect : ScrollRect
    {
        [Tooltip("Seconds for the list to glide to the new position. 0 = jump instantly.")]
        [SerializeField] private float smoothTime = 0.12f;

        [Tooltip("Add an invisible graphic covering the scroll area so the wheel " +
                 "also works in the gaps between items. Uncheck only if you have " +
                 "already put a background image there yourself.")]
        [SerializeField] private bool catchPointerOverGaps = true;

        private float _target;
        private float _smoothVelocity;
        private bool _hasTarget;

        // ---- drag is disabled ----

        public override void OnBeginDrag(PointerEventData eventData) { }

        public override void OnDrag(PointerEventData eventData) { }

        public override void OnEndDrag(PointerEventData eventData) { }

        /// <summary>
        /// Also refuse to be the drag target at all, so a drag started over the
        /// list is ignored rather than swallowed here.
        /// </summary>
        public override void OnInitializePotentialDrag(PointerEventData eventData) { }

        // ---- wheel ----

        protected override void Awake()
        {
            base.Awake();

            if (catchPointerOverGaps)
                EnsurePointerCatcher();
        }

        /// <summary>
        /// Scroll events go to whatever the pointer is over. Between two items
        /// there is usually nothing at all, the raycast misses, and the wheel
        /// does nothing — which feels broken. A fully transparent graphic across
        /// the scroll area gives those gaps something to hit. Alpha does not
        /// affect raycasting, so it stays invisible while still catching input.
        /// </summary>
        private void EnsurePointerCatcher()
        {
            RectTransform view = viewport != null ? viewport : (RectTransform)transform;

            if (view.GetComponent<Graphic>() != null)
                return; // a background is already there

            var catcher = view.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            catcher.raycastTarget = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SyncTargetToCurrent();
        }

        public override void OnScroll(PointerEventData eventData)
        {
            if (!IsActive())
                return;

            float scrollable = ScrollableSize();
            if (scrollable <= 0f)
                return; // everything fits; nothing to scroll

            float delta = eventData.scrollDelta.y;
            if (Mathf.Approximately(delta, 0f))
                return;

            if (!_hasTarget)
                SyncTargetToCurrent();

            // Clamped to 0..1, so a notch near the end travels only the distance
            // that is actually left instead of bouncing off the boundary.
            _target = Mathf.Clamp01(_target + delta * scrollSensitivity / scrollable);

            // Any leftover inertia would fight the glide below.
            velocity = Vector2.zero;

            if (smoothTime <= 0f)
                SetNormalizedPosition(_target);
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (!_hasTarget || smoothTime <= 0f)
                return;

            float current = GetNormalizedPosition();
            if (Mathf.Abs(current - _target) < 0.0005f)
            {
                SetNormalizedPosition(_target);
                return;
            }

            SetNormalizedPosition(
                Mathf.SmoothDamp(current, _target, ref _smoothVelocity, smoothTime, Mathf.Infinity,
                    Time.unscaledDeltaTime));
        }

        /// <summary>
        /// Jumps to the beginning of the list. Call this when a panel is opened:
        /// the content keeps whatever position it was left in, so reopening a
        /// list would otherwise start part-way down.
        /// </summary>
        public void ScrollToStart() => ScrollTo(vertical ? 1f : 0f);

        /// <summary>Jumps to the end of the list — the bottom of a vertical one.</summary>
        public void ScrollToEnd() => ScrollTo(vertical ? 0f : 1f);

        /// <summary>
        /// Jumps to a normalized position (vertical: 0 is the bottom, 1 the top)
        /// and clears the glide, so the next wheel notch starts from here.
        /// </summary>
        public void ScrollTo(float normalized)
        {
            velocity = Vector2.zero;
            SetNormalizedPosition(Mathf.Clamp01(normalized));
            SyncTargetToCurrent();
        }

        /// <summary>How much of the content is actually off-screen, in pixels.</summary>
        private float ScrollableSize()
        {
            if (content == null)
                return 0f;

            RectTransform view = viewport != null ? viewport : (RectTransform)transform;

            return vertical
                ? content.rect.height - view.rect.height
                : content.rect.width - view.rect.width;
        }

        private float GetNormalizedPosition() =>
            vertical ? verticalNormalizedPosition : horizontalNormalizedPosition;

        private void SetNormalizedPosition(float value)
        {
            if (vertical)
                verticalNormalizedPosition = value;
            else
                horizontalNormalizedPosition = value;
        }

        /// <summary>
        /// Re-reads where the list actually is. Needed whenever the panel is
        /// reopened with different contents, or the stored target would make the
        /// first wheel notch jump.
        /// </summary>
        private void SyncTargetToCurrent()
        {
            _target = Mathf.Clamp01(GetNormalizedPosition());
            _smoothVelocity = 0f;
            _hasTarget = true;
        }
    }
}
