using STSlike.Domain.Actors;
using STSlike.Domain.Cards;

namespace STSlike.Domain.Combat
{
    /// <summary>Combat is set up and about to begin.</summary>
    public readonly struct CombatStartedEvent { }

    /// <summary>A new player turn has begun (energy refilled, hand drawn).</summary>
    public readonly struct PlayerTurnStartedEvent { }

    /// <summary>The player has ended their turn (before the enemy acts).</summary>
    public readonly struct PlayerTurnEndedEvent { }

    /// <summary>A card has started being played (before its effects resolve).</summary>
    public readonly struct CardPlayedEvent
    {
        public readonly Card Card;
        public CardPlayedEvent(Card card) => Card = card;
    }

    /// <summary>
    /// A card has fully resolved and left the hand for the discard pile.
    ///
    /// Separate from <see cref="CardPlayedEvent"/>, which fires before the
    /// effects run: anything that reads the hand — the hand UI above all — must
    /// wait for this one, or it will still see the card that was just played.
    /// </summary>
    public readonly struct CardResolvedEvent
    {
        public readonly Card Card;
        public CardResolvedEvent(Card card) => Card = card;
    }

    /// <summary>Combat has ended.</summary>
    public readonly struct CombatEndedEvent
    {
        public readonly bool PlayerWon;
        public CombatEndedEvent(bool playerWon) => PlayerWon = playerWon;
    }

    /// <summary>Damage was applied to a combatant (raised by the effect that dealt it).</summary>
    public readonly struct DamageDealtEvent
    {
        public readonly Combatant Target;
        public readonly int Amount;
        public DamageDealtEvent(Combatant target, int amount)
        {
            Target = target;
            Amount = amount;
        }
    }

    /// <summary>Block was granted to a combatant.</summary>
    public readonly struct BlockGainedEvent
    {
        public readonly Combatant Actor;
        public readonly int Amount;
        public BlockGainedEvent(Combatant actor, int amount)
        {
            Actor = actor;
            Amount = amount;
        }
    }
}
