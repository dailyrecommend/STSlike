using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Pushes combat state into whatever you built in the scene.
    ///
    /// Nothing here creates or positions UI — you design the layout, then drag
    /// your objects into these slots. Every field is optional: leave a slot empty
    /// and that piece is simply not driven, so you can wire the HUD up one
    /// element at a time.
    /// </summary>
    public sealed class CombatHudBinder : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private Image characterIcon;
        [SerializeField] private TMP_Text floorText;
        [SerializeField] private TMP_Text creditsText;
        [SerializeField] private TMP_Text deckCountText;

        [Header("Piles (bottom bar)")]
        [SerializeField] private TMP_Text drawCountText;
        [SerializeField] private TMP_Text discardCountText;

        [Header("Player")]
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private Image playerHpFill;
        [SerializeField] private TMP_Text playerBlockText;
        [SerializeField] private GameObject playerBlockRoot;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private Image energyIcon;

        [Header("Enemy")]
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private TMP_Text enemyHpText;
        [SerializeField] private Image enemyHpFill;
        [SerializeField] private TMP_Text enemyBlockText;
        [SerializeField] private GameObject enemyBlockRoot;
        [SerializeField] private TMP_Text enemyIntentText;

        [Header("Screens (shown/hidden by state)")]
        [SerializeField] private GameObject combatRoot;
        [SerializeField] private GameObject rewardRoot;
        [SerializeField] private GameObject runOverRoot;
        [SerializeField] private TMP_Text runOverText;

        [Header("Buttons")]
        [SerializeField] private Button endTurnButton;
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private Button newRunButton;

        private CombatPresenter _presenter;
        private CharacterPresentation _character;

        [Inject]
        public void Construct(CombatPresenter presenter, CharacterPresentation character)
        {
            _presenter = presenter;
            _character = character;
            _presenter.Changed += Refresh;

            ApplyCharacterArt();

            if (endTurnButton != null)
                endTurnButton.onClick.AddListener(() => _presenter.EndTurn());
            if (newRunButton != null)
                newRunButton.onClick.AddListener(() => _presenter.StartNewRun());

            Refresh();
        }

        private void OnDestroy()
        {
            if (_presenter != null)
                _presenter.Changed -= Refresh;
        }

        // Art is set once: it belongs to the character, not to combat state.
        private void ApplyCharacterArt()
        {
            if (_character == null)
                return;

            SetSprite(characterIcon, _character.Icon);
            SetSprite(energyIcon, _character.EnergyIcon);
        }

        // Only replaces the sprite when one was actually supplied. Switching the
        // Image off for missing art would also throw away whatever placeholder
        // was put in the prefab, which reads as a broken HUD.
        private static void SetSprite(Image target, Sprite sprite)
        {
            if (target != null && sprite != null)
                target.sprite = sprite;
        }

        private void Refresh()
        {
            if (_presenter == null)
                return;

            RunView run = _presenter.Run;
            SetText(floorText, run.Floor.ToString());
            SetText(creditsText, run.Credits.ToString());
            SetText(deckCountText, run.DeckCount.ToString());

            PlayerView player = _presenter.Player;
            SetText(playerHpText, $"{player.Hp}/{player.MaxHp}");
            SetFill(playerHpFill, player.HpFraction);
            SetText(energyText, $"{player.Energy}/{player.MaxEnergy}");
            SetBlock(playerBlockRoot, playerBlockText, player.Block);
            SetText(drawCountText, player.DrawCount.ToString());
            SetText(discardCountText, player.DiscardCount.ToString());
            SetText(turnText, _presenter.Turn.ToString());

            EnemyView enemy = _presenter.Enemy;
            SetText(enemyNameText, enemy.Name);
            SetText(enemyHpText, $"{enemy.Hp}/{enemy.MaxHp}");
            SetFill(enemyHpFill, enemy.HpFraction);
            SetBlock(enemyBlockRoot, enemyBlockText, enemy.Block);
            SetText(enemyIntentText, enemy.IntentAmount > 0
                ? $"{enemy.IntentKind} {enemy.IntentAmount}"
                : enemy.IntentKind);

            SetActive(combatRoot, _presenter.IsInCombat);
            SetActive(rewardRoot, _presenter.IsAwaitingReward);
            SetActive(runOverRoot, _presenter.IsRunOver);
            if (_presenter.IsRunOver)
                SetText(runOverText, run.PlayerWon ? "VICTORY" : "DEFEAT");
        }

        // Block is usually hidden entirely at zero rather than shown as "0".
        private static void SetBlock(GameObject root, TMP_Text text, int block)
        {
            SetActive(root, block > 0);
            SetText(text, block.ToString());
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value;
        }

        /// <summary>Works with a filled Image, or with a plain sprite via its anchor.</summary>
        private static void SetFill(Image target, float fraction)
        {
            if (target == null)
                return;

            fraction = Mathf.Clamp01(fraction);
            if (target.type == Image.Type.Filled)
            {
                target.fillAmount = fraction;
            }
            else
            {
                RectTransform rt = target.rectTransform;
                rt.anchorMax = new Vector2(fraction, rt.anchorMax.y);
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
