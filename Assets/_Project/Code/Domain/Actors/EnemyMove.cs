using System;

namespace STSlike.Domain.Actors
{
    /// <summary>
    /// One step in an enemy's routine, as authored in data: what it does, how
    /// much, and how many times.
    ///
    /// A pattern is just a list of these, which is enough to give an enemy a
    /// recognisable rhythm ("hits three times, then guards") without any code.
    /// </summary>
    [Serializable]
    public struct EnemyMove
    {
        public IntentType type;
        public int amount;

        /// <summary>
        /// Repeats for a damaging move: 5 x3 rather than 15. The distinction
        /// matters — each hit is absorbed by block separately, so many small
        /// hits cut through a guard that a single big one would not.
        /// </summary>
        public int hits;

        public EnemyMove(IntentType type, int amount, int hits = 1)
        {
            this.type = type;
            this.amount = amount;
            this.hits = hits;
        }

        public IntentType Type => type;
        public int Amount => amount;

        /// <summary>At least one, so an unset field in the inspector still works.</summary>
        public int Hits => hits < 1 ? 1 : hits;
    }
}
