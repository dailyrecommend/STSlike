using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using STSlike.Data;
using STSlike.Presentation.UI;

namespace STSlike.Presentation.View
{
    /// <summary>
    /// Puts the fight on stage: spawns the player and enemy models, and plays
    /// their motions in response to what happens in combat.
    ///
    /// It listens to the same presenter the HUD does, so the 3D layer and the UI
    /// layer stay independent — either can be rebuilt without disturbing the
    /// other, and neither can reach into the rules.
    /// </summary>
    public sealed class CombatStageBinder : MonoBehaviour
    {
        [SerializeField] private CombatArena arena;

        [Tooltip("Used when a character or enemy has no model assigned yet, so " +
                 "the arena is never empty while art is still being made.")]
        [SerializeField] private ActorView placeholderPrefab;

        [Header("Enemy Motions")]
        [Tooltip("Used only when a pattern step has no animation of its own. " +
                 "The motion normally comes from the enemy asset's routine.")]
        [SerializeField] private string fallbackEnemyAttackMotion = "Attack";

        [Header("Pacing")]
        [Tooltip("Pause after a blow lands before the next one starts. Used only " +
                 "when the card or the pattern step leaves its own hit interval " +
                 "at zero, so authored rhythms always win.")]
        [SerializeField, Min(0f)] private float reactionTime = 0.15f;

        private CombatPresenter _presenter;
        private CombatRunner _runner;
        private CharacterPresentation _character;
        private CombatSequencer _sequencer;

        private ActorView _player;
        private readonly List<ActorView> _enemies = new List<ActorView>();

        // Rhythm of the action currently being announced. The domain resolves a
        // whole action in one synchronous burst — the "started" event and all of
        // its blows arrive back to back — so reading this while handling a hit
        // always gives the interval authored for the move that landed it.
        private float _hitInterval;

        [Inject]
        public void Construct(CombatPresenter presenter, CombatRunner runner,
            CharacterPresentation character, CombatSequencer sequencer)
        {
            _presenter = presenter;
            _runner = runner;
            _character = character;
            _sequencer = sequencer;

            // Spawning waits for CombatStarted, not EncounterStarted: the
            // encounter is announced before the fight is built, so at that point
            // there are no enemies to spawn yet.
            _presenter.CombatStarted += OnCombatStarted;
            _presenter.CardPlayed += OnCardPlayed;
            _presenter.EnemyActionStarted += OnEnemyAction;
            _presenter.Hit += OnHit;
            _presenter.Changed += SyncDeaths;
        }

        private void OnDestroy()
        {
            if (_presenter == null)
                return;

            _presenter.CombatStarted -= OnCombatStarted;
            _presenter.CardPlayed -= OnCardPlayed;
            _presenter.EnemyActionStarted -= OnEnemyAction;
            _presenter.Hit -= OnHit;
            _presenter.Changed -= SyncDeaths;
        }

        // ---- staging ----

        private void OnCombatStarted()
        {
            EnsurePlayer();
            SpawnEnemies();
        }

        private void EnsurePlayer()
        {
            if (_player == null)
                _player = Spawn(_character?.ModelPrefab, arena != null ? arena.PlayerSlot : transform);

            _player?.ResetActor();
        }

        private void SpawnEnemies()
        {
            IReadOnlyList<EnemyData> data = _runner.CurrentEnemyData;

            // Clear the previous room: models belong to one fight, not the run.
            foreach (ActorView enemy in _enemies)
            {
                if (enemy != null)
                    Destroy(enemy.gameObject);
            }
            _enemies.Clear();

            int count = Mathf.Max(1, data.Count);
            for (int i = 0; i < count; i++)
            {
                GameObject prefab = i < data.Count && data[i] != null ? data[i].ModelPrefab : null;
                Transform slot = arena != null ? arena.EnemySlot(i) : transform;

                _enemies.Add(Spawn(prefab, slot));
            }
        }

        private ActorView Spawn(GameObject prefab, Transform slot)
        {
            GameObject source = prefab != null
                ? prefab
                : placeholderPrefab != null ? placeholderPrefab.gameObject : null;

            if (source == null)
                return null;

            GameObject instance = Instantiate(source, slot.position, slot.rotation, slot);

            // The model may not carry an ActorView; add one so it can still be
            // placed and hidden, just without animation.
            if (!instance.TryGetComponent(out ActorView view))
                view = instance.AddComponent<ActorView>();

            return view;
        }

        // ---- reactions ----

        /// <summary>
        /// The character's motion comes from the card, so every card can have its
        /// own — and the wind-up is held here so the flinch that follows lands
        /// with the swing instead of before it.
        /// </summary>
        private void OnCardPlayed(CardView card)
        {
            ActorView player = _player;
            string motion = card.AnimationTrigger;
            float windUp = card.ImpactDelay;
            _hitInterval = card.HitInterval;

            _sequencer.Enqueue(async () =>
            {
                player?.Play(motion);
                await Wait(windUp);
            });
        }

        /// <summary>
        /// One motion per action, however many blows it lands. A three-hit
        /// flurry is a single animation that swings three times — replaying it
        /// per hit would restart it from the first frame each time.
        /// </summary>
        private void OnEnemyAction(ActorActionView action)
        {
            ActorView actor = Resolve(action.Actor);
            string motion = string.IsNullOrEmpty(action.Motion)
                ? fallbackEnemyAttackMotion
                : action.Motion;
            _hitInterval = action.HitInterval;

            _sequencer.Enqueue(async () =>
            {
                actor?.Play(motion);
                await Wait(action.ImpactDelay);
            });
        }

        private void OnHit(HitView hit)
        {
            ActorView target = Resolve(hit.Target);

            // Read now, not inside the closure: by the time the queue drains,
            // later actions will have moved the field on.
            float gap = _hitInterval > 0f ? _hitInterval : reactionTime;

            // Only the reaction is queued here; whoever swung already started
            // their motion when the action began.
            _sequencer.Enqueue(async () =>
            {
                target?.PlayHit();
                await Wait(gap);
            });
        }

        private static async UniTask Wait(float seconds)
        {
            if (seconds > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), DelayType.UnscaledDeltaTime);
        }


        /// <summary>
        /// Plays the death motion once an enemy's health reaches zero. Driven
        /// from the state rather than from a damage event, so an enemy killed by
        /// any means — poison, a reflected hit — still falls over.
        /// </summary>
        private void SyncDeaths()
        {
            IReadOnlyList<EnemyView> enemies = _presenter.Enemies;

            for (int i = 0; i < _enemies.Count && i < enemies.Count; i++)
            {
                if (enemies[i].Hp <= 0)
                    _enemies[i]?.PlayDeath();
            }
        }

        private ActorView Resolve(ActorRef actor)
        {
            if (actor.IsPlayer)
                return _player;

            int index = actor.EnemyIndex;
            return index >= 0 && index < _enemies.Count ? _enemies[index] : null;
        }
    }
}
