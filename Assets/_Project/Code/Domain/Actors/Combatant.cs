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

        /// <summary>Block absorbs damage first; the remainder reduces HP.</summary>
        public void TakeDamage(int amount)
        {
            if (amount <= 0)
                return;

            int absorbed = Math.Min(Block, amount);
            Block -= absorbed;

            int remaining = amount - absorbed;
            CurrentHp = Math.Max(0, CurrentHp - remaining);
        }

        /// <summary>Called at the start of a turn: block does not carry over.</summary>
        public void ResetBlock()
        {
            Block = 0;
        }
    }
}
