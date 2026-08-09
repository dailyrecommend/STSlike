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
        CardType Type { get; }

        /// <summary>Who the effects apply to, and whether the player picks.</summary>
        CardTarget Target { get; }

        CardRarity Rarity { get; }

        /// <summary>
        /// Whether this may be offered as a reward.
        ///
        /// A rules concept, not a filter someone maintains by hand: starter
        /// cards exist to be in the opening deck, not to be won again. With the
        /// catalogue holding every card in the game, the alternative is a
        /// separate curated list that has to be remembered every time a card is
        /// added — and that a mod could not contribute to.
        /// </summary>
        bool Collectable { get; }

        /// <summary>
        /// Keywords this card carries — see <see cref="CardTags"/>. Never null.
        /// What a card *is*, as opposed to what it does; conditions and stances
        /// ask about these when deciding what may be played.
        /// </summary>
        IReadOnlyList<string> Tags { get; }
        IReadOnlyList<ICardEffect> Effects { get; }
    }
}
