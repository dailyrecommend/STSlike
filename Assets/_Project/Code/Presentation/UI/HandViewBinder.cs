using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Keeps a row of card prefabs in sync with the hand.
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

        [Inject]
        public void Construct(CombatPresenter presenter, CardHoverChannel hover)
        {
            _presenter = presenter;
            _hover = hover;
            _presenter.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_presenter != null)
                _presenter.Changed -= Refresh;
        }

        private void Refresh()
        {
            if (_presenter == null || cardPrefab == null)
                return;

            Transform parent = cardContainer != null ? cardContainer : transform;
            IReadOnlyList<CardView> hand = _presenter.Hand;

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
                int handIndex = view.HandIndex;
                binder.Bind(view, () => _presenter.PlayCard(handIndex));
            }
        }
    }
}
