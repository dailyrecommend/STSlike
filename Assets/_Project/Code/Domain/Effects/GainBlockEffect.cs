using System;

namespace STSlike.Domain.Effects
{
    /// <summary>Grants block to the caster (the one who played the card).</summary>
    [Serializable]
    public sealed class GainBlockEffect : ICardEffect
    {
        public int amount;

        public GainBlockEffect() { }

        public GainBlockEffect(int amount)
        {
            this.amount = amount;
        }

        public void Execute(EffectContext context)
        {
            context.Source.GainBlock(amount);
        }
    }
}
