using System;

namespace STSlike.Domain.Actors
{
    /// <summary>
    /// A weapon with a limited supply of shots, carried through a fight.
    ///
    /// Its own object rather than three more fields on the player: a character
    /// either carries one or does not, and null says that far more plainly than
    /// a capacity of zero. It also keeps the general idea of a player from
    /// growing a hole shaped like one particular character's revolver.
    ///
    /// What the shots *mean* — the tag they carry, the wound they leave, the
    /// stance that amplifies them — belongs to the character. This is only the
    /// magazine and the number on the box.
    /// </summary>
    public sealed class WeaponState
    {
        /// <summary>Rounds it holds when full.</summary>
        public int MaxAmmo { get; }

        public int Ammo { get; private set; }

        /// <summary>
        /// Base damage of one round, before anything modifies it. A property of
        /// the weapon rather than of the card that pulls the trigger, which is
        /// what makes every shot land for the same amount.
        /// </summary>
        public int ShotDamage { get; }

        public bool IsEmpty => Ammo <= 0;

        public WeaponState(int maxAmmo, int shotDamage)
        {
            if (maxAmmo <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxAmmo),
                    "A weapon with no capacity should be no weapon at all.");
            if (shotDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(shotDamage));

            MaxAmmo = maxAmmo;
            ShotDamage = shotDamage;
            Ammo = maxAmmo;
        }

        /// <summary>
        /// Puts rounds back, never past capacity. A count rather than "fill it",
        /// so a card can offer a partial reload.
        /// </summary>
        public void Reload(int rounds)
        {
            if (rounds <= 0)
                return;

            Ammo = Math.Min(MaxAmmo, Ammo + rounds);
        }

        /// <summary>Fills it. Used when a fight begins.</summary>
        public void ReloadFully() => Ammo = MaxAmmo;

        /// <summary>
        /// Takes one round if there is one. False means the hammer fell on an
        /// empty chamber — the card was still played and still spent, it simply
        /// did nothing.
        /// </summary>
        public bool TrySpend()
        {
            if (Ammo <= 0)
                return false;

            Ammo--;
            return true;
        }
    }
}
