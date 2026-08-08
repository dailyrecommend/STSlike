using System;
using System.Collections.Generic;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Characters;

namespace STSlike.Domain.Content
{
    /// <summary>
    /// Merges every source into one catalogue keyed by id.
    ///
    /// Sources are read in the order given and a later one wins, so a source
    /// added after the built-in assets can replace a card rather than only add
    /// to it — which is what patching content means. Overrides are recorded
    /// instead of being silently swallowed: inside one source a repeated id is
    /// an authoring mistake, and the two cases are indistinguishable at the
    /// point they happen.
    ///
    /// Built once and then read. Nothing here reloads.
    /// </summary>
    public sealed class ContentRegistry : IContentRegistry
    {
        private readonly Dictionary<string, ICardDefinition> _cards =
            new Dictionary<string, ICardDefinition>(StringComparer.Ordinal);

        private readonly Dictionary<string, IEnemyDefinition> _enemies =
            new Dictionary<string, IEnemyDefinition>(StringComparer.Ordinal);

        private readonly Dictionary<string, ICharacterDefinition> _characters =
            new Dictionary<string, ICharacterDefinition>(StringComparer.Ordinal);

        private readonly List<ICardDefinition> _cardList = new List<ICardDefinition>();
        private readonly List<IEnemyDefinition> _enemyList = new List<IEnemyDefinition>();
        private readonly List<ICharacterDefinition> _characterList = new List<ICharacterDefinition>();

        private readonly List<string> _overrides = new List<string>();

        /// <summary>
        /// Ids that were claimed more than once, in the form "kind 'id': source
        /// replaced source". Empty for a healthy build; whoever has a console
        /// can report them.
        /// </summary>
        public IReadOnlyList<string> Overrides => _overrides;

        public ContentRegistry(IEnumerable<IContentSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            foreach (IContentSource source in sources)
            {
                if (source == null)
                    continue;

                Absorb(source.Cards, _cards, source.Name, "card", d => d.Id);
                Absorb(source.Enemies, _enemies, source.Name, "enemy", d => d.Id);
                Absorb(source.Characters, _characters, source.Name, "character", d => d.Id);
            }

            _cardList.AddRange(_cards.Values);
            _enemyList.AddRange(_enemies.Values);
            _characterList.AddRange(_characters.Values);
        }

        public ICardDefinition Card(string id) => Find(_cards, id);
        public IEnemyDefinition Enemy(string id) => Find(_enemies, id);
        public ICharacterDefinition Character(string id) => Find(_characters, id);

        public IReadOnlyList<ICardDefinition> Cards => _cardList;
        public IReadOnlyList<IEnemyDefinition> Enemies => _enemyList;
        public IReadOnlyList<ICharacterDefinition> Characters => _characterList;

        private static T Find<T>(Dictionary<string, T> from, string id) where T : class
        {
            return !string.IsNullOrEmpty(id) && from.TryGetValue(id, out T found) ? found : null;
        }

        // Entries with no id are skipped rather than throwing: one unfinished
        // asset should not stop the rest of the game from loading.
        private void Absorb<T>(IEnumerable<T> incoming, Dictionary<string, T> into,
            string sourceName, string kind, Func<T, string> idOf) where T : class
        {
            if (incoming == null)
                return;

            var seenHere = new HashSet<string>(StringComparer.Ordinal);

            foreach (T definition in incoming)
            {
                if (definition == null)
                    continue;

                string id = idOf(definition);
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (into.ContainsKey(id))
                    _overrides.Add(Describe(kind, id, sourceName, seenHere.Contains(id)));

                seenHere.Add(id);
                into[id] = definition;
            }
        }

        private static string Describe(string kind, string id, string source, bool sameSource)
        {
            return sameSource
                ? $"{kind} '{id}': listed twice by '{source}'"
                : $"{kind} '{id}': replaced by '{source}'";
        }
    }
}
