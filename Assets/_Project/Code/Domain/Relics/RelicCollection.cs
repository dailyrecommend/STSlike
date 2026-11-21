using System.Collections.Generic;
using STSlike.Domain.Cards;

namespace STSlike.Domain.Relics
{
    /// <summary>
    /// What the player is carrying, and the one place the rules ask about it.
    ///
    /// Relics are consulted in the order they were picked up, which is the only
    /// order a player can reason about — an alphabetical or arbitrary one would
    /// make two relics that interact impossible to predict.
    /// </summary>
    public sealed class RelicCollection
    {
        private readonly List<Relic> _held = new List<Relic>();

        public IReadOnlyList<Relic> Held => _held;

        public int Count => _held.Count;

        /// <summary>
        /// Picks one up. A relic already carried is ignored rather than doubled:
        /// duplicates are a shop or reward mistake, and silently stacking one
        /// would be a strange way to find out.
        /// </summary>
        public bool Add(Relic relic)
        {
            if (relic == null || Has(relic.Id))
                return false;

            _held.Add(relic);
            return true;
        }

        public bool Has(string id)
        {
            foreach (Relic relic in _held)
            {
                if (relic.Id == id)
                    return true;
            }
            return false;
        }

        // ---- reactions ----

        public void CombatStarted(RelicContext context) => Each(r => r.OnCombatStarted(context));
        public void TurnStarted(RelicContext context) => Each(r => r.OnTurnStarted(context));
        public void CombatEnded(RelicContext context) => Each(r => r.OnCombatEnded(context));

        public void CardPlayed(Card card, RelicContext context) =>
            Each(r => r.OnCardPlayed(card, context));

        public void CardResolved(Card card, RelicContext context) =>
            Each(r => r.OnCardResolved(card, context));

        // ---- interception ----

        public int ModifyOutgoingDamage(int amount, string tag)
        {
            foreach (Relic relic in _held)
                amount = relic.ModifyOutgoingDamage(amount, tag);

            return amount;
        }

        public int ModifyIncomingDamage(int amount, string tag)
        {
            foreach (Relic relic in _held)
                amount = relic.ModifyIncomingDamage(amount, tag);

            return amount;
        }

        private void Each(System.Action<Relic> act)
        {
            foreach (Relic relic in _held)
                act(relic);
        }
    }
}
