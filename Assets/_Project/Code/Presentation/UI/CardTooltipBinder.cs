using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Shows the full text of whatever card the pointer is over, following the
    /// cursor.
    ///
    /// This is what lets the card face itself stay minimal — cost, name and art
    /// only — while the rules text still has room to be readable. Build the
    /// panel however you like and drag its pieces in; every slot is optional.
    /// </summary>
    public sealed class CardTooltipBinder : MonoBehaviour
    {
        private enum FollowMode
        {
            Pointer,
            Card,
            Fixed,
        }

        [Header("Panel")]
        [Tooltip("The panel that is shown, hidden and moved. Leave empty to use " +
                 "this GameObject.")]
        [SerializeField] private GameObject root;

        [Header("Content")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text rarityText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Image artwork;

        [Header("Placement")]
        [SerializeField] private FollowMode follow = FollowMode.Pointer;

        [Tooltip("Offset from the cursor (or card), in canvas units.")]
        [SerializeField] private Vector2 offset = new Vector2(24f, 24f);

        [Tooltip("Keep the panel fully inside its parent instead of running off screen.")]
        [SerializeField] private bool keepOnScreen = true;

        private CardHoverChannel _hover;
        private RectTransform _panelRect;
        private RectTransform _parentRect;
        private Canvas _canvas;

        [Inject]
        public void Construct(CardHoverChannel hover)
        {
            _hover = hover;
            _hover.Changed += Refresh;
            Refresh();
        }

        private void Awake()
        {
            GameObject panel = root != null ? root : gameObject;

            // The panel is what moves — not necessarily this component's own
            // object, since the binder may sit on a parent.
            _panelRect = panel.transform as RectTransform;
            _parentRect = _panelRect != null ? _panelRect.parent as RectTransform : null;
            _canvas = GetComponentInParent<Canvas>();

            // A tooltip that sits under the cursor would eat the card's own
            // pointer events and make the hover drop the moment it appears.
            //
            // Scoped to the panel, and only blocksRaycasts: a CanvasGroup applies
            // to everything underneath it, so touching `interactable` here would
            // grey out any other UI that happens to live below this object.
            if (!panel.TryGetComponent(out CanvasGroup group))
                group = panel.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            if (_panelRect != null)
            {
                // Placement below is expressed relative to a single anchor point.
                _panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                _panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            }
        }

        private void OnDestroy()
        {
            if (_hover != null)
                _hover.Changed -= Refresh;
        }

        // Position is updated every frame so the panel keeps up with the cursor;
        // the contents only change when Refresh runs.
        private void LateUpdate()
        {
            if (_hover != null && _hover.IsShowing)
                Reposition();
        }

        private void Refresh()
        {
            GameObject panel = root != null ? root : gameObject;

            if (_hover == null || !_hover.IsShowing)
            {
                if (panel.activeSelf)
                    panel.SetActive(false);
                return;
            }

            CardView card = _hover.Card;

            UiSlot.SetText(nameText, card.Name);
            UiSlot.SetText(costText, card.Cost.ToString());
            UiSlot.SetText(typeText, card.Type.ToString());
            UiSlot.SetText(rarityText, card.Rarity.ToString());
            UiSlot.SetText(descriptionText, card.Description);

            // Keep the prefab's placeholder art when a card has none of its own.
            // Unlike a card in hand, this panel is not pooled across cards — it
            // is rebuilt every time it appears, so nothing stale can linger.
            UiSlot.SetSprite(artwork, card.Artwork);

            if (!panel.activeSelf)
                panel.SetActive(true);

            Reposition();
        }

        private void Reposition()
        {
            if (_panelRect == null || follow == FollowMode.Fixed)
                return;

            if (follow == FollowMode.Card)
            {
                if (_hover.Source != null)
                    _panelRect.position = _hover.Source.position + (Vector3)offset;
                return;
            }

            if (_parentRect == null)
                return;

            // Screen space -> the parent's local space, so this works the same
            // in Overlay and Camera canvases.
            Camera camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parentRect, ReadPointerPosition(), camera, out Vector2 local))
                return;

            _panelRect.anchoredPosition = keepOnScreen ? Clamp(local + offset) : local + offset;
        }

        /// <summary>
        /// Polls the pointer rather than relying on pointer-move events, which
        /// are not delivered by every input module setup. The position carried
        /// by the hover channel is the fallback, so the tooltip still appears in
        /// the right place even with no pointer device at all.
        /// </summary>
        private Vector2 ReadPointerPosition() => GameInput.Position(_hover.PointerPosition);

        /// <summary>
        /// Keeps the panel inside its parent. Near the right or top edge the
        /// offset would otherwise push it off screen exactly when the player
        /// needs to read it.
        /// </summary>
        private Vector2 Clamp(Vector2 position)
        {
            Rect parent = _parentRect.rect;
            Rect self = _panelRect.rect;

            float halfWidth = self.width * 0.5f;
            float halfHeight = self.height * 0.5f;

            // anchoredPosition is measured from the parent's center here.
            float minX = parent.xMin + halfWidth;
            float maxX = parent.xMax - halfWidth;
            float minY = parent.yMin + halfHeight;
            float maxY = parent.yMax - halfHeight;

            return new Vector2(
                maxX >= minX ? Mathf.Clamp(position.x, minX, maxX) : position.x,
                maxY >= minY ? Mathf.Clamp(position.y, minY, maxY) : position.y);
        }

    }
}
