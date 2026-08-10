using System;
using STSlike.Domain.Actors;
using STSlike.Domain.Effects;
using STSlike.Domain.Combat;

namespace STSlike.Domain.Characters.Albino
{
    /// <summary>
    /// Puts rounds back in the cylinder.
    ///
    /// A count rather than "fill it", so a card can offer a partial reload and
    /// the choice between topping up now or emptying the gun first stays a real
    /// one. Never goes past capacity.
    /// </summary>
    [Serializable]
    public sealed class ReloadEffect : ICardEffect, IDescribableEffect
    {
        public int rounds = 6;

        public int DisplayValue => rounds;

        public ReloadEffect() { }

        public ReloadEffect(int rounds)
        {
            this.rounds = rounds;
        }

        public void Execute(EffectContext context)
        {
            PlayerState player = context.Player;
            if (player?.Weapon == null || rounds <= 0)
                return;

            WeaponState weapon = player.Weapon;
            int before = weapon.Ammo;
            weapon.Reload(rounds);

            // Silent when the cylinder was already full: nothing moved, so
            // there is nothing for a view to react to.
            if (weapon.Ammo != before)
                context.Bus?.Publish(new AmmoChangedEvent(player, weapon.Ammo));
        }
    }
}
