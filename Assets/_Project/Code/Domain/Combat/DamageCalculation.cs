using STSlike.Domain.Actors;
using STSlike.Domain.Relics;

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
            thrown = Carried(source)?.ModifyOutgoingDamage(thrown, tag) ?? thrown;

            int landing = target != null ? target.Statuses.ModifyIncomingDamage(thrown, tag) : thrown;
            return Carried(target)?.ModifyIncomingDamage(landing, tag) ?? landing;
        }

        // Only the player carries anything. Asked here rather than hung off
        // every combatant so an enemy does not grow a pocket it never uses.
        private static RelicCollection Carried(Combatant combatant) =>
            (combatant as PlayerState)?.Relics;
    }
}
