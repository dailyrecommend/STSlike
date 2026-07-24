using STSlike.Domain.Cards;

namespace STSlike.Domain.Combat
{
    /// <summary>Combat is set up and about to begin.</summary>
    public readonly struct CombatStartedEvent { }

    /// <summary>A new player turn has begun (energy refilled, hand drawn).</summary>
    public readonly struct PlayerTurnStartedEvent { }

    /// <summary>The player has ended their turn (before the enemy acts).</summary>
    public readonly struct PlayerTurnEndedEvent { }

    /// <summary>A card has finished resolving.</summary>
    public readonly struct CardPlayedEvent
    {
        public readonly Card Card;
        public CardPlayedEvent(Card card) => Card = card;
    }

    /// <summary>Combat has ended.</summary>
    public readonly struct CombatEndedEvent
    {
        public readonly bool PlayerWon;
        public CombatEndedEvent(bool playerWon) => PlayerWon = playerWon;
    }
}
