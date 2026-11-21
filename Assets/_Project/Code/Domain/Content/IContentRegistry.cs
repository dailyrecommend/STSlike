using System.Collections.Generic;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Characters;
using STSlike.Domain.Relics;

namespace STSlike.Domain.Content
{
    /// <summary>
    /// Everything the game knows how to make, looked up by id.
    ///
    /// Ids are the currency: decks, saved runs, reward pools and encounter
    /// tables all name content rather than holding references to it. That is
    /// what lets a run be written to disk and read back, and what lets content
    /// arrive from somewhere the scene never heard of.
    ///
    /// Deliberately a plain catalogue. It answers "what exists" and nothing
    /// about "what should happen" — which enemies suit a floor, or which cards
    /// may be offered as a reward, are rules, and they live with the rules.
    /// </summary>
    public interface IContentRegistry
    {
        /// <summary>Null when nothing claims that id.</summary>
        ICardDefinition Card(string id);

        IEnemyDefinition Enemy(string id);
        ICharacterDefinition Character(string id);
        IEncounterDefinition Encounter(string id);
        IRelicDefinition Relic(string id);

        IReadOnlyList<ICardDefinition> Cards { get; }
        IReadOnlyList<IEnemyDefinition> Enemies { get; }
        IReadOnlyList<ICharacterDefinition> Characters { get; }
        IReadOnlyList<IEncounterDefinition> Encounters { get; }
        IReadOnlyList<IRelicDefinition> Relics { get; }
    }
}
