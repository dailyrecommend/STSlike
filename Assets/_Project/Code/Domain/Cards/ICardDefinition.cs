using System.Collections.Generic;
using STSlike.Domain.Effects;

namespace STSlike.Domain.Cards
{
    /// <summary>
    /// The domain's view of a card's design data: what it costs and what it
    /// does. Intentionally free of presentation concerns (display name, art)
    /// and of any Unity type, so combat rules depend only on this contract.
    /// ScriptableObjects, in-memory data, or a future bake pipeline can all
    /// supply it.
    /// </summary>
    public interface ICardDefinition
    {
        string Id { get; }
        int Cost { get; }
        IReadOnlyList<ICardEffect> Effects { get; }
    }
}
