using System;
using STSlike.Domain.Actors;
using STSlike.Domain.Combat;
using STSlike.Domain.Statuses;

namespace STSlike.Domain.Effects
{
    /// <summary>
    /// Fires rounds from the weapon at the target.
    ///
    /// The damage is the weapon's, not the card's — every shot lands for the
    /// same amount, and a card distinguishes itself by how many rounds it sends
    /// and what else it does. That is why there is no damage field here.
    ///
    /// One round per shot. Running dry does not stop the card being played: it
    /// was paid for and it leaves the hand either way, the hammer simply falls
    /// on nothing. That is the cost of firing without watching the cylinder.
    /// </summary>
    [Serializable]
    public sealed class ShotEffect : ICardEffect, IDescribableEffect
    {
        public int shots = 1;

        /// <summary>Rounds fired — what the card's text is about.</summary>
        public int DisplayValue => shots;

        public ShotEffect() { }

        public ShotEffect(int shots)
        {
            this.shots = shots;
        }

        public void Execute(EffectContext context)
        {
            PlayerState player = context.Player;
            Combatant target = context.Target;

            if (player == null || target == null || shots <= 0)
                return;

            for (int i = 0; i < shots; i++)
            {
                // Nothing left to shoot at. Stopping rather than emptying the
                // cylinder into a corpse — the rounds are worth more than the
                // overkill.
                if (target.IsDead)
                    return;

                if (!player.TrySpendAmmo())
                {
                    // Once dry, the rest of the burst is dry too.
                    context.Bus?.Publish(new ShotFizzledEvent(player));
                    return;
                }

                context.Bus?.Publish(new AmmoChangedEvent(player, player.Ammo));
                Fire(context, player, target);
            }
        }

        private static void Fire(EffectContext context, PlayerState player, Combatant target)
        {
            // What the wound has already collected is carried by this round.
            // Read before it is fed, so the first shot into a clean target is
            // just the weapon — there is nothing to build on yet.
            int bonus = target.Statuses.StacksOf(HoneycombStatus.StatusId);

            // Folded in before anything modifies the shot, so a firing stance
            // amplifies the mark along with the round. Adding it afterwards
            // would leave the whole chain immune to the character's own
            // multipliers, which is backwards.
            int landing = DamageCalculation.Resolve(player, target, player.ShotDamage + bonus,
                DamageTags.Shot);

            int lost = target.TakeDamage(landing);
            context.Bus?.Publish(new DamageDealtEvent(player, target, landing, lost));

            // Then the round leaves its own mark, sized by what it was worth —
            // so opening the target up first makes the whole chain grow faster.
            int added = HoneycombStatus.StacksFrom(landing);
            if (added <= 0)
                return;

            target.Statuses.Apply(HoneycombStatus.Instance, added);
            context.Bus?.Publish(new StatusChangedEvent(target, HoneycombStatus.StatusId));
        }
    }
}
