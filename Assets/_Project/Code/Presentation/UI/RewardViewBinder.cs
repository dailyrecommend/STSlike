using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

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

        [Inject]
        public void Construct(CombatPresenter presenter)
        {
            _presenter = presenter;
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

            if (headlineText != null)
                headlineText.text = rewards.Count > 0 ? "Choose a reward" : string.Empty;

            while (_spawned.Count < rewards.Count)
                _spawned.Add(Instantiate(optionPrefab, parent));

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
                var asCard = new CardView(optionIndex, label, reward.CardCost, true);

                binder.Bind(asCard, null, () => _presenter.ChooseReward(optionIndex));
            }
        }
    }
}
