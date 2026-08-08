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

    /// <summary>
    /// An enemy is beginning its telegraphed move, before any of it resolves.
    ///
    /// Separate from the damage events because one action can land several
    /// blows: the motion belongs to the action and plays once, while the hits
    /// that follow are what the target reacts to.
    /// </summary>
    public readonly struct EnemyActionStartedEvent
    {
        public readonly Actors.EnemyState Enemy;
        public readonly Actors.EnemyIntent Intent;

        public EnemyActionStartedEvent(Actors.EnemyState enemy, Actors.EnemyIntent intent)
        {
            Enemy = enemy;
            Intent = intent;
        }
    }

    /// <summary>
    /// Damage was applied to a combatant (raised by the effect that dealt it).
    /// Carries who swung as well as who was hit, so the stage can play an attack
    /// on one actor and a flinch on the other.
    /// </summary>
    public readonly struct DamageDealtEvent
    {
        public readonly Combatant Source;
        public readonly Combatant Target;

        /// <summary>What was thrown, before block had its say.</summary>
        public readonly int Amount;

        /// <summary>
        /// What actually reached health. Zero when block ate the whole blow —
        /// which is the difference between a hit that hurt and one that did not,
        /// and the only way a view can tell them apart.
        /// </summary>
        public readonly int HpLost;

        public DamageDealtEvent(Combatant source, Combatant target, int amount, int hpLost = 0)
        {
            Source = source;
            Target = target;
            Amount = amount;
            HpLost = hpLost;
        }
    }

    /// <summary>
    /// A condition was applied to a combatant, or ran out. Carries the id
    /// rather than the status so a view can look up an icon without the rules
    /// knowing what an icon is.
    /// </summary>
    public readonly struct StatusChangedEvent
    {
        public readonly Combatant Actor;
        public readonly string StatusId;

        public StatusChangedEvent(Combatant actor, string statusId)
        {
            Actor = actor;
            StatusId = statusId;
        }
    }

    /// <summary>
    /// Energy was granted by an effect, not by the turn's refill. Separate from
    /// the refill so a view can react to the gain without flashing at the start
    /// of every turn.
    /// </summary>
    public readonly struct EnergyGainedEvent
    {
        public readonly Combatant Actor;
        public readonly int Amount;

        public EnergyGainedEvent(Combatant actor, int amount)
        {
            Actor = actor;
            Amount = amount;
        }
    }

    /// <summary>
    /// Cards moved into hand. The count is what was actually drawn, which can be
    /// short of what was asked for when both piles have run dry.
    /// </summary>
    public readonly struct CardsDrawnEvent
    {
        public readonly Combatant Actor;
        public readonly int Count;

        public CardsDrawnEvent(Combatant actor, int count)
        {
            Actor = actor;
            Count = count;
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
