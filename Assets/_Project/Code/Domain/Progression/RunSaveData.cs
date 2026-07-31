using System;
using System.Collections.Generic;

namespace STSlike.Domain.Progression
{
    /// <summary>Where a suspended run was when it was saved.</summary>
    public enum RunSaveStage
    {
        Combat = 0,
        Reward = 1,
    }

    /// <summary>
    /// A snapshot of a run in progress, so a player can quit mid-run and come
    /// back to the same fight.
    ///
    /// <see cref="RngCallCount"/> is what makes the resume exact: restoring only
    /// the seed would rewind every future shuffle and reward roll back to the
    /// start of the run. Replaying that many draws puts the stream back where it
    /// was.
    /// </summary>
    public sealed class RunSaveData
    {
        public string CharacterId { get; }
        public int Seed { get; }
        public int RngCallCount { get; }
        public int CurrentEncounter { get; }
        public int TotalEncounters { get; }
        public int PlayerHp { get; }
        public int PlayerMaxHp { get; }
        public RunSaveStage Stage { get; }
        public IReadOnlyList<string> DeckCardIds { get; }
        public DateTime SavedAtUtc { get; }

        public RunSaveData(string characterId, int seed, int rngCallCount,
            int currentEncounter, int totalEncounters, int playerHp, int playerMaxHp,
            RunSaveStage stage, IReadOnlyList<string> deckCardIds, DateTime savedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                throw new ArgumentException("CharacterId must be provided.", nameof(characterId));
            if (currentEncounter <= 0)
                throw new ArgumentOutOfRangeException(nameof(currentEncounter));
            if (playerHp <= 0)
                throw new ArgumentOutOfRangeException(nameof(playerHp), "A dead run is not resumable.");

            CharacterId = characterId;
            Seed = seed;
            RngCallCount = rngCallCount;
            CurrentEncounter = currentEncounter;
            TotalEncounters = totalEncounters;
            PlayerHp = playerHp;
            PlayerMaxHp = playerMaxHp;
            Stage = stage;
            DeckCardIds = deckCardIds ?? throw new ArgumentNullException(nameof(deckCardIds));
            SavedAtUtc = savedAtUtc;
        }
    }
}
