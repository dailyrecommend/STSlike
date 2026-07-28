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

        public int TotalEncounters { get; }
        public int CurrentEncounter { get; private set; }
        public bool IsFinished { get; private set; }
        public bool PlayerWon { get; private set; }

        public bool IsFinalEncounter => CurrentEncounter >= TotalEncounters;

        public RunContext(int totalEncounters, PlayerState player, IReadOnlyList<Card> starterDeck)
        {
            if (totalEncounters <= 0)
                throw new ArgumentOutOfRangeException(nameof(totalEncounters), "A run needs at least one encounter.");

            TotalEncounters = totalEncounters;
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

            Player.RestoreToFull();
            Player.ResetBlock();
            _cards.Clear();
            _cards.AddRange(_starterDeck);
        }
    }
}
