using System;
using System.Collections.Generic;
using UnityEngine;
using STSlike.Domain.Actors;

namespace STSlike.Data
{
    /// <summary>
    /// Authoring asset for an enemy. Implements the domain's
    /// <see cref="IEnemyDefinition"/>, so combat consumes it without knowing it
    /// is a Unity asset, while the presentation reads the model and icon.
    ///
    /// Adding an enemy is data: create one of these, write its routine, drop in
    /// a model. No code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_New", menuName = "STSlike/Enemy", order = 2)]
    public sealed class EnemyData : ScriptableObject, IEnemyDefinition
    {
        [Header("Identity")]
        [SerializeField] private string id = "grunt";
        [SerializeField] private EnemyTier tier = EnemyTier.Normal;

        [Header("Stats")]
        [SerializeField, Min(1)] private int maxHp = 40;

        /// <summary>
        /// One step of the routine as authored: the move plus the motion to play
        /// for it. The animation name lives here rather than on the domain move,
        /// for the same reason a card's motion lives on CardData — the rules
        /// never need to know what the model does.
        /// </summary>
        [Serializable]
        private struct PatternStep
        {
            public IntentType type;
            [Min(0)] public int amount;

            [Tooltip("Repeats a damaging move: 5 x3 is three separate hits, each " +
                     "chewing through block on its own.")]
            [Min(1)] public int hits;

            [Tooltip("Animator trigger (or state name) played for this step.")]
            public string animation;

            [Tooltip("Seconds from the start of the motion until the first blow lands.")]
            [Min(0f)] public float impactDelay;

            [Tooltip("Seconds between the blows of a multi-hit move. Match it to " +
                     "the rhythm of the animation's swings.")]
            [Min(0f)] public float hitInterval;
        }

        [Header("Routine (repeats in order)")]
        [Tooltip("The moves this enemy cycles through. A readable rhythm — " +
                 "\"hits three times, then guards\" — is what lets the player plan.")]
        [SerializeField]
        private List<PatternStep> pattern = new List<PatternStep>
        {
            new PatternStep { type = IntentType.Attack, amount = 8, hits = 1, animation = "Attack" },
        };

        [Header("Presentation (read by the view, not by combat)")]
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        [Tooltip("Spawned in the arena when this enemy appears.")]
        [SerializeField] private GameObject modelPrefab;

        public string Id => id;
        public EnemyTier Tier => tier;
        public int MaxHp => maxHp;

        public IReadOnlyList<EnemyMove> Pattern => BuildPattern();

        /// <summary>The motion authored for a step of the routine.</summary>
        public string AnimationFor(int moveIndex)
        {
            if (moveIndex < 0 || moveIndex >= pattern.Count)
                return null;
            return pattern[moveIndex].animation;
        }

        /// <summary>How long that motion winds up before it connects.</summary>
        public float ImpactDelayFor(int moveIndex)
        {
            if (moveIndex < 0 || moveIndex >= pattern.Count)
                return 0f;
            return pattern[moveIndex].impactDelay;
        }

        /// <summary>Gap between the blows of a multi-hit move.</summary>
        public float HitIntervalFor(int moveIndex)
        {
            if (moveIndex < 0 || moveIndex >= pattern.Count)
                return 0f;
            return pattern[moveIndex].hitInterval;
        }

        private List<EnemyMove> BuildPattern()
        {
            var moves = new List<EnemyMove>(pattern.Count);
            foreach (PatternStep step in pattern)
                moves.Add(new EnemyMove(step.type, step.amount, step.hits));
            return moves;
        }

        /// <summary>Falls back to the id so an unfinished enemy is still readable.</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;

        /// <summary>Used for the boss marker in the top bar and intent panels.</summary>
        public Sprite Icon => icon;

        public GameObject ModelPrefab => modelPrefab;
    }
}
