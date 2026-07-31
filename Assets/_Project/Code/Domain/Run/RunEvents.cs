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

    /// <summary>
    /// The player took one of the offered rewards. Carries the reward itself so
    /// listeners (stats, achievements, UI) can react to what was chosen without
    /// the reward flow having to know about any of them.
    /// </summary>
    public readonly struct RewardTakenEvent
    {
        public readonly int EncounterNumber;
        public readonly Rewards.IRunReward Reward;

        public RewardTakenEvent(int encounterNumber, Rewards.IRunReward reward)
        {
            EncounterNumber = encounterNumber;
            Reward = reward;
        }
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
