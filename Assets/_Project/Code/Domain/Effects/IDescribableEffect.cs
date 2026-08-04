namespace STSlike.Domain.Effects
{
    /// <summary>
    /// An effect that can contribute a number to its card's description text.
    ///
    /// Card text is written with placeholders ("Deal {0} damage") and the values
    /// are pulled from the effects themselves, so upgrading a card — or
    /// rebalancing it in the inspector — updates the text automatically instead
    /// of leaving a stale hand-written number behind.
    ///
    /// Opting in is voluntary: effects with nothing to show (draw a card, gain
    /// energy) simply do not implement it.
    /// </summary>
    public interface IDescribableEffect
    {
        int DisplayValue { get; }
    }
}
