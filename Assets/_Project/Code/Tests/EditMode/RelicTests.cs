using System.Collections.Generic;
using NUnit.Framework;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Characters.Albino;
using STSlike.Domain.Combat;
using STSlike.Domain.Effects;
using STSlike.Domain.Events;
using STSlike.Domain.Randomness;
using STSlike.Domain.Relics;

namespace STSlike.Tests.EditMode
{
    /// <summary>
    /// Things carried for the whole run that quietly change the rules.
    ///
    /// The two here are the two shapes a relic comes in: one reacts to something
    /// happening, the other intercepts a calculation and needs its own memory to
    /// know when to.
    /// </summary>
    public sealed class RelicTests
    {
        private static PlayerState Gunhand() =>
            new PlayerState("Albino", 70, 3, new Deck(new SystemRng(1)), new WeaponState(6, 6));

        private static Card Attack(string id = "bang") =>
            new Card(new CardDefinition(id, 1, new List<ICardEffect> { new DealDamageEffect(10) },
                CardType.Attack));

        private static Card Skill(string id = "guard") =>
            new Card(new CardDefinition(id, 1, new List<ICardEffect>(), CardType.Skill));

        private static CombatManager Fight(PlayerState player, out EnemyState enemy)
        {
            enemy = new EnemyState("Grunt", 400, 5);

            var manager = new CombatManager(
                new CombatContext(player, new List<EnemyState> { enemy }),
                new EventBus(), cardsPerTurn: 1);

            manager.StartCombat(new List<Card> { Attack("filler") });
            return manager;
        }

        // ---- carrying them ----

        [Test]
        public void TheSameRelicIsNotPickedUpTwice()
        {
            var relics = new RelicCollection();

            Assert.IsTrue(relics.Add(new SpeedLoaderRelic()));
            Assert.IsFalse(relics.Add(new SpeedLoaderRelic()));
            Assert.AreEqual(1, relics.Count);
        }

        // ---- the reacting kind ----

        [Test]
        public void SpeedLoaderChambersRoundsBeforeTheFightStarts()
        {
            PlayerState player = Gunhand();
            player.Relics.Add(new SpeedLoaderRelic());

            Fight(player, out EnemyState _);

            // A fight normally opens with an empty weapon; this is the relic
            // changing what the first turn is for.
            Assert.AreEqual(new SpeedLoaderRelic().rounds, player.Weapon.Ammo);
        }

        [Test]
        public void WithoutItTheFightStillOpensEmpty()
        {
            PlayerState player = Gunhand();

            Fight(player, out EnemyState _);

            Assert.AreEqual(0, player.Weapon.Ammo);
        }

        // ---- the intercepting kind ----

        [Test]
        public void OverchargeDoublesTheCardAfterTheTenthAttack()
        {
            PlayerState player = Gunhand();
            var relic = new OverchargeRelic();
            player.Relics.Add(relic);
            var context = new RelicContext(player);

            for (int i = 0; i < new OverchargeRelic().threshold; i++)
                relic.OnCardPlayed(Attack(), context);

            Assert.IsTrue(relic.IsArmed);

            // Not yet: arming happens on the tenth, the payoff is the eleventh.
            Assert.AreEqual(10, player.Relics.ModifyOutgoingDamage(10, DamageTags.None));

            relic.OnCardPlayed(Attack(), context);
            Assert.AreEqual(20, player.Relics.ModifyOutgoingDamage(10, DamageTags.None));
        }

        [Test]
        public void TheDoublingCoversEveryBlowOfOneCard()
        {
            PlayerState player = Gunhand();
            var relic = new OverchargeRelic();
            player.Relics.Add(relic);
            var context = new RelicContext(player);

            for (int i = 0; i < new OverchargeRelic().threshold; i++)
                relic.OnCardPlayed(Attack(), context);
            relic.OnCardPlayed(Attack(), context);

            // A burst is one card, so saving the charge for a full cylinder is
            // the right play rather than a trick.
            Assert.AreEqual(20, player.Relics.ModifyOutgoingDamage(10, DamageTags.None));
            Assert.AreEqual(20, player.Relics.ModifyOutgoingDamage(10, DamageTags.None));

            relic.OnCardResolved(Attack(), context);
            Assert.AreEqual(10, player.Relics.ModifyOutgoingDamage(10, DamageTags.None));
        }

        [Test]
        public void OnlyAttackCardsCountTowardsIt()
        {
            var relic = new OverchargeRelic();
            var context = new RelicContext(Gunhand());

            for (int i = 0; i < new OverchargeRelic().threshold; i++)
                relic.OnCardPlayed(Skill(), context);

            Assert.AreEqual(0, relic.Progress);
            Assert.IsFalse(relic.IsArmed);
        }

        [Test]
        public void ACashedChargeDoesNotAlsoCountTowardsTheNext()
        {
            var relic = new OverchargeRelic();
            var context = new RelicContext(Gunhand());

            for (int i = 0; i < new OverchargeRelic().threshold; i++)
                relic.OnCardPlayed(Attack(), context);
            relic.OnCardPlayed(Attack(), context);

            Assert.AreEqual(0, relic.Progress,
                "the card that spent the charge is not also the first of the next ten");
        }

        [Test]
        public void AChargeIsNotCarriedIntoTheNextFight()
        {
            PlayerState player = Gunhand();
            var relic = new OverchargeRelic();
            player.Relics.Add(relic);
            var context = new RelicContext(player);

            for (int i = 0; i < new OverchargeRelic().threshold; i++)
                relic.OnCardPlayed(Attack(), context);

            Fight(player, out EnemyState _);

            // Walking into a boss with a free doubled cylinder, decided several
            // rooms earlier, is not a decision anybody made in that fight.
            Assert.IsFalse(relic.IsArmed);
        }

        // ---- through the real pipeline ----

        [Test]
        public void ARelicReachesTheDamageThatIsActuallyDealt()
        {
            PlayerState player = Gunhand();
            var relic = new OverchargeRelic();
            player.Relics.Add(relic);
            var context = new RelicContext(player);

            var enemy = new EnemyState("Grunt", 400, 5);

            for (int i = 0; i < new OverchargeRelic().threshold; i++)
                relic.OnCardPlayed(Attack(), context);
            relic.OnCardPlayed(Attack(), context);

            new DealDamageEffect(10).Execute(new EffectContext(player, enemy));

            // Not a parallel path: the relic rides the same calculation the card
            // face and the enemy's intent read from.
            Assert.AreEqual(400 - 20, enemy.CurrentHp);
        }
    }
}
