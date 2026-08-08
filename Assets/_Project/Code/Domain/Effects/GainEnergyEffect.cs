using System;
using STSlike.Domain.Actors;
using STSlike.Domain.Combat;

namespace STSlike.Domain.Effects
{
    /// <summary>
    /// Hands energy back to whoever played the card.
    ///
    /// Always the source, never the target: a card that pays for itself gives
    /// energy to the person who spent it, wherever it was aimed.
    /// </summary>
    [Serializable]
    public sealed class GainEnergyEffect : ICardEffect, IDescribableEffect
    {
        public int amount;

        public int DisplayValue => amount;

        public GainEnergyEffect() { }

        public GainEnergyEffect(int amount)
        {
            this.amount = amount;
        }

        public void Execute(EffectContext context)
        {
            // Enemies have no energy, so an enemy running this does nothing
            // rather than throwing.
            PlayerState player = context.Player;
            if (player == null || amount <= 0)
                return;

            player.GainEnergy(amount);
            context.Bus?.Publish(new EnergyGainedEvent(player, amount));
        }
    }
}
