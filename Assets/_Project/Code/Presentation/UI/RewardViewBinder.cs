using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using STSlike.Domain.Cards;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Fills the reward screen with the offered choices. Reuses the same
    /// reuse-not-respawn approach as the hand.
    /// </summary>
    public sealed class RewardViewBinder : MonoBehaviour
    {
        [SerializeField] private CardViewBinder optionPrefab;
        [SerializeField] private Transform optionContainer;
        [SerializeField] private TMP_Text headlineText;
        [SerializeField] private Button skipButton;

        private readonly List<CardViewBinder> _spawned = new List<CardViewBinder>();
        private CombatPresenter _presenter;
        private CardHoverChannel _hover;

        [Inject]
        public void Construct(CombatPresenter presenter, CardHoverChannel hover)
        {
            _presenter = presenter;
            _hover = hover;
            _presenter.Changed += Refresh;

            if (skipButton != null)
                skipButton.onClick.AddListener(() => _presenter.SkipReward());

            Refresh();
        }

        private void OnDestroy()
        {
            if (_presenter != null)
                _presenter.Changed -= Refresh;
        }

        private void Refresh()
        {
            if (_presenter == null || optionPrefab == null)
                return;

            IReadOnlyList<RewardView> rewards = _presenter.Rewards;
            Transform parent = optionContainer != null ? optionContainer : transform;

            UiSlot.SetText(headlineText, rewards.Count > 0 ? "Choose a reward" : string.Empty);

            while (_spawned.Count < rewards.Count)
            {
                CardViewBinder instance = Instantiate(optionPrefab, parent);
                instance.SetHoverChannel(_hover);
                _spawned.Add(instance);
            }

            for (int i = 0; i < _spawned.Count; i++)
            {
                CardViewBinder binder = _spawned[i];
                bool inUse = i < rewards.Count;
                binder.gameObject.SetActive(inUse);

                if (!inUse)
                    continue;

                RewardView reward = rewards[i];
                int optionIndex = reward.OptionIndex;

                // Reward options reuse the card prefab, so a heal is described
                // in the same slots rather than needing its own prefab.
                string label = reward.Kind == RewardKind.AddCard ? reward.CardId : $"Heal +{reward.Amount}";
                var asCard = new CardView(optionIndex, label, reward.CardCost, true,
                    label, string.Empty, null, CardType.Skill, CardRarity.Common);

                binder.Bind(asCard, () => _presenter.ChooseReward(optionIndex));
            }
        }
    }
}
