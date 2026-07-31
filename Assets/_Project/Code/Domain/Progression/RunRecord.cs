using System;

namespace STSlike.Domain.Progression
{
    /// <summary>
    /// One finished run, as persisted for meta progression. The seed is stored
    /// alongside the result so a run can be replayed or inspected later — that
    /// is why every random draw in a run comes from a single seeded stream.
    /// </summary>
    public sealed class RunRecord
    {
        public Guid RunId { get; }
        public string CharacterId { get; }
        public int Seed { get; }
        public bool PlayerWon { get; }
        public int EncountersCleared { get; }
        public int TotalEncounters { get; }
        public int DeckSize { get; }
        public DateTime CompletedAtUtc { get; }

        public RunRecord(Guid runId, string characterId, int seed, bool playerWon,
            int encountersCleared, int totalEncounters, int deckSize, DateTime completedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                throw new ArgumentException("CharacterId must be provided.", nameof(characterId));

            RunId = runId;
            CharacterId = characterId;
            Seed = seed;
            PlayerWon = playerWon;
            EncountersCleared = encountersCleared;
            TotalEncounters = totalEncounters;
            DeckSize = deckSize;
            CompletedAtUtc = completedAtUtc;
        }
    }
}
