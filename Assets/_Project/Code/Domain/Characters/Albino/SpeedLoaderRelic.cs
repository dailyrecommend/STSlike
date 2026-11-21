using STSlike.Domain.Combat;
using STSlike.Domain.Relics;

namespace STSlike.Domain.Characters.Albino
{
    /// <summary>
    /// Speed Loader: three rounds already chambered when a fight begins.
    ///
    /// Aimed squarely at the character's worst moment. A fight opens with an
    /// empty weapon, so the first turn is normally spent loading and taking a
    /// hit for it; three rounds means that turn can be spent shooting instead —
    /// or still loading, for a fuller cylinder sooner. It changes what the
    /// opening turn is *for* rather than simply adding damage.
    /// </summary>
    [System.Serializable]
    public sealed class SpeedLoaderRelic : Relic
    {
        public const string RelicId = "SpeedLoader";

        /// <summary>Tuned on the asset rather than fixed here.</summary>
        public int rounds = 3;

        public override string Id => RelicId;

        public override void OnCombatStarted(RelicContext context)
        {
            var weapon = context.Player?.Weapon;
            if (weapon == null || rounds <= 0)
                return;

            weapon.Reload(rounds);
            context.Bus?.Publish(new AmmoChangedEvent(context.Player, weapon.Ammo));
        }
    }
}
