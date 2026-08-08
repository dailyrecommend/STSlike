using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// The popup used to browse a pile of cards: the draw pile, the discard
    /// pile, or the whole run deck.
    ///
    /// One component covers all three because they only differ by which list
    /// they read and what the title says. Cards are shown with the same prefab
    /// as the hand, so they get the hover tooltip for free.
    ///
    /// Wire the open methods straight to your buttons' OnClick — they take no
    /// arguments for exactly that reason.
    /// </summary>
    public sealed class CardListPopupBinder : MonoBehaviour
    {
        private enum Source
        {
            None,
            DrawPile,
            DiscardPile,
            RunDeck,
        }

        [Header("Panel")]
        [Tooltip("Shown and hidden as the popup opens and closes. Leave empty to use this GameObject.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button closeButton;

        [Header("Header")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text countText;

        [Tooltip("Titles for the draw pile, discard pile and run deck, in that order.")]
        [SerializeField] private string drawPileTitle = "Draw Pile";
        [SerializeField] private string discardPileTitle = "Discard Pile";
        [SerializeField] private string runDeckTitle = "Deck";

        [Header("Cards")]
        [Tooltip("Same prefab as the hand. Put a Grid Layout Group on the container.")]
        [SerializeField] private CardViewBinder cardPrefab;
        [SerializeField] private Transform cardContainer;

        [Tooltip("Optional. Assigned so the list always opens at the top.")]
        [SerializeField] private ScrollRect scrollList;

        private readonly List<CardViewBinder> _spawned = new List<CardViewBinder>();
        private CombatPresenter _presenter;
        private CardHoverChannel _hover;
        private PanelToggle _panel;
        private Source _source = Source.None;

        // Created on demand rather than in Awake: VContainer injects from the
        // LifetimeScope's own Awake, which can run before this component's.
        private PanelToggle Panel => _panel ??= new PanelToggle(root, gameObject);

        [Inject]
        public void Construct(CombatPresenter presenter, CardHoverChannel hover)
        {
            _presenter = presenter;
            _hover = hover;

            // Keeps an open popup honest while cards move between piles.
            _presenter.Changed += RefreshIfOpen;

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            Hide();
        }

        private void OnDestroy()
        {
            if (_presenter != null)
                _presenter.Changed -= RefreshIfOpen;
        }

        // ---- hook these to buttons ----

        /// <summary>Opens the draw pile, or closes it if it is already open.</summary>
        public void ToggleDrawPile() => Toggle(Source.DrawPile);

        public void ToggleDiscardPile() => Toggle(Source.DiscardPile);

        public void ToggleRunDeck() => Toggle(Source.RunDeck);

        public void ShowDrawPile() => Show(Source.DrawPile);

        public void ShowDiscardPile() => Show(Source.DiscardPile);

        public void ShowRunDeck() => Show(Source.RunDeck);

        public void Hide()
        {
            _source = Source.None;
            SetPanelActive(false);

            // Clear the tooltip: the card it was describing is about to vanish
            // without ever raising a pointer-exit.
            _hover?.Hide();
        }

        // ---- internals ----

        private void Toggle(Source source)
        {
            if (_source == source)
                Hide();
            else
                Show(source);
        }

        private void Show(Source source)
        {
            _source = source;
            SetPanelActive(true);
            Rebuild();
            ScrollToTop();
        }

        /// <summary>
        /// A list always opens at the top. The content keeps the position it was
        /// left in, and its height only settles once the new cards have been
        /// laid out — so without this, reopening a long list starts part-way
        /// down at an arbitrary point.
        /// </summary>
        private void ScrollToTop()
        {
            if (scrollList == null)
                return;

            // The cards were spawned this frame; force the layout so the content
            // height is final before the scroll position is computed from it.
            Canvas.ForceUpdateCanvases();

            if (scrollList is WheelOnlyScrollRect wheel)
                wheel.ScrollToStart();
            else
                scrollList.verticalNormalizedPosition = 1f;
        }

        private void RefreshIfOpen()
        {
            if (_source != Source.None)
                Rebuild();
        }

        private void Rebuild()
        {
            if (_presenter == null || cardPrefab == null)
                return;

            IReadOnlyList<CardView> cards = ResolveCards();
            Transform parent = cardContainer != null ? cardContainer : transform;

            UiSlot.SetText(titleText, ResolveTitle());
            UiSlot.SetText(countText, cards.Count.ToString());

            while (_spawned.Count < cards.Count)
            {
                CardViewBinder instance = Instantiate(cardPrefab, parent);
                instance.SetHoverChannel(_hover);
                _spawned.Add(instance);
            }

            for (int i = 0; i < _spawned.Count; i++)
            {
                CardViewBinder binder = _spawned[i];
                bool inUse = i < cards.Count;
                binder.gameObject.SetActive(inUse);

                if (inUse)
                {
                    // Browsing only: no click action, and the view's HandIndex is
                    // -1 so it could not be played even if one were attached.
                    binder.Bind(cards[i], null);
                }
            }
        }

        private IReadOnlyList<CardView> ResolveCards()
        {
            switch (_source)
            {
                case Source.DrawPile: return _presenter.DrawPileContents;
                case Source.DiscardPile: return _presenter.DiscardPileContents;
                case Source.RunDeck: return _presenter.RunDeckContents;
                default: return System.Array.Empty<CardView>();
            }
        }

        private string ResolveTitle()
        {
            switch (_source)
            {
                case Source.DrawPile: return drawPileTitle;
                case Source.DiscardPile: return discardPileTitle;
                case Source.RunDeck: return runDeckTitle;
                default: return string.Empty;
            }
        }

        private void SetPanelActive(bool active) => Panel.Set(active);

    }
}
