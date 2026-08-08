using System;

namespace STSlike.Domain.Actors
{
    /// <summary>
    /// A participant in combat (player or enemy). Owns health and block,
    /// and the rules for how incoming damage is absorbed.
    /// </summary>
    public class Combatant
    {
        public string Name { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public int Block { get; private set; }

        public bool IsDead => CurrentHp <= 0;

        public Combatant(string name, int maxHp)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name must be provided.", nameof(name));
            if (maxHp <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHp), "Max HP must be positive.");

            Name = name;
            MaxHp = maxHp;
            CurrentHp = maxHp;
            Block = 0;
        }

        public void GainBlock(int amount)
        {
            if (amount <= 0)
                return;
            Block += amount;
        }

        /// <summary>
        /// Block absorbs damage first; the remainder reduces HP. Returns the
        /// health actually lost, which is not the amount asked for: block may
        /// have eaten all of it, and a killing blow only takes what is left.
        /// </summary>
        public int TakeDamage(int amount)
        {
            if (amount <= 0)
                return 0;

            int absorbed = Math.Min(Block, amount);
            Block -= absorbed;

            int remaining = amount - absorbed;
            int lost = Math.Min(CurrentHp, remaining);
            CurrentHp -= lost;
            return lost;
        }

        /// <summary>Called at the start of a turn: block does not carry over.</summary>
        public void ResetBlock()
        {
            Block = 0;
        }

        /// <summary>
        /// Restores health up to the maximum. Does nothing to a dead combatant —
        /// reviving is a separate concern, not a side effect of healing.
        /// </summary>
        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead)
                return;
            CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
        }

        /// <summary>Back to full health (used when a fresh run begins).</summary>
        public void RestoreToFull()
        {
            CurrentHp = MaxHp;
        }
    }
}
