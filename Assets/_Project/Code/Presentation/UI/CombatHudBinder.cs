using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using STSlike.Domain.Map;

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

        [Header("Current Stage")]
        [Tooltip("Shows what kind of stop the player is on: a normal fight, an " +
                 "elite, a rest, the boss. The floor number says how far along " +
                 "the act they are; this says what they walked into. Hidden " +
                 "until they have travelled somewhere.")]
        [SerializeField] private Image stageIcon;

        [Tooltip("Art per kind. A kind with no entry keeps whatever sprite the " +
                 "prefab already has, so unfinished art never blanks the slot.")]
        [SerializeField] private ArtEntry<MapNodeType>[] stageIcons;

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

        // Enemy name, health, block and intent are not here: they float over the
        // models instead. A fight can hold several opponents, and a fixed panel
        // can only ever describe one of them, which is worse than misleading —
        // it quietly reports the wrong enemy's intent.

        [Header("Screens (shown/hidden by state)")]
        [SerializeField] private GameObject combatRoot;
        [SerializeField] private GameObject rewardRoot;
        [SerializeField] private GameObject runOverRoot;
        [SerializeField] private TMP_Text runOverText;

        [Header("Buttons")]
        [SerializeField] private Button endTurnButton;
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private Button newRunButton;

        [Header("Bars")]
        [Tooltip("How quickly a health bar catches up to the value being shown. " +
                 "0 snaps. Timing a hit to its impact is no longer this field's " +
                 "job, so it only needs enough to take the edge off a single " +
                 "blow — too low and a fast flurry outruns the bar entirely.")]
        [SerializeField, Min(0f)] private float barSmoothing = 20f;

        private CombatPresenter _presenter;
        private CharacterPresentation _character;
        private CombatDisplayState _display;

        private float _playerFill = -1f;

        [Inject]
        public void Construct(CombatPresenter presenter, CharacterPresentation character,
            CombatDisplayState display)
        {
            _presenter = presenter;
            _character = character;
            _display = display;
            _presenter.Changed += Refresh;

            // Blows are released one at a time as the stage plays them, which is
            // not something the rules have an opinion about.
            _display.Changed += Refresh;

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
            if (_display != null)
                _display.Changed -= Refresh;
        }

        // Art is set once: it belongs to the character, not to combat state.
        private void ApplyCharacterArt()
        {
            if (_character == null)
                return;

            UiSlot.SetSprite(characterIcon, _character.Icon);
            UiSlot.SetSprite(energyIcon, _character.EnergyIcon);
        }


        private void Refresh()
        {
            if (_presenter == null)
                return;

            RunView run = _presenter.Run;
            UiSlot.SetText(floorText, run.Floor.ToString());
            UiSlot.SetText(creditsText, run.Credits.ToString());
            UiSlot.SetText(deckCountText, run.DeckCount.ToString());
            ApplyStage(run);

            PlayerView player = _presenter.Player;
            CombatDisplayState.Vitals shown = Shown(player);
            UiSlot.SetText(playerHpText, $"{shown.Hp}/{player.MaxHp}");
            UiSlot.SetText(energyText, $"{player.Energy}/{player.MaxEnergy}");
            SetBlock(playerBlockRoot, playerBlockText, shown.Block);
            UiSlot.SetText(drawCountText, player.DrawCount.ToString());
            UiSlot.SetText(discardCountText, player.DiscardCount.ToString());
            UiSlot.SetText(turnText, _presenter.Turn.ToString());

            // Greyed out while the enemy's turn is playing out on screen.
            UiSlot.SetInteractable(endTurnButton, _presenter.CanAct);

            UiSlot.SetActive(combatRoot, _presenter.IsInCombat);
            UiSlot.SetActive(rewardRoot, _presenter.IsAwaitingReward);
            UiSlot.SetActive(runOverRoot, _presenter.IsRunOver);
            if (_presenter.IsRunOver)
                UiSlot.SetText(runOverText, run.PlayerWon ? "VICTORY" : "DEFEAT");
        }

        /// <summary>
        /// Bars are drained here rather than in Refresh so they glide toward the
        /// value on show. They chase the same figure the text prints — a bar and
        /// a number disagreeing about how hurt someone is looks like a bug.
        /// </summary>
        private void Update()
        {
            if (_presenter == null)
                return;

            PlayerView player = _presenter.Player;
            _playerFill = UiSlot.Approach(_playerFill,
                UiSlot.Fraction(Shown(player).Hp, player.MaxHp), barSmoothing);
            UiSlot.SetFill(playerHpFill, _playerFill);
        }

        // Health as the stage has played it so far. Falls through to the real
        // numbers whenever nothing is waiting to be shown, so a HUD with no
        // stage in the scene behaves exactly as it always did.
        private CombatDisplayState.Vitals Shown(PlayerView player) =>
            _display != null
                ? _display.Of(ActorRef.Player, player.Hp, player.Block)
                : new CombatDisplayState.Vitals(player.Hp, player.Block);

        /// <summary>
        /// Names the kind of stop the player is standing on. Uses the same
        /// mapping shape as the map's own node art, so the icon in the top bar
        /// and the icon on the map can be the same sprite.
        /// </summary>
        private void ApplyStage(RunView run)
        {
            if (stageIcon == null)
                return;

            UiSlot.SetActive(stageIcon.gameObject, run.HasNode);

            if (run.HasNode)
                ArtTable.Apply(stageIcons, run.NodeType, stageIcon);
        }

        // Block is usually hidden entirely at zero rather than shown as "0".
        private static void SetBlock(GameObject root, TMP_Text text, int block)
        {
            UiSlot.SetActive(root, block > 0);
            UiSlot.SetText(text, block.ToString());
        }

    }
}
