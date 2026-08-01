using System.Collections.Generic;
using STSlike.Domain.Cards;

namespace STSlike.Domain.Characters
{
    /// <summary>
    /// What the rules need to know about a playable character: the resources a
    /// run starts with and the deck it starts with.
    ///
    /// Icons, portraits and display names are deliberately absent — those are
    /// presentation, and keeping them out means combat never depends on a
    /// Unity asset being loaded.
    /// </summary>
    public interface ICharacterDefinition
    {
        string Id { get; }
        int MaxHp { get; }
        int MaxEnergy { get; }
        int StartingCredits { get; }

        /// <summary>Expanded copies: three Strikes appear three times.</summary>
        IReadOnlyList<ICardDefinition> StartingDeck { get; }
    }
}
