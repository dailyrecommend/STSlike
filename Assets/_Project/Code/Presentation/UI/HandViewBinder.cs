using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Keeps a row of card prefabs in sync with the hand, and turns a click on
    /// one into either a play or an aim.
    ///
    /// Instances are reused rather than destroyed and respawned: cards change
    /// several times per turn, and recreating them would fight any animation
    /// you put on the prefab (and churn the GC). This is also the seam where
    /// object pooling formally lands later.
    /// </summary>
    public sealed class HandViewBinder : MonoBehaviour
    {
        [SerializeField] private CardViewBinder cardPrefab;
        [SerializeField] private Transform cardContainer;

        private readonly List<CardViewBinder> _spawned = new List<CardViewBinder>();
        private CombatPresenter _presenter;
        private CardHoverChannel _hover;
        private CardSelection _selection;

        [Inject]
        public void Construct(CombatPresenter presenter, CardHoverChannel hover,
            CardSelection selection)
        {
            _presenter = presenter;
            _hover = hover;
            _selection = selection;

            _presenter.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_presenter != null)
                _presenter.Changed -= Refresh;
        }

        /// <summary>
        /// A click picks the card up rather than playing it.
        ///
        /// What a second click on the same card means depends on what the card
        /// is still waiting for. One that needs a target is put back, because
        /// the target is chosen somewhere else entirely; one that acts on the
        /// player has nothing left to wait for, so clicking it again is the
        /// confirmation. Right-click and Escape cancel either.
        /// </summary>
        private void OnCardClicked(CardView card)
        {
            if (_selection.HandIndex != card.HandIndex)
            {
                _selection.Select(card.HandIndex, card.NeedsTarget);
                return;
            }

            _selection.Clear();

            if (!card.NeedsTarget)
                _presenter.PlayCard(card.HandIndex);
        }

        private void Update()
        {
            // Not on the click that picked the card up.
            if (_selection == null || !_selection.IsAiming || _selection.JustSelected)
                return;

            // Right-click and Escape both put an aimed card back.
            if (GameInput.ClickedSecondary || GameInput.EscapePressed)
            {
                _selection.Clear();
                return;
            }

            // A card that acts on the player has nobody to be pointed at, so the
            // click that commits it is simply one away from the UI. Handled here
            // rather than with the target picking, which needs a stage; a card
            // aimed at yourself should not depend on there being models.
            if (!_selection.NeedsTarget && GameInput.ClickedPrimary && !GameInput.OverUi)
            {
                int handIndex = _selection.HandIndex;
                _selection.Clear();
                _presenter.PlayCard(handIndex);
            }
        }

        private void Refresh()
        {
            if (_presenter == null || cardPrefab == null)
                return;

            Transform parent = cardContainer != null ? cardContainer : transform;
            IReadOnlyList<CardView> hand = _presenter.Hand;

            // The hand has been redealt, the fight is over, or the screen has
            // fallen behind — whatever was being aimed is no longer that card,
            // or the player is no longer allowed to throw it.
            if (_selection.HandIndex >= hand.Count || !_presenter.CanAct)
                _selection.Clear();

            // Grow the pool to fit the current hand.
            while (_spawned.Count < hand.Count)
            {
                CardViewBinder instance = Instantiate(cardPrefab, parent);
                instance.SetHoverChannel(_hover);
                _spawned.Add(instance);
            }

            for (int i = 0; i < _spawned.Count; i++)
            {
                CardViewBinder binder = _spawned[i];
                bool inUse = i < hand.Count;
                binder.gameObject.SetActive(inUse);

                if (!inUse)
                    continue;

                CardView view = hand[i];
                binder.Bind(view, () => OnCardClicked(view));
            }
        }
    }
}
