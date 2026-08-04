using STSlike.Domain.Map;

namespace STSlike.Domain.Run
{
    /// <summary>A run has begun (or restarted).</summary>
    public readonly struct RunStartedEvent { }

    /// <summary>
    /// The player is being asked which node to travel to. The choices are on the
    /// run context, so the map screen can read them when this arrives.
    /// </summary>
    public readonly struct MapChoiceOfferedEvent { }

    /// <summary>The player moved onto a node; its content is about to start.</summary>
    public readonly struct NodeEnteredEvent
    {
        public readonly MapNode Node;
        public NodeEnteredEvent(MapNode node) => Node = node;
    }

    /// <summary>A fight is starting on this floor.</summary>
    public readonly struct EncounterStartedEvent
    {
        public readonly int Floor;
        public readonly int TotalFloors;
        public readonly MapNodeType NodeType;

        public EncounterStartedEvent(int floor, int totalFloors, MapNodeType nodeType)
        {
            Floor = floor;
            TotalFloors = totalFloors;
            NodeType = nodeType;
        }
    }

    /// <summary>A non-combat node is being visited and is waiting on the player.</summary>
    public readonly struct NodeVisitStartedEvent
    {
        public readonly MapNodeType NodeType;
        public NodeVisitStartedEvent(MapNodeType nodeType) => NodeType = nodeType;
    }

    /// <summary>A rest stop restored health.</summary>
    public readonly struct RestedEvent
    {
        public readonly int HealedAmount;
        public RestedEvent(int healedAmount) => HealedAmount = healedAmount;
    }

    /// <summary>Treasure was collected.</summary>
    public readonly struct TreasureFoundEvent
    {
        public readonly int Credits;
        public TreasureFoundEvent(int credits) => Credits = credits;
    }

    /// <summary>An encounter was cleared; the player is being offered a reward.</summary>
    public readonly struct RewardOfferedEvent
    {
        public readonly int Floor;
        public RewardOfferedEvent(int floor) => Floor = floor;
    }

    /// <summary>
    /// The player took one of the offered rewards. Carries the reward itself so
    /// listeners (stats, achievements, UI) can react to what was chosen without
    /// the reward flow having to know about any of them.
    /// </summary>
    public readonly struct RewardTakenEvent
    {
        public readonly int Floor;
        public readonly Rewards.IRunReward Reward;

        public RewardTakenEvent(int floor, Rewards.IRunReward reward)
        {
            Floor = floor;
            Reward = reward;
        }
    }

    /// <summary>The run is over, either cleared or failed.</summary>
    public readonly struct RunEndedEvent
    {
        public readonly bool PlayerWon;
        public readonly int FloorsCleared;

        public RunEndedEvent(bool playerWon, int floorsCleared)
        {
            PlayerWon = playerWon;
            FloorsCleared = floorsCleared;
        }
    }
}
