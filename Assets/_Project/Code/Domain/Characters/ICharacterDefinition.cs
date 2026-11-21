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

        /// <summary>
        /// Rounds this character's weapon holds. Zero for one who carries none,
        /// which is what makes the whole cylinder a character trait rather than
        /// something the rules assume everybody has.
        /// </summary>
        int MaxAmmo { get; }

        /// <summary>Base damage of one round, before anything modifies it.</summary>
        int ShotDamage { get; }

        /// <summary>Expanded copies: three Strikes appear three times.</summary>
        IReadOnlyList<ICardDefinition> StartingDeck { get; }

        /// <summary>
        /// Relics carried from the first room, by id — the character's signature
        /// item rather than something won.
        ///
        /// Ids rather than the relics themselves because a relic keeps its own
        /// state: the run needs a fresh one, not the object the content was
        /// authored on.
        /// </summary>
        IReadOnlyList<string> StartingRelicIds { get; }
    }
}
