using System;

namespace STSlike.Domain.Actors
{
    /// <summary>What an enemy is about to do, shown to the player in advance.</summary>
    public enum IntentType
    {
        Attack = 0,
        Defend = 1,
        Buff = 2,

        /// <summary>Deliberately hidden — for enemies that should be unpredictable.</summary>
        Unknown = 3,
    }

    /// <summary>
    /// An enemy's announced move for the coming turn.
    ///
    /// Telegraphing the move is what turns combat into a puzzle: the player can
    /// only decide between attacking and blocking if they know what is coming.
    /// </summary>
    public sealed class EnemyIntent
    {
        public IntentType Type { get; }

        /// <summary>Damage or block per hit.</summary>
        public int Amount { get; }

        /// <summary>How many times it lands. Always at least 1.</summary>
        public int Hits { get; }

        /// <summary>
        /// Which step of the enemy's routine this is, or -1 when it did not come
        /// from one. The view uses it to look up the motion authored beside that
        /// step, which keeps animation names out of the rules.
        /// </summary>
        public int MoveIndex { get; }

        /// <summary>Total damage if nothing blocks it — for display only.</summary>
        public int TotalAmount => Amount * Hits;

        private EnemyIntent(IntentType type, int amount, int hits, int moveIndex)
        {
            Type = type;
            Amount = amount;
            Hits = hits;
            MoveIndex = moveIndex;
        }

        public static EnemyIntent Attack(int damage, int hits = 1) => Make(IntentType.Attack, damage, hits, -1);

        public static EnemyIntent Defend(int block) => Make(IntentType.Defend, block, 1, -1);

        public static EnemyIntent Buff(int amount) => Make(IntentType.Buff, amount, 1, -1);

        public static EnemyIntent Unknown() => new EnemyIntent(IntentType.Unknown, 0, 1, -1);

        public static EnemyIntent From(EnemyMove move, int moveIndex = -1) =>
            Make(move.Type, move.Amount, move.Hits, moveIndex);

        private static EnemyIntent Make(IntentType type, int amount, int hits, int moveIndex)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Intent amounts cannot be negative.");

            return new EnemyIntent(type, amount, Math.Max(1, hits), moveIndex);
        }
    }
}
