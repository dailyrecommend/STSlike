using System;

namespace STSlike.Domain.Run.Rewards
{
    /// <summary>
    /// Restores health. Meaningful only because the player persists across
    /// encounters — trading a card pickup for survivability is the choice.
    /// </summary>
    public sealed class HealReward : IRunReward
    {
        public int Amount { get; }

        public HealReward(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Heal amount must be positive.");
            Amount = amount;
        }

        public void Apply(RunContext run)
        {
            run.Player.Heal(Amount);
        }
    }
}
