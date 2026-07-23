using UnityEngine;
using STSlike.Domain.Combat;

namespace STSlike.Presentation
{
    /// <summary>
    /// Temporary IMGUI view that proves the core loop is playable on screen.
    /// It reads the CombatManager each frame and turns clicks into PlayCard /
    /// EndTurn calls. Stage 2 replaces this with a proper uGUI view that
    /// redraws only on the manager's StateChanged event.
    /// </summary>
    [RequireComponent(typeof(CombatBootstrap))]
    public sealed class CombatDebugView : MonoBehaviour
    {
        [SerializeField] private int fontSize = 26;
        [SerializeField] private float panelWidth = 780f;

        private CombatBootstrap _bootstrap;

        private void Awake() => _bootstrap = GetComponent<CombatBootstrap>();

        private void OnGUI()
        {
            CombatManager manager = _bootstrap.Manager;
            if (manager == null)
                return;

            CombatContext ctx = manager.Context;

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize };
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize + 6,
                fontStyle = FontStyle.Bold,
            };
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
                    _bootstrap.StartNewCombat();
                GUILayout.EndArea();
                return;
            }

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

            GUILayout.Space(16);
            if (GUILayout.Button("End Turn", buttonStyle, GUILayout.Height(buttonHeight)))
                manager.EndTurn();

            GUILayout.EndArea();
        }
    }
}
