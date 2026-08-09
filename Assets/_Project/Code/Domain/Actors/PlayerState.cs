using System;
using STSlike.Domain.Cards;

namespace STSlike.Domain.Actors
{
    /// <summary>
    /// The player during combat: a <see cref="Combatant"/> plus the energy
    /// resource and a <see cref="Deck"/>. The deck is injected so the run's
    /// seeded RNG is decided at the composition root.
    /// </summary>
    public sealed class PlayerState : Combatant
    {
        public int MaxEnergy { get; }
        public int Energy { get; private set; }
        public Deck Deck { get; }

        /// <summary>
        /// Rounds in the cylinder. Unlike energy this does not come back at the
        /// start of a turn — running dry and having to reload is the cost the
        /// whole weapon is built around.
        ///
        /// Zero max for a character who carries no such weapon.
        /// </summary>
        public int MaxAmmo { get; }
        public int Ammo { get; private set; }

        /// <summary>
        /// What one round does before anything modifies it. A property of the
        /// weapon rather than of the card that pulls the trigger, which is what
        /// makes every shot land for the same amount.
        /// </summary>
        public int ShotDamage { get; }

        public PlayerState(string name, int maxHp, int maxEnergy, Deck deck,
            int maxAmmo = 0, int shotDamage = 0)
            : base(name, maxHp)
        {
            if (maxEnergy < 0)
                throw new ArgumentOutOfRangeException(nameof(maxEnergy), "Max energy cannot be negative.");
            if (maxAmmo < 0)
                throw new ArgumentOutOfRangeException(nameof(maxAmmo), "Max ammo cannot be negative.");

            MaxEnergy = maxEnergy;
            MaxAmmo = maxAmmo;
            ShotDamage = shotDamage;
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
            Energy = 0;
            Ammo = maxAmmo;
        }

        /// <summary>
        /// Puts rounds back in, never past the cylinder's capacity. A count
        /// rather than "fill it" so a card can offer a partial reload.
        /// </summary>
        public void Reload(int rounds)
        {
            if (rounds <= 0)
                return;

            Ammo = Math.Min(MaxAmmo, Ammo + rounds);
        }

        /// <summary>Fills the cylinder. Used when a fight begins.</summary>
        public void ReloadFully() => Ammo = MaxAmmo;

        /// <summary>
        /// Takes one round if there is one. False means the hammer fell on an
        /// empty chamber — the card was still played and still spent, it simply
        /// did nothing.
        /// </summary>
        public bool TrySpendAmmo()
        {
            if (Ammo <= 0)
                return false;

            Ammo--;
            return true;
        }

        /// <summary>Called at the start of the player's turn.</summary>
        public void RefillEnergy() => Energy = MaxEnergy;

        /// <summary>
        /// Grants energy outside the turn's refill.
        ///
        /// Allowed to go past <see cref="MaxEnergy"/> on purpose: the maximum is
        /// what a turn begins with, not a ceiling. Clamping here would make a
        /// card that hands energy back do nothing on a full bar, which is
        /// exactly the turn you would play it.
        /// </summary>
        public void GainEnergy(int amount)
        {
            if (amount <= 0)
                return;

            Energy += amount;
        }

        public bool CanAfford(int cost) => Energy >= cost;

        public void SpendEnergy(int cost)
        {
            if (cost < 0)
                throw new ArgumentOutOfRangeException(nameof(cost), "Cost cannot be negative.");
            if (!CanAfford(cost))
                throw new InvalidOperationException("Not enough energy.");

            Energy -= cost;
        }
    }
}
