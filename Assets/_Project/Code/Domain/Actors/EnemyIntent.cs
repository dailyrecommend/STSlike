using System;

namespace STSlike.Domain.Actors
{
    public enum IntentType
    {
        Attack,
    }

    /// <summary>
    /// What an enemy plans to do next turn, shown to the player so they can
    /// react. Stage 1 only has Attack; more kinds (Defend, Buff, ...) slot in
    /// via new <see cref="IntentType"/> values and factory methods.
    /// </summary>
    public sealed class EnemyIntent
    {
        public IntentType Type { get; }
        public int Amount { get; }

        private EnemyIntent(IntentType type, int amount)
        {
            Type = type;
            Amount = amount;
        }

        public static EnemyIntent Attack(int damage)
        {
            if (damage < 0)
                throw new ArgumentOutOfRangeException(nameof(damage), "Damage cannot be negative.");
            return new EnemyIntent(IntentType.Attack, damage);
        }
    }
}
