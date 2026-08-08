using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using STSlike.Data;
using STSlike.Domain.Actors;
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
    public sealed class CombatStageBinder : MonoBehaviour, IActorStage
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

        [Tooltip("Logs the gap actually achieved between blows, next to the one " +
                 "asked for and the frame time that had to deliver it. A blow can " +
                 "only land on a frame, so an interval near the frame time cannot " +
                 "be exact — this is how to tell that apart from a real fault.")]
        [SerializeField] private bool logHitTiming;

        private CombatPresenter _presenter;
        private CombatRunner _runner;
        private CharacterPresentation _character;
        private CombatSequencer _sequencer;
        private CombatDisplayState _display;

        private ActorView _player;
        private readonly List<ActorView> _enemies = new List<ActorView>();

        // Rhythm of the action currently being announced. The domain resolves a
        // whole action in one synchronous burst — the "started" event and all of
        // its blows arrive back to back — so reading this while handling a hit
        // always gives the interval authored for the move that landed it.
        private float _hitInterval;

        // Unscaled time the next blow is due. Blows are scheduled against this
        // running clock rather than by sleeping between them: a sleep rounds up
        // to a frame boundary, and across an eight-hit flurry that rounding
        // stacks into a visibly slower rhythm than the one authored.
        private double _beat;

        // Only for the timing log.
        private double _lastBlow;

        [Inject]
        public void Construct(CombatPresenter presenter, CombatRunner runner,
            CharacterPresentation character, CombatSequencer sequencer,
            CombatDisplayState display)
        {
            _presenter = presenter;
            _runner = runner;
            _character = character;
            _sequencer = sequencer;
            _display = display;

            // Spawning waits for CombatStarted, not EncounterStarted: the
            // encounter is announced before the fight is built, so at that point
            // there are no enemies to spawn yet.
            _presenter.CombatStarted += OnCombatStarted;
            _presenter.CardPlayed += OnCardPlayed;
            _presenter.EnemyActionStarted += OnEnemyAction;
            _presenter.Hit += OnHit;
            _presenter.Changed += SyncDeaths;

            // A killing blow is only fatal once it has been played, so the last
            // release of a flurry is what drops the body.
            _display.Changed += SyncDeaths;
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

            if (_display != null)
                _display.Changed -= SyncDeaths;
        }

        // ---- staging ----

        private void OnCombatStarted()
        {
            // Blows owed by the last fight will never be played now.
            _display.Clear();

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
            IReadOnlyList<IEnemyDefinition> data = _runner.CurrentEnemies;

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
                // Only content authored as an asset carries a model. Anything
                // else falls to the placeholder rather than failing to appear.
                GameObject prefab = i < data.Count && data[i] is EnemyData asset
                    ? asset.ModelPrefab
                    : null;

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
                _beat = Time.unscaledTimeAsDouble;
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
                _beat = Time.unscaledTimeAsDouble;
            });
        }

        private void OnHit(HitView hit)
        {
            ActorView target = Resolve(hit.Target);

            // Read now, not inside the closure: by the time the queue drains,
            // later actions will have moved the field on.
            float gap = _hitInterval > 0f ? _hitInterval : reactionTime;

            // The rules have already applied this blow. Park the result so the
            // HUD keeps showing the health from before it until the impact plays.
            ActorRef victim = hit.Target;
            CombatDisplayState.Vitals landed = Read(victim);
            _display.Defer(victim, landed.Hp, landed.Block);

            // A blow that block ate never reached the figure, so it does not
            // flinch — the guard held. The pause still happens either way, so a
            // blocked flurry keeps its rhythm instead of resolving all at once.
            bool hurt = hit.Hurt;

            // Only the reaction is queued here; whoever swung already started
            // their motion when the action began.
            _sequencer.Enqueue(async () =>
            {
                _display.Reveal(victim);

                if (hurt)
                    target?.PlayHit();

                // A blow that arrives outside an action, or a queue that has
                // fallen more than one beat behind, restarts the rhythm here
                // instead of firing what it owes all at once.
                double now = Time.unscaledTimeAsDouble;
                if (_beat < now - gap)
                    _beat = now;

                if (logHitTiming)
                {
                    Debug.Log($"[Stage] blow: {(now - _lastBlow) * 1000.0:F1} ms since the last, " +
                              $"asked for {gap * 1000f:F1} ms, frame is " +
                              $"{Time.unscaledDeltaTime * 1000f:F1} ms " +
                              $"({1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f):F0} fps)");
                    _lastBlow = now;
                }

                _beat += gap;
                await WaitUntil(_beat);
            });
        }

        private static async UniTask Wait(float seconds)
        {
            if (seconds > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), DelayType.UnscaledDeltaTime);
        }

        /// <summary>
        /// Resumes on whichever frame sits closest to an absolute unscaled time.
        ///
        /// Waiting for the first frame strictly past the deadline rounds every
        /// blow up, and on an interval near the frame time that bias is the
        /// difference between a flurry and a jog. Stopping half a frame short
        /// instead splits the error either side of the beat. Overshooting also
        /// shortens the next beat rather than pushing it back, so a run of blows
        /// keeps the average rate it was authored with.
        /// </summary>
        private static async UniTask WaitUntil(double unscaledTime)
        {
            while (Time.unscaledTimeAsDouble + Time.unscaledDeltaTime * 0.5 < unscaledTime)
                await UniTask.Yield(PlayerLoopTiming.Update);
        }


        /// <summary>
        /// Plays the death motion once an enemy's health reaches zero. Driven
        /// from the state rather than from a damage event, so an enemy killed by
        /// any means — poison, a reflected hit — still falls over.
        /// </summary>
        private void SyncDeaths()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                ActorRef actor = ActorRef.Enemy(i);

                // A model standing in a slot the room never filled — the arena
                // always puts someone on stage — has no health to run out.
                if (!_presenter.TryGetActor(actor, out ActorHudView view))
                    continue;

                // Displayed health, not real: an enemy killed by the third blow
                // of a flurry must stay standing until that blow has played.
                if (_display.Of(actor, view.Hp, view.Block).Hp <= 0)
                    _enemies[i]?.PlayDeath();
            }
        }

        /// <summary>The figure's health as the rules have it right now.</summary>
        private CombatDisplayState.Vitals Read(ActorRef actor)
        {
            return _presenter.TryGetActor(actor, out ActorHudView view)
                ? new CombatDisplayState.Vitals(view.Hp, view.Block)
                : default;
        }

        /// <summary>
        /// Hands the overlay the point it should float above. Null until the
        /// models for this fight exist, which is what lets the overlay be built
        /// without caring whether it or the stage was set up first.
        /// </summary>
        public Transform AnchorFor(ActorRef actor)
        {
            ActorView view = Resolve(actor);
            return view != null ? view.UiAnchor : null;
        }

        /// <summary>
        /// Names the figure a piece of geometry belongs to. The search walks up
        /// from whatever was hit, so a collider anywhere on a model — on a limb,
        /// on a child of the rig — identifies the whole figure.
        /// </summary>
        public bool TryIdentify(Transform hit, out ActorRef actor)
        {
            actor = ActorRef.None;
            if (hit == null)
                return false;

            ActorView view = hit.GetComponentInParent<ActorView>();
            if (view == null || view.IsDead)
                return false;

            if (view == _player)
            {
                actor = ActorRef.Player;
                return true;
            }

            int index = _enemies.IndexOf(view);
            if (index < 0)
                return false;

            actor = ActorRef.Enemy(index);
            return true;
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
