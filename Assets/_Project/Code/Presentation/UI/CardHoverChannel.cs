using System;
using UnityEngine;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Carries "the pointer is over this card" from whichever card is hovered to
    /// whoever shows the tooltip.
    ///
    /// Going through a shared channel rather than wiring each card to the
    /// tooltip means hand cards, reward options and pile popups all get tooltips
    /// for free, and the tooltip never needs to know where the card came from.
    /// </summary>
    public sealed class CardHoverChannel
    {
        public bool IsShowing { get; private set; }
        public CardView Card { get; private set; }

        /// <summary>The hovered card's transform, so the tooltip can sit next to it.</summary>
        public RectTransform Source { get; private set; }

        /// <summary>
        /// Latest pointer position in screen space. Updated as the pointer moves
        /// without raising <see cref="Changed"/>: the tooltip only needs to
        /// rebuild its text when the card changes, not every frame the mouse
        /// twitches.
        /// </summary>
        public Vector2 PointerPosition { get; private set; }

        public event Action Changed;

        public void Show(CardView card, RectTransform source, Vector2 pointerPosition)
        {
            Card = card;
            Source = source;
            PointerPosition = pointerPosition;
            IsShowing = true;
            Changed?.Invoke();
        }

        public void UpdatePointer(Vector2 pointerPosition) => PointerPosition = pointerPosition;

        /// <summary>
        /// Hides only if that card is still the one being shown. Moving the
        /// pointer between two adjacent cards fires the new card's enter before
        /// the old card's exit; without this check the tooltip would flicker off
        /// immediately after appearing.
        /// </summary>
        public void HideIf(RectTransform source)
        {
            if (!IsShowing || Source != source)
                return;
            Hide();
        }

        public void Hide()
        {
            IsShowing = false;
            Source = null;
            Changed?.Invoke();
        }
    }
}
