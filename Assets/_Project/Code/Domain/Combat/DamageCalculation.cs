using STSlike.Domain.Actors;

namespace STSlike.Domain.Combat
{
    /// <summary>
    /// What a blow actually lands for, once both sides have had their say.
    ///
    /// Deliberately its own thing rather than a step inside the damage effect:
    /// the number on a card and the number in an enemy's intent have to be the
    /// number that will really happen, so the screen asks the same question the
    /// rules will answer later. Two copies of this arithmetic would drift, and
    /// the symptom would be a card promising 2 and dealing 1.
    ///
    /// Block is not part of it — that is spent when the blow arrives, and an
    /// intent showing damage already absorbed would be lying about the threat.
    /// </summary>
    public static class DamageCalculation
    {
        public static int Resolve(Combatant source, Combatant target, int amount,
            string tag = DamageTags.None)
        {
            if (amount <= 0)
                return amount;

            // The attacker's conditions shape what is thrown, the defender's
            // what arrives, and each step rounds down on its own. The tag rides
            // along so a condition can decline to touch this kind of blow.
            int thrown = source != null ? source.Statuses.ModifyOutgoingDamage(amount, tag) : amount;
            return target != null ? target.Statuses.ModifyIncomingDamage(thrown, tag) : thrown;
        }
    }
}
