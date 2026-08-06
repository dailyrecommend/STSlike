using System;
using System.Collections.Generic;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Health as the player is currently allowed to see it.
    ///
    /// The rules settle an action the instant it is played, so by the time the
    /// first swing animates, the real health is already final. A HUD reading it
    /// straight gives the whole result away at once and then has nothing left to
    /// say for the rest of the animation — which reads as sluggish however fast
    /// the motion actually is.
    ///
    /// So each blow's outcome is parked here as it is dealt and released as the
    /// stage plays that blow. What is parked is a snapshot, not a subtraction:
    /// block absorbs, health floors at zero, and an amount would drift from what
    /// really happened.
    /// </summary>
    public sealed class CombatDisplayState
    {
        public readonly struct Vitals
        {
            public readonly int Hp;
            public readonly int Block;

            public Vitals(int hp, int block)
            {
                Hp = hp;
                Block = block;
            }
        }

        private sealed class Ledger
        {
            public readonly Queue<Vitals> Owed = new Queue<Vitals>();
            public Vitals Shown;
        }

        private readonly Dictionary<int, Ledger> _ledgers = new Dictionary<int, Ledger>();

        /// <summary>A blow was released; anything drawing health should redraw.</summary>
        public event Action Changed;

        /// <summary>Holds a result back until its impact has been played.</summary>
        public void Defer(ActorRef actor, int hp, int block)
        {
            if (!actor.IsValid)
                return;

            For(actor).Owed.Enqueue(new Vitals(hp, block));
        }

        /// <summary>Releases the oldest result still owed for this figure.</summary>
        public void Reveal(ActorRef actor)
        {
            if (!actor.IsValid)
                return;

            Ledger ledger = For(actor);
            if (ledger.Owed.Count == 0)
                return;

            ledger.Shown = ledger.Owed.Dequeue();
            Changed?.Invoke();
        }

        /// <summary>
        /// What to draw for this figure.
        ///
        /// While nothing is owed the real values pass straight through, so heals,
        /// block and the start of a fight appear at once — and they are kept as
        /// the resting point the next flurry will be shown from.
        /// </summary>
        public Vitals Of(ActorRef actor, int realHp, int realBlock)
        {
            if (!actor.IsValid)
                return new Vitals(realHp, realBlock);

            Ledger ledger = For(actor);
            if (ledger.Owed.Count == 0)
                ledger.Shown = new Vitals(realHp, realBlock);

            return ledger.Shown;
        }

        /// <summary>Drops everything still owed. Used when a new fight begins.</summary>
        public void Clear()
        {
            if (_ledgers.Count == 0)
                return;

            _ledgers.Clear();
            Changed?.Invoke();
        }

        // The player and each enemy slot get their own ledger; a figure nobody
        // has hit yet simply has none.
        private Ledger For(ActorRef actor)
        {
            int key = actor.IsPlayer ? -1 : actor.EnemyIndex;
            if (!_ledgers.TryGetValue(key, out Ledger ledger))
            {
                ledger = new Ledger();
                _ledgers[key] = ledger;
            }
            return ledger;
        }
    }
}
