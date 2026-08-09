using System;
using System.Collections.Generic;
using STSlike.Domain.Cards;

namespace STSlike.Domain.Statuses
{
    /// <summary>One condition a figure is currently under, and for how long.</summary>
    public readonly struct ActiveStatus
    {
        public readonly IStatusEffect Status;
        public readonly int Stacks;

        public string Id => Status.Id;

        public ActiveStatus(IStatusEffect status, int stacks)
        {
            Status = status;
            Stacks = stacks;
        }
    }

    /// <summary>
    /// What a combatant is currently suffering, and the single place a
    /// calculation asks about it.
    ///
    /// Every active condition gets a say, in the order they were applied. That
    /// is what keeps the damage formula from listing conditions by name — it
    /// hands the number round and takes back whatever comes out.
    /// </summary>
    public sealed class StatusTracker
    {
        private readonly List<ActiveStatus> _active = new List<ActiveStatus>();

        public IReadOnlyList<ActiveStatus> Active => _active;

        /// <summary>
        /// Applies a condition, or extends one already running. Extending rather
        /// than replacing is what makes applying Weak twice last twice as long
        /// instead of restarting the clock.
        /// </summary>
        public void Apply(IStatusEffect status, int stacks)
        {
            if (status == null)
                throw new ArgumentNullException(nameof(status));
            if (stacks <= 0)
                return;

            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Id != status.Id)
                    continue;

                _active[i] = new ActiveStatus(status, _active[i].Stacks + stacks);
                return;
            }

            _active.Add(new ActiveStatus(status, stacks));
        }

        public int StacksOf(string id)
        {
            foreach (ActiveStatus entry in _active)
            {
                if (entry.Id == id)
                    return entry.Stacks;
            }
            return 0;
        }

        public bool Has(string id) => StacksOf(id) > 0;

        public int ModifyOutgoingDamage(int amount, string tag)
        {
            foreach (ActiveStatus entry in _active)
                amount = entry.Status.ModifyOutgoingDamage(amount, entry.Stacks, tag);

            return amount;
        }

        public int ModifyIncomingDamage(int amount, string tag)
        {
            foreach (ActiveStatus entry in _active)
                amount = entry.Status.ModifyIncomingDamage(amount, entry.Stacks, tag);

            return amount;
        }

        /// <summary>
        /// Whether every active condition permits playing that card. One veto is
        /// enough, so a stance can narrow the hand without anything else knowing
        /// stances exist.
        /// </summary>
        public bool AllowsPlaying(ICardDefinition card)
        {
            foreach (ActiveStatus entry in _active)
            {
                if (!entry.Status.AllowsPlaying(card, entry.Stacks))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// This figure's own turn has ended, so its turn-counted conditions lose
        /// a step. Ones that reach zero are dropped rather than left sitting at
        /// zero, so the panel above the figure empties on its own.
        ///
        /// Marks tied to the player's turn and anything persistent are left
        /// alone — their clocks are not this one.
        /// </summary>
        public void EndOwnerTurn()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].Status.Duration != StatusDuration.OwnerTurns)
                    continue;

                int left = _active[i].Stacks - 1;
                if (left > 0)
                    _active[i] = new ActiveStatus(_active[i].Status, left);
                else
                    _active.RemoveAt(i);
            }
        }

        /// <summary>
        /// The player's turn has ended. Marks that only exist to be exploited
        /// within it are wiped outright — they are not counted down, because a
        /// half-built chain is worth nothing on the next turn anyway.
        ///
        /// Called on every figure, since these marks usually sit on the enemy.
        /// </summary>
        public bool EndPlayerTurn()
        {
            bool removed = false;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].Status.Duration != StatusDuration.PlayerTurn)
                    continue;

                _active.RemoveAt(i);
                removed = true;
            }

            return removed;
        }

        /// <summary>Conditions do not survive a fight.</summary>
        public void Clear() => _active.Clear();
    }
}
