using System;
using System.Collections.Generic;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Map;

namespace STSlike.Domain.Run
{
    /// <summary>
    /// Everything that persists for the length of a run: the player (so health
    /// carries between fights, which is what makes block and healing matter),
    /// the deck collected so far, the act map, and where on it the player
    /// currently stands.
    /// </summary>
    public sealed class RunContext
    {
        private readonly List<Card> _starterDeck;
        private readonly List<Card> _cards = new List<Card>();
        private readonly int _startingCredits;

        // The route taken so far, so the map can show where the player has been.
        private readonly HashSet<int> _visited = new HashSet<int>();

        /// <summary>Lives for the whole run; combats reuse this instance.</summary>
        public PlayerState Player { get; }

        /// <summary>The run's deck. Rewards add to this between fights.</summary>
        public IReadOnlyList<Card> Cards => _cards;

        /// <summary>The act being climbed.</summary>
        public RunMap Map { get; }

        /// <summary>Where the player stands, or null before the first choice.</summary>
        public MapNode CurrentNode { get; private set; }

        /// <summary>The run's RNG seed, stored with its history so it can be replayed.</summary>
        public int Seed { get; }

        /// <summary>Which character is being played; drives the portrait and starting deck.</summary>
        public string CharacterId { get; }

        /// <summary>Run currency, spent at shops and awarded for clearing fights.</summary>
        public int Credits { get; private set; }

        public bool IsFinished { get; private set; }
        public bool PlayerWon { get; private set; }

        /// <summary>1-based floor for display. Zero before the run has begun.</summary>
        public int Floor => CurrentNode != null ? CurrentNode.Row + 1 : 0;

        public int TotalFloors => Map.RowCount;

        /// <summary>Floors fully cleared, used when the run is recorded.</summary>
        public int FloorsCleared => CurrentNode != null ? CurrentNode.Row : 0;

        public bool IsOnBoss => CurrentNode != null && CurrentNode.Type == MapNodeType.Boss;

        public RunContext(RunMap map, PlayerState player, IReadOnlyList<Card> starterDeck,
            int seed = 0, int startingCredits = 0, string characterId = "default")
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            _starterDeck = new List<Card>(starterDeck ?? throw new ArgumentNullException(nameof(starterDeck)));

            if (startingCredits < 0)
                throw new ArgumentOutOfRangeException(nameof(startingCredits));

            Seed = seed;
            CharacterId = string.IsNullOrWhiteSpace(characterId) ? "default" : characterId;
            _startingCredits = startingCredits;
            Credits = startingCredits;

            _cards.AddRange(_starterDeck);
        }

        /// <summary>The nodes the player may travel to next.</summary>
        public IReadOnlyList<MapNode> AvailableNodes =>
            CurrentNode == null ? Map.StartNodes : Map.NextOf(CurrentNode);

        /// <summary>
        /// Moves onto a node. Rejects anything not connected to where the player
        /// stands, so no caller — UI included — can skip ahead on the map.
        /// </summary>
        public void MoveTo(MapNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            bool reachable = false;
            foreach (MapNode candidate in AvailableNodes)
            {
                if (candidate == node)
                {
                    reachable = true;
                    break;
                }
            }

            if (!reachable)
                throw new InvalidOperationException($"Node {node.Id} is not reachable from here.");

            CurrentNode = node;
            _visited.Add(node.Id);
        }

        /// <summary>True once the player has stood on this node.</summary>
        public bool HasVisited(int nodeId) => _visited.Contains(nodeId);

        public void AddCard(Card card)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));
            _cards.Add(card);
        }

        public void AddCredits(int amount)
        {
            if (amount <= 0)
                return;
            Credits += amount;
        }

        /// <summary>Returns false (and spends nothing) when the player cannot afford it.</summary>
        public bool TrySpendCredits(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (Credits < amount)
                return false;

            Credits -= amount;
            return true;
        }

        public void Finish(bool playerWon)
        {
            IsFinished = true;
            PlayerWon = playerWon;
        }

        /// <summary>Wipes run progress: back to the foot of the map, full health, starting deck.</summary>
        public void Reset()
        {
            CurrentNode = null;
            IsFinished = false;
            PlayerWon = false;
            Credits = _startingCredits;
            _visited.Clear();

            Player.RestoreToFull();
            Player.ResetBlock();
            _cards.Clear();
            _cards.AddRange(_starterDeck);
        }
    }
}
