using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using STSlike.Domain.Combat;
using STSlike.Domain.Events;
using STSlike.Domain.Run;
using STSlike.Domain.Run.Rewards;

namespace STSlike.Presentation
{
    /// <summary>
    /// Minimal IMGUI developer harness for playing/verifying the run while the
    /// real presentation is built separately. Injected by the container. Reads
    /// live state for the HUD and plays the event log one entry at a time
    /// through the UniTask sequencer. Not the game's UI — a throwaway dev tool.
    /// </summary>
    public sealed class CombatDebugView : MonoBehaviour
    {
        [SerializeField] private int fontSize = 26;
        [SerializeField] private float panelWidth = 780f;
        [SerializeField] private int stepDelayMs = 220;

        private IEventBus _bus;
        private CombatRunner _runner;
        private RunFlow _flow;
        private CombatSequencer _sequencer;
        private readonly List<string> _log = new List<string>();

        [Inject]
        public void Construct(IEventBus bus, CombatRunner runner, RunFlow flow, CombatSequencer sequencer)
        {
            _bus = bus;
            _runner = runner;
            _flow = flow;
            _sequencer = sequencer;

            bus.Subscribe<RunStartedEvent>(OnRunStarted);
            bus.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
            bus.Subscribe<PlayerTurnStartedEvent>(OnTurnStarted);
            bus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            bus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            bus.Subscribe<BlockGainedEvent>(OnBlockGained);
            bus.Subscribe<PlayerTurnEndedEvent>(OnTurnEnded);
            bus.Subscribe<CombatEndedEvent>(OnCombatEnded);
            bus.Subscribe<RewardOfferedEvent>(OnRewardOffered);
            bus.Subscribe<RewardTakenEvent>(OnRewardTaken);
            bus.Subscribe<RunEndedEvent>(OnRunEnded);
        }

        private void OnDestroy()
        {
            if (_bus == null)
                return;
            _bus.Unsubscribe<RunStartedEvent>(OnRunStarted);
            _bus.Unsubscribe<EncounterStartedEvent>(OnEncounterStarted);
            _bus.Unsubscribe<PlayerTurnStartedEvent>(OnTurnStarted);
            _bus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            _bus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            _bus.Unsubscribe<BlockGainedEvent>(OnBlockGained);
            _bus.Unsubscribe<PlayerTurnEndedEvent>(OnTurnEnded);
            _bus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
            _bus.Unsubscribe<RewardOfferedEvent>(OnRewardOffered);
            _bus.Unsubscribe<RewardTakenEvent>(OnRewardTaken);
            _bus.Unsubscribe<RunEndedEvent>(OnRunEnded);
        }

        private void OnRunStarted(RunStartedEvent e) => Step("=== run started ===");
        private void OnEncounterStarted(EncounterStartedEvent e) =>
            Step($"— encounter {e.EncounterNumber}/{e.TotalEncounters} —");
        private void OnTurnStarted(PlayerTurnStartedEvent e) => Step("player turn started");
        private void OnCardPlayed(CardPlayedEvent e) => Step($"played: {e.Card.Id}");
        private void OnDamageDealt(DamageDealtEvent e) => Step($"    dmg {e.Amount} -> {e.Target.Name}");
        private void OnBlockGained(BlockGainedEvent e) => Step($"    block +{e.Amount} ({e.Actor.Name})");
        private void OnTurnEnded(PlayerTurnEndedEvent e) => Step("turn ended -> enemy acts");
        private void OnCombatEnded(CombatEndedEvent e) => Step(e.PlayerWon ? "combat won" : "combat lost");
        private void OnRewardOffered(RewardOfferedEvent e) => Step($"reward offered (after {e.EncounterNumber})");
        private void OnRewardTaken(RewardTakenEvent e) => Step("reward taken");
        private void OnRunEnded(RunEndedEvent e) =>
            Step(e.PlayerWon ? $"RUN CLEARED ({e.EncountersCleared})" : $"RUN FAILED (cleared {e.EncountersCleared})");

        // Each log line is a sequencer step: append it, then wait, so entries
        // reveal one at a time instead of all at once.
        private void Step(string line)
        {
            _sequencer.Enqueue(async () =>
            {
                _log.Add(line);
                if (_log.Count > 10)
                    _log.RemoveAt(0);
                await UniTask.Delay(stepDelayMs);
            });
        }

        private void OnGUI()
        {
            if (_flow == null || _runner == null)
                return;

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize };
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize + 6,
                fontStyle = FontStyle.Bold,
            };
            var logStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(fontSize * 0.72f) };
            var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
            float buttonHeight = fontSize * 2.2f;

            GUILayout.BeginArea(new Rect(20, 20, panelWidth, Screen.height - 40), GUI.skin.box);
            GUILayout.Space(8);

            RunContext run = _flow.Run;
            GUILayout.Label($"Encounter {run.CurrentEncounter}/{run.TotalEncounters}", titleStyle);

            // Run-persistent state: this HP carries between encounters.
            GUILayout.Label(
                $"RUN   HP {run.Player.CurrentHp}/{run.Player.MaxHp}   Deck {run.Cards.Count}", labelStyle);
            GUILayout.Space(10);

            if (run.IsFinished)
            {
                GUILayout.Label(run.PlayerWon ? "RUN CLEARED" : "RUN FAILED", titleStyle);
                if (GUILayout.Button("New Run", buttonStyle, GUILayout.Height(buttonHeight)))
                    _flow.Restart();
            }
            else if (_flow.IsAwaitingReward)
            {
                GUILayout.Label("Encounter cleared — take a reward", labelStyle);
                GUILayout.Space(8);

                var rewards = _flow.CurrentRewards;
                for (int i = 0; i < rewards.Count; i++)
                {
                    if (GUILayout.Button(Describe(rewards[i]), buttonStyle, GUILayout.Height(buttonHeight)))
                    {
                        _flow.ChooseReward(i);
                        break; // options are gone this frame
                    }
                }

                GUILayout.Space(12);
                if (GUILayout.Button("Skip", buttonStyle, GUILayout.Height(buttonHeight)))
                    _flow.SkipReward();
            }
            else
            {
                DrawCombat(labelStyle, buttonStyle, buttonHeight);
            }

            GUILayout.Space(16);
            GUILayout.Label("Event log", labelStyle);
            for (int i = _log.Count - 1; i >= 0; i--)
                GUILayout.Label(_log[i], logStyle);

            GUILayout.EndArea();
        }

        // Dev-tool formatting only; real reward text will come from the
        // localization tables, not from strings baked into a view.
        private static string Describe(IRunReward reward)
        {
            switch (reward)
            {
                case AddCardReward card:
                    return $"Add card:  {card.Card.Id}  (cost {card.Card.Cost})";
                case HealReward heal:
                    return $"Heal  +{heal.Amount}";
                default:
                    return reward.GetType().Name;
            }
        }

        private void DrawCombat(GUIStyle labelStyle, GUIStyle buttonStyle, float buttonHeight)
        {
            CombatManager manager = _runner.Current;
            if (manager == null)
                return;

            CombatContext ctx = manager.Context;
            var enemy = ctx.Enemy;
            var player = ctx.Player;

            GUILayout.Label($"Phase: {ctx.Phase}", labelStyle);
            GUILayout.Space(6);
            GUILayout.Label(
                $"ENEMY   HP {enemy.CurrentHp}/{enemy.MaxHp}   Block {enemy.Block}" +
                $"   Intent: {enemy.Intent.Type} {enemy.Intent.Amount}", labelStyle);
            GUILayout.Label(
                $"PLAYER  HP {player.CurrentHp}/{player.MaxHp}   Block {player.Block}" +
                $"   Energy {player.Energy}/{player.MaxEnergy}", labelStyle);

            GUILayout.Space(16);

            if (ctx.IsOver)
                return; // the flow is about to move on

            GUILayout.Label("Hand  (click a card to play it)", labelStyle);
            var hand = player.Deck.Hand.Cards;
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                GUI.enabled = player.CanAfford(card.Cost);
                if (GUILayout.Button($"{card.Id}   (cost {card.Cost})", buttonStyle, GUILayout.Height(buttonHeight)))
                {
                    manager.PlayCard(card, enemy);
                    GUI.enabled = true;
                    break; // hand changed this frame; stop iterating
                }
                GUI.enabled = true;
            }

            GUILayout.Space(12);
            if (GUILayout.Button("End Turn", buttonStyle, GUILayout.Height(buttonHeight)))
                manager.EndTurn();

            GUILayout.Space(6);
            GUILayout.Label($"commands run: {manager.CommandHistory.Count}", labelStyle);
        }
    }
}
