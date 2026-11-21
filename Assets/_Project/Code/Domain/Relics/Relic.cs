using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Events;

namespace STSlike.Domain.Relics
{
    /// <summary>What a relic is given when it acts.</summary>
    public sealed class RelicContext
    {
        public PlayerState Player { get; }

        /// <summary>Optional, so a relic can be run in a test without one.</summary>
        public IEventBus Bus { get; }

        public RelicContext(PlayerState player, IEventBus bus = null)
        {
            Player = player;
            Bus = bus;
        }
    }

    /// <summary>
    /// Something carried for the whole run that quietly changes the rules.
    ///
    /// A class with its own state rather than a shared instance, which is the
    /// real difference from a condition: a condition is a fact about a figure
    /// and the figure counts the stacks, but a relic that waits for the tenth
    /// attack has to do its own counting. One object per run, per relic.
    ///
    /// The hooks come in two kinds. Some are reactions — a fight begins, a card
    /// is played — and could as easily be bus subscriptions; they are methods
    /// here so a relic is one readable object rather than a constructor full of
    /// subscriptions to unpick later. The rest intercept a calculation partway
    /// through, which an event cannot do.
    ///
    /// Everything is a no-op by default, so a relic overrides the one or two
    /// things it actually cares about.
    /// </summary>
    public abstract class Relic
    {
        /// <summary>Stable key, for saves and for looking up its art.</summary>
        public abstract string Id { get; }

        // ---- reactions ----

        /// <summary>A fight has been set up and is about to begin.</summary>
        public virtual void OnCombatStarted(RelicContext context) { }

        /// <summary>The player's turn has begun and the hand has been dealt.</summary>
        public virtual void OnTurnStarted(RelicContext context) { }

        /// <summary>A card has been paid for, before its effects run.</summary>
        public virtual void OnCardPlayed(Card card, RelicContext context) { }

        /// <summary>That card has finished and left the hand.</summary>
        public virtual void OnCardResolved(Card card, RelicContext context) { }

        /// <summary>The fight is over, whichever way it went.</summary>
        public virtual void OnCombatEnded(RelicContext context) { }

        // ---- interception ----

        /// <summary>Damage the player is about to deal.</summary>
        public virtual int ModifyOutgoingDamage(int amount, string tag) => amount;

        /// <summary>Damage the player is about to take.</summary>
        public virtual int ModifyIncomingDamage(int amount, string tag) => amount;
    }
}
