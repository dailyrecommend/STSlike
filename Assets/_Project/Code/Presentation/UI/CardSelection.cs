using System;
using UnityEngine;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Which card is being aimed, and at nothing yet.
    ///
    /// Playing a card is two decisions — which card, and at whom — and between
    /// them the game is in a state the rules know nothing about: a card has been
    /// picked up but not committed, and can still be put back. That state is
    /// pure interaction, so it lives here rather than in the domain.
    ///
    /// Both halves of the screen read it: the hand raises the aimed card out of
    /// the row, and the figures on stage offer themselves as targets. Neither
    /// knows about the other.
    /// </summary>
    public sealed class CardSelection
    {
        /// <summary>Hand slot being aimed, or -1 when nothing is.</summary>
        public int HandIndex { get; private set; } = -1;

        public bool IsAiming => HandIndex >= 0;

        /// <summary>
        /// Whether the card in hand needs somebody picked before it can be
        /// thrown. A card that acts on the player has nothing to aim at, so no
        /// figure lights up and a click anywhere off the UI is the confirmation.
        /// </summary>
        public bool NeedsTarget { get; private set; }

        /// <summary>
        /// True during the frame the card was picked up.
        ///
        /// The button press that picks a card up is still "pressed this frame"
        /// when the rest of the frame runs, so without this the same click both
        /// selects a card and commits it — the card never reaches the middle.
        /// Whether it happens at all depends on script execution order, which is
        /// exactly the kind of thing that works on one machine and not the next.
        /// </summary>
        public bool JustSelected => Time.frameCount == _selectedFrame;

        private int _selectedFrame = -1;

        /// <summary>
        /// The figure under the pointer while aiming — where the card would land
        /// if it were thrown right now. Not a commitment; it changes as the
        /// pointer moves and is only there so the target can light up.
        /// </summary>
        public ActorRef Hovered { get; private set; } = ActorRef.None;

        public event Action Changed;

        /// <summary>Picks a card up, ready to be committed.</summary>
        public void Select(int handIndex, bool needsTarget)
        {
            if (HandIndex == handIndex)
                return;

            HandIndex = handIndex;
            NeedsTarget = needsTarget;
            _selectedFrame = Time.frameCount;
            Changed?.Invoke();
        }

        /// <summary>Points the aim at a figure, or at nobody.</summary>
        public void Hover(ActorRef actor)
        {
            if (Same(Hovered, actor))
                return;

            Hovered = actor;
            Changed?.Invoke();
        }

        /// <summary>Puts the card back. Safe to call when nothing is aimed.</summary>
        public void Clear()
        {
            if (HandIndex < 0 && !Hovered.IsValid)
                return;

            HandIndex = -1;
            NeedsTarget = false;
            Hovered = ActorRef.None;
            Changed?.Invoke();
        }

        private static bool Same(ActorRef a, ActorRef b) =>
            a.IsPlayer == b.IsPlayer && a.EnemyIndex == b.EnemyIndex;
    }
}
