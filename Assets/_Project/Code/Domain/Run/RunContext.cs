using System;
using System.Collections.Generic;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;

namespace STSlike.Domain.Run
{
    /// <summary>
    /// Everything that persists for the length of a run: the player (so health
    /// carries between encounters, which is what makes block and healing
    /// matter), the deck the player has collected, and progress through the
    /// encounters.
    /// </summary>
    public sealed class RunContext
    {
        private readonly List<Card> _starterDeck;
        private readonly List<Card> _cards = new List<Card>();

        /// <summary>Lives for the whole run; combats reuse this instance.</summary>
        public PlayerState Player { get; }

        /// <summary>The run's deck. Rewards add to this between encounters.</summary>
        public IReadOnlyList<Card> Cards => _cards;

        /// <summary>The run's RNG seed, stored with its history so it can be replayed.</summary>
        public int Seed { get; }

        /// <summary>Which character is being played; drives the portrait and starting deck.</summary>
        public string CharacterId { get; }

        /// <summary>Run currency, spent at shops and awarded for clearing encounters.</summary>
        public int Credits { get; private set; }

        public int TotalEncounters { get; }
        public int CurrentEncounter { get; private set; }
        public bool IsFinished { get; private set; }
        public bool PlayerWon { get; private set; }

        public bool IsFinalEncounter => CurrentEncounter >= TotalEncounters;

        private readonly int _startingCredits;

        public RunContext(int totalEncounters, PlayerState player, IReadOnlyList<Card> starterDeck,
            int seed = 0, int startingCredits = 0, string characterId = "default")
        {
            if (totalEncounters <= 0)
                throw new ArgumentOutOfRangeException(nameof(totalEncounters), "A run needs at least one encounter.");
            if (startingCredits < 0)
                throw new ArgumentOutOfRangeException(nameof(startingCredits));

            TotalEncounters = totalEncounters;
            Seed = seed;
            CharacterId = string.IsNullOrWhiteSpace(characterId) ? "default" : characterId;
            _startingCredits = startingCredits;
            Credits = startingCredits;
            Player = player ?? throw new ArgumentNullException(nameof(player));
            _starterDeck = new List<Card>(starterDeck ?? throw new ArgumentNullException(nameof(starterDeck)));

            CurrentEncounter = 1;
            _cards.AddRange(_starterDeck);
        }

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

        public void AdvanceEncounter()
        {
            if (IsFinalEncounter)
                throw new InvalidOperationException("Already at the final encounter.");
            CurrentEncounter++;
        }

        public void Finish(bool playerWon)
        {
            IsFinished = true;
            PlayerWon = playerWon;
        }

        /// <summary>Wipes run progress: full health and the starting deck again.</summary>
        public void Reset()
        {
            CurrentEncounter = 1;
            IsFinished = false;
            PlayerWon = false;
            Credits = _startingCredits;

            Player.RestoreToFull();
            Player.ResetBlock();
            _cards.Clear();
            _cards.AddRange(_starterDeck);
        }
    }
}
