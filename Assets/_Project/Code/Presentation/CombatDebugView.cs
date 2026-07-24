using System.Collections.Generic;
using UnityEngine;
using VContainer;
using STSlike.Domain.Combat;
using STSlike.Domain.Events;

namespace STSlike.Presentation
{
    /// <summary>
    /// Temporary IMGUI view. Its dependencies (event bus, combat runner) are
    /// injected by VContainer instead of fetched via GetComponent. It reads
    /// combat state each frame for the HUD and subscribes to the bus for a
    /// live combat log.
    /// </summary>
    public sealed class CombatDebugView : MonoBehaviour
    {
        [SerializeField] private int fontSize = 26;
        [SerializeField] private float panelWidth = 780f;

        private IEventBus _bus;
        private CombatRunner _runner;
        private readonly List<string> _log = new List<string>();

        [Inject]
        public void Construct(IEventBus bus, CombatRunner runner)
        {
            _bus = bus;
            _runner = runner;

            bus.Subscribe<CombatStartedEvent>(OnCombatStarted);
            bus.Subscribe<PlayerTurnStartedEvent>(OnTurnStarted);
            bus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            bus.Subscribe<PlayerTurnEndedEvent>(OnTurnEnded);
            bus.Subscribe<CombatEndedEvent>(OnCombatEnded);
        }

        private void OnDestroy()
        {
            if (_bus == null)
                return;
            _bus.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
            _bus.Unsubscribe<PlayerTurnStartedEvent>(OnTurnStarted);
            _bus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            _bus.Unsubscribe<PlayerTurnEndedEvent>(OnTurnEnded);
            _bus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
        }

        private void OnCombatStarted(CombatStartedEvent e) => Log("— combat started —");
        private void OnTurnStarted(PlayerTurnStartedEvent e) => Log("player turn started");
        private void OnCardPlayed(CardPlayedEvent e) => Log($"played: {e.Card.Id}");
        private void OnTurnEnded(PlayerTurnEndedEvent e) => Log("turn ended → enemy acts");
        private void OnCombatEnded(CombatEndedEvent e) => Log(e.PlayerWon ? "WON" : "LOST");

        private void Log(string line)
        {
            _log.Add(line);
            if (_log.Count > 8)
                _log.RemoveAt(0);
        }

        private void OnGUI()
        {
            if (_runner == null || _runner.Current == null)
                return;

            CombatManager manager = _runner.Current;
            CombatContext ctx = manager.Context;

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
            GUILayout.Label($"Phase: {ctx.Phase}", titleStyle);
            GUILayout.Space(10);

            var enemy = ctx.Enemy;
            GUILayout.Label(
                $"ENEMY   HP {enemy.CurrentHp}/{enemy.MaxHp}   Block {enemy.Block}" +
                $"   Intent: {enemy.Intent.Type} {enemy.Intent.Amount}", labelStyle);

            var player = ctx.Player;
            GUILayout.Label(
                $"PLAYER  HP {player.CurrentHp}/{player.MaxHp}   Block {player.Block}" +
                $"   Energy {player.Energy}/{player.MaxEnergy}", labelStyle);

            GUILayout.Space(16);

            if (ctx.IsOver)
            {
                GUILayout.Label(ctx.Phase == CombatPhase.Won ? "YOU WON" : "YOU LOST", titleStyle);
                if (GUILayout.Button("Restart", buttonStyle, GUILayout.Height(buttonHeight)))
                    _runner.StartNew();
            }
            else
            {
                GUILayout.Label("Hand  (click a card to play it; damage hits the enemy)", labelStyle);
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
            }

            GUILayout.Space(16);
            GUILayout.Label("Event log", labelStyle);
            for (int i = _log.Count - 1; i >= 0; i--)
                GUILayout.Label(_log[i], logStyle);

            GUILayout.EndArea();
        }
    }
}
