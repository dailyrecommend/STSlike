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
        [Tooltip("Framing per card type, so an attack and a skill read differently.")]
        [SerializeField] private Image typeFrame;
        [SerializeField] private ArtEntry<CardType>[] typeStyles;

        [Tooltip("Framing per rarity — gem, border colour, and so on.")]
        [SerializeField] private Image rarityFrame;
        [SerializeField] private ArtEntry<CardRarity>[] rarityStyles;

        [Header("Unaffordable")]
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField, Range(0f, 1f)] private float unaffordableAlpha = 0.45f;

        private Action _onClick;
        private CardHoverChannel _hover;
        private RectTransform _rect;

        public CardView View { get; private set; }

        /// <summary>
        /// A visual set to the card itself counts as no visual at all. The
        /// layout drives the root to its place in the row and the visual to its
        /// hover offset; pointing both at one transform makes them fight, and
        /// since an unhovered card's offset is zero, the row loses — every card
        /// gets dragged back to the origin.
        /// </summary>
        private bool HasVisualChild => visual != null && visual != _rect;

        /// <summary>
        /// What the hand layout animates on hover. Falls back to the card root
        /// when no separate visual child is assigned.
        /// </summary>
        public RectTransform Visual => HasVisualChild ? visual : _rect;

        /// <summary>True when hover motion can move the pointer off the card.</summary>
        public bool MovesHitArea => !HasVisualChild;

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

            UiSlot.SetText(nameText, view.Name);
            UiSlot.SetText(costText, view.Cost.ToString());

            // Cleared as well as set: these instances are pooled, so skipping a
            // card that has no art of its own would leave whichever card sat in
            // this slot before still showing.
            UiSlot.SetSpriteOrClear(artwork, view.Artwork);

            ArtTable.Apply(typeStyles, view.Type, typeFrame);
            ArtTable.Apply(rarityStyles, view.Rarity, rarityFrame);

            UiSlot.SetInteractable(button, view.IsPlayable);
            UiSlot.SetAlpha(fadeGroup, view.IsPlayable ? 1f : unaffordableAlpha);
        }

        private void HandleClick() => _onClick?.Invoke();
    }
}
