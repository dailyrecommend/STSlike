using System.Collections.Generic;
using NUnit.Framework;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Combat;
using STSlike.Domain.Effects;
using STSlike.Domain.Events;
using STSlike.Domain.Randomness;
using STSlike.Domain.Statuses;

namespace STSlike.Tests.EditMode
{
    /// <summary>
    /// Whether a card may be played, which is now one question with one answer.
    ///
    /// It used to be asked twice — once by the hand to decide what to grey out,
    /// once by the manager on click — and the two only agreed because they were
    /// both checking energy. Once a stance can forbid a card, two copies would
    /// mean a card that looks playable and silently refuses.
    /// </summary>
    public sealed class PlayRulesTests
    {
        /// <summary>Forbids everything without the tag it is looking for.</summary>
        private sealed class OnlyAllowsTag : IStatusEffect
        {
            private readonly string _required;

            public OnlyAllowsTag(string required) => _required = required;

            public string Id => "test-stance";

            // A stance stays until something turns it off, which is what the
            // real one will be.
            public StatusDuration Duration => StatusDuration.Persistent;

            public int ModifyOutgoingDamage(int amount, int stacks, string tag) => amount;
            public int ModifyIncomingDamage(int amount, int stacks, string tag) => amount;

            public bool AllowsPlaying(ICardDefinition card, int stacks)
            {
                foreach (string tag in card.Tags)
                {
                    if (tag == _required)
                        return true;
                }
                return false;
            }
        }

        private static Card Card(string id, int cost = 1, params string[] tags) =>
            new Card(new CardDefinition(id, cost, new List<ICardEffect> { new DealDamageEffect(5) },
                tags: tags));

        private static CombatManager Fight(out PlayerState player, out EnemyState enemy,
            params Card[] deck)
        {
            player = new PlayerState("Albino", 70, 3, new Deck(new SystemRng(1)));
            enemy = new EnemyState("Grunt", 30, 5);

            var manager = new CombatManager(
                new CombatContext(player, new List<EnemyState> { enemy }),
                new EventBus(), cardsPerTurn: deck.Length);

            manager.StartCombat(new List<Card>(deck));
            return manager;
        }

        [Test]
        public void ACardInHandYouCanAffordIsPlayable()
        {
            CombatManager manager = Fight(out PlayerState player, out EnemyState _, Card("strike"));

            Assert.IsTrue(manager.CanPlay(player.Deck.Hand.Cards[0]));
        }

        [Test]
        public void AnUnaffordableCardIsNot()
        {
            CombatManager manager = Fight(out PlayerState player, out EnemyState _,
                Card("heavy", cost: 9));

            Assert.IsFalse(manager.CanPlay(player.Deck.Hand.Cards[0]));
        }

        [Test]
        public void ACardNotInHandIsNot()
        {
            CombatManager manager = Fight(out PlayerState _, out EnemyState __, Card("strike"));

            // A view a frame out of date can ask about a card already played.
            Assert.IsFalse(manager.CanPlay(Card("elsewhere")));
        }

        [Test]
        public void AConditionCanForbidACard()
        {
            CombatManager manager = Fight(out PlayerState player, out EnemyState _,
                Card("shot", 1, CardTags.Shot), Card("guard"));

            player.Statuses.Apply(new OnlyAllowsTag(CardTags.Shot), 1);

            // Found by tag rather than by hand position — the shuffle decides
            // the order, and it must not decide whether this test passes.
            Card shot = null;
            Card guard = null;
            foreach (Card card in player.Deck.Hand.Cards)
            {
                if (card.HasTag(CardTags.Shot))
                    shot = card;
                else
                    guard = card;
            }

            Assert.IsTrue(manager.CanPlay(shot));
            Assert.IsFalse(manager.CanPlay(guard), "the stance permits only what it is looking for");
        }

        [Test]
        public void AForbiddenCardIsAlsoRefusedOnClick()
        {
            CombatManager manager = Fight(out PlayerState player, out EnemyState enemy,
                Card("guard"));

            player.Statuses.Apply(new OnlyAllowsTag(CardTags.Shot), 1);
            Card guard = player.Deck.Hand.Cards[0];
            int energyBefore = player.Energy;

            // The face and the click agree because they ask the same method.
            Assert.IsFalse(manager.PlayCard(guard, enemy));
            Assert.AreEqual(energyBefore, player.Energy, "a refused card must not be charged for");
            Assert.AreEqual(1, player.Deck.Hand.Cards.Count, "nor leave the hand");
        }

        [Test]
        public void NothingIsPlayableOutsideThePlayersTurn()
        {
            CombatManager manager = Fight(out PlayerState player, out EnemyState _, Card("strike"));
            Card card = player.Deck.Hand.Cards[0];

            manager.ForceResolve(playerWon: true);

            Assert.IsFalse(manager.CanPlay(card));
        }

        // ---- tags ----

        [Test]
        public void ACardKnowsItsOwnKeywords()
        {
            Card shot = Card("fire", 1, CardTags.Shot);

            Assert.IsTrue(shot.HasTag(CardTags.Shot));
            Assert.IsFalse(shot.HasTag("Melee"));
            Assert.IsFalse(Card("plain").HasTag(CardTags.Shot));
        }

        [Test]
        public void AnUntaggedCardHasAnEmptyListRatherThanNull()
        {
            Assert.IsNotNull(Card("plain").Definition.Tags);
        }
    }
}
