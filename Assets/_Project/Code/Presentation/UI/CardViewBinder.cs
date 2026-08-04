using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using STSlike.Domain.Cards;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Put this on your card prefab and drag its pieces into the slots. It only
    /// fills in data and reports clicks — the look, size and animation are
    /// entirely yours. Every slot is optional.
    /// </summary>
    public sealed class CardViewBinder : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        /// <summary>Per-type framing, so an attack and a skill can read differently.</summary>
        [Serializable]
        private struct TypeStyle
        {
            public CardType type;
            public Sprite frame;
            public Color tint;
        }

        /// <summary>Per-rarity framing (gem, border colour, ...).</summary>
        [Serializable]
        private struct RarityStyle
        {
            public CardRarity rarity;
            public Sprite frame;
            public Color tint;
        }

        // The card face stays minimal on purpose: the full rules text lives in
        // the hover tooltip, which is what makes a small card shape workable.
        [Tooltip("Child holding everything you can see. The hand layout raises " +
                 "THIS on hover while the root stays put, so lifting a card can " +
                 "never move it out from under the pointer. Leave empty to move " +
                 "the whole card (simpler, but hovering between two overlapping " +
                 "cards will flicker).")]
        [SerializeField] private RectTransform visual;

        [Header("Content")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Image artwork;
        [SerializeField] private Button button;

        [Header("Type / Rarity (optional styling)")]
        [SerializeField] private Image typeFrame;
        [SerializeField] private TypeStyle[] typeStyles;
        [SerializeField] private Image rarityFrame;
        [SerializeField] private RarityStyle[] rarityStyles;

        [Header("Unaffordable")]
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField, Range(0f, 1f)] private float unaffordableAlpha = 0.45f;

        private Action _onClick;
        private CardHoverChannel _hover;
        private RectTransform _rect;

        public CardView View { get; private set; }

        /// <summary>
        /// What the hand layout animates on hover. Falls back to the card root
        /// when no separate visual child is assigned.
        /// </summary>
        public RectTransform Visual => visual != null ? visual : _rect;

        /// <summary>True when hover motion can move the pointer off the card.</summary>
        public bool MovesHitArea => visual == null;

        private void Awake()
        {
            _rect = transform as RectTransform;

            if (button == null)
                button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        /// <summary>Called once by whichever binder spawned this card.</summary>
        public void SetHoverChannel(CardHoverChannel channel) => _hover = channel;

        public void OnPointerEnter(PointerEventData eventData) => _hover?.Show(View, _rect, eventData.position);

        public void OnPointerExit(PointerEventData eventData) => _hover?.HideIf(_rect);

        // Taking the position from the event rather than polling an input API
        // keeps this working under either input backend, and it is already in
        // the right screen-space coordinates.
        public void OnPointerMove(PointerEventData eventData)
        {
            if (_hover != null && _hover.Source == _rect)
                _hover.UpdatePointer(eventData.position);
        }

        private void OnDisable()
        {
            // A card played from hand is deactivated while the pointer is still
            // over it, so no exit event ever arrives — clear it here instead.
            _hover?.HideIf(_rect);
        }

        /// <summary>Called by the hand/reward binders whenever the contents change.</summary>
        public void Bind(CardView view, Action onClick)
        {
            View = view;
            _onClick = onClick;

            SetText(nameText, view.Name);
            SetText(costText, view.Cost.ToString());

            // Keep the prefab's placeholder art when a card has none of its own.
            if (artwork != null && view.Artwork != null)
                artwork.sprite = view.Artwork;

            ApplyTypeStyle(view.Type);
            ApplyRarityStyle(view.Rarity);

            if (button != null)
                button.interactable = view.IsPlayable;
            if (fadeGroup != null)
                fadeGroup.alpha = view.IsPlayable ? 1f : unaffordableAlpha;
        }

        private void ApplyTypeStyle(CardType type)
        {
            if (typeFrame == null || typeStyles == null)
                return;

            foreach (TypeStyle style in typeStyles)
            {
                if (style.type != type)
                    continue;

                if (style.frame != null)
                    typeFrame.sprite = style.frame;
                if (style.tint.a > 0f)
                    typeFrame.color = style.tint;
                return;
            }
        }

        private void ApplyRarityStyle(CardRarity rarity)
        {
            if (rarityFrame == null || rarityStyles == null)
                return;

            foreach (RarityStyle style in rarityStyles)
            {
                if (style.rarity != rarity)
                    continue;

                if (style.frame != null)
                    rarityFrame.sprite = style.frame;
                if (style.tint.a > 0f)
                    rarityFrame.color = style.tint;
                return;
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value;
        }

        private void HandleClick() => _onClick?.Invoke();
    }
}
