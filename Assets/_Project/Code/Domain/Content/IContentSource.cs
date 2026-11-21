using System.Collections.Generic;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Characters;
using STSlike.Domain.Relics;

namespace STSlike.Domain.Content
{
    /// <summary>
    /// Somewhere content comes from.
    ///
    /// The game does not care whether a card was authored as a Unity asset, read
    /// out of a file a player wrote, or built in a test — only that it satisfies
    /// the definition contracts. Keeping that as an interface is what allows a
    /// second source to be added later without the rules noticing: the assets
    /// stay one supplier among several rather than the only way in.
    ///
    /// A source is read once, when the registry is built. Nothing here is asked
    /// to stay live.
    /// </summary>
    public interface IContentSource
    {
        /// <summary>Named for logs and error messages; never shown to a player.</summary>
        string Name { get; }

        IEnumerable<ICardDefinition> Cards { get; }
        IEnumerable<IEnemyDefinition> Enemies { get; }
        IEnumerable<ICharacterDefinition> Characters { get; }

        /// <summary>Rooms the player can walk into.</summary>
        IEnumerable<IEncounterDefinition> Encounters { get; }

        /// <summary>Relics that can be carried.</summary>
        IEnumerable<IRelicDefinition> Relics { get; }
    }
}
