namespace STSlike.Domain.Effects
{
    /// <summary>
    /// Strategy: a single, self-contained card effect. One effect kind =
    /// one implementation. Cards are built by composing these.
    /// </summary>
    public interface ICardEffect
    {
        void Execute(EffectContext context);
    }
}
