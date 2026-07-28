namespace STSlike.Domain.Run
{
    /// <summary>A run has begun (or restarted).</summary>
    public readonly struct RunStartedEvent { }

    /// <summary>An encounter is starting.</summary>
    public readonly struct EncounterStartedEvent
    {
        public readonly int EncounterNumber;
        public readonly int TotalEncounters;
        public EncounterStartedEvent(int encounterNumber, int totalEncounters)
        {
            EncounterNumber = encounterNumber;
            TotalEncounters = totalEncounters;
        }
    }

    /// <summary>An encounter was cleared; the player is being offered a reward.</summary>
    public readonly struct RewardOfferedEvent
    {
        public readonly int EncounterNumber;
        public RewardOfferedEvent(int encounterNumber) => EncounterNumber = encounterNumber;
    }

    /// <summary>The player took one of the offered rewards.</summary>
    public readonly struct RewardTakenEvent
    {
        public readonly int EncounterNumber;
        public RewardTakenEvent(int encounterNumber) => EncounterNumber = encounterNumber;
    }

    /// <summary>The run is over, either cleared or failed.</summary>
    public readonly struct RunEndedEvent
    {
        public readonly bool PlayerWon;
        public readonly int EncountersCleared;
        public RunEndedEvent(bool playerWon, int encountersCleared)
        {
            PlayerWon = playerWon;
            EncountersCleared = encountersCleared;
        }
    }
}
