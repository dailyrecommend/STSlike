using System;
using STSlike.Domain.Cards;
using STSlike.Domain.Relics;

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
        /// The weapon being carried, or null for a character who carries none.
        ///
        /// Unlike energy its ammunition does not come back at the start of a
        /// turn — running dry and having to reload is the cost a weapon like
        /// that is built around.
        /// </summary>
        public WeaponState Weapon { get; }

        public bool IsArmed => Weapon != null;

        /// <summary>
        /// What is being carried this run. On the player rather than on the run
        /// because relics reach into damage, and the damage calculation only
        /// ever sees combatants.
        /// </summary>
        public RelicCollection Relics { get; } = new RelicCollection();

        public PlayerState(string name, int maxHp, int maxEnergy, Deck deck,
            WeaponState weapon = null)
            : base(name, maxHp)
        {
            if (maxEnergy < 0)
                throw new ArgumentOutOfRangeException(nameof(maxEnergy), "Max energy cannot be negative.");

            MaxEnergy = maxEnergy;
            Weapon = weapon;
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
            Energy = 0;
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
