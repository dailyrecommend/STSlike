using System;
using STSlike.Domain.Actors;
using STSlike.Domain.Combat;
using STSlike.Domain.Effects;
using STSlike.Domain.Statuses;

namespace STSlike.Domain.Characters.Albino
{
    /// <summary>Which way a stance card moves the figure.</summary>
    public enum FocusChange
    {
        /// <summary>In if out, out if in. One card as the switch.</summary>
        Toggle = 0,

        /// <summary>Only enters. Does nothing if already held.</summary>
        Enter = 1,

        /// <summary>Only leaves. Does nothing if not held.</summary>
        Exit = 2,
    }

    /// <summary>
    /// Enters the firing stance, or leaves it.
    ///
    /// One dropdown rather than a pair of switches: a card is a toggle, a way
    /// in, or a way out, and no fourth thing exists. Two booleans could express
    /// combinations that mean nothing.
    ///
    /// Whichever way it goes, the card carrying it needs the Stance tag — the
    /// stance forbids everything else, including, without that tag, the very
    /// card that would drop it.
    /// </summary>
    [Serializable]
    public sealed class ToggleFocusEffect : ICardEffect, IDescribableEffect
    {
        public FocusChange change = FocusChange.Toggle;

        /// <summary>Nothing numeric to print; the card's text speaks for itself.</summary>
        public int DisplayValue => 0;

        public void Execute(EffectContext context)
        {
            PlayerState player = context.Player;
            if (player == null)
                return;

            bool held = player.Statuses.Has(FocusStatus.StatusId);
            bool wantsIn = change switch
            {
                FocusChange.Enter => true,
                FocusChange.Exit => false,
                _ => !held,
            };

            if (wantsIn == held)
                return;

            if (wantsIn)
                player.Statuses.Apply(FocusStatus.Instance, 1);
            else
                player.Statuses.Remove(FocusStatus.StatusId);

            context.Bus?.Publish(new StatusChangedEvent(player, FocusStatus.StatusId));
        }
    }
}
