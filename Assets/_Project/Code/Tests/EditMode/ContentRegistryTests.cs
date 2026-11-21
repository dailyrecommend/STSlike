using System.Collections.Generic;
using NUnit.Framework;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Characters;
using STSlike.Domain.Content;
using STSlike.Domain.Relics;
using STSlike.Domain.Effects;

namespace STSlike.Tests.EditMode
{
    /// <summary>
    /// The registry is what makes content addressable by id, so these cover the
    /// merge rules a second source will depend on rather than just the happy
    /// lookup.
    /// </summary>
    public sealed class ContentRegistryTests
    {
        private sealed class FakeSource : IContentSource
        {
            public string Name { get; set; } = "fake";
            public IEnumerable<ICardDefinition> Cards { get; set; } = new List<ICardDefinition>();
            public IEnumerable<IEnemyDefinition> Enemies { get; set; } = new List<IEnemyDefinition>();
            public IEnumerable<ICharacterDefinition> Characters { get; set; } = new List<ICharacterDefinition>();
            public IEnumerable<IEncounterDefinition> Encounters { get; set; } = new List<IEncounterDefinition>();
            public IEnumerable<IRelicDefinition> Relics { get; set; } = new List<IRelicDefinition>();
        }

        private sealed class FakeEncounter : IEncounterDefinition
        {
            public string Id { get; set; }
            public EnemyTier Tier { get; set; } = EnemyTier.Normal;
            public IReadOnlyList<string> EnemyIds { get; set; } = new List<string>();
            public int MinFloor { get; set; } = 1;
            public int MaxFloor { get; set; }
        }

        private static CardDefinition Card(string id, int cost = 1) =>
            new CardDefinition(id, cost, new List<ICardEffect> { new DealDamageEffect(6) });

        [Test]
        public void LooksContentUpById()
        {
            var registry = new ContentRegistry(new[]
            {
                new FakeSource { Cards = new List<ICardDefinition> { Card("strike"), Card("defend") } },
            });

            Assert.AreEqual("strike", registry.Card("strike").Id);
            Assert.AreEqual(2, registry.Cards.Count);
        }

        [Test]
        public void UnknownIdIsNullRatherThanAThrow()
        {
            var registry = new ContentRegistry(new List<IContentSource>());

            // A save naming content that is no longer installed must be
            // survivable, not fatal.
            Assert.IsNull(registry.Card("missing"));
            Assert.IsNull(registry.Enemy("missing"));
            Assert.IsNull(registry.Character(null));
        }

        [Test]
        public void LaterSourceReplacesEarlierOne()
        {
            var builtIn = new FakeSource
            {
                Name = "built-in",
                Cards = new List<ICardDefinition> { Card("strike", cost: 1) },
            };
            var mod = new FakeSource
            {
                Name = "mod",
                Cards = new List<ICardDefinition> { Card("strike", cost: 3) },
            };

            var registry = new ContentRegistry(new[] { builtIn, mod });

            // Patching, not just adding: this is the whole point of the ordering.
            Assert.AreEqual(3, registry.Card("strike").Cost);
            Assert.AreEqual(1, registry.Cards.Count, "an override must not also add a second entry");
        }

        [Test]
        public void OverridesAreRecordedForDiagnosis()
        {
            var registry = new ContentRegistry(new[]
            {
                new FakeSource { Name = "a", Cards = new List<ICardDefinition> { Card("strike") } },
                new FakeSource { Name = "b", Cards = new List<ICardDefinition> { Card("strike") } },
            });

            Assert.AreEqual(1, registry.Overrides.Count);
            StringAssert.Contains("strike", registry.Overrides[0]);
        }

        [Test]
        public void ContentWithNoIdIsSkippedRatherThanBreakingTheLoad()
        {
            var registry = new ContentRegistry(new[]
            {
                new FakeSource
                {
                    Cards = new List<ICardDefinition> { null, Card("strike") },
                },
            });

            // One unfinished asset should not take the rest of the game with it.
            Assert.AreEqual(1, registry.Cards.Count);
            Assert.IsNotNull(registry.Card("strike"));
        }

        [Test]
        public void RoomsAreCataloguedLikeEverythingElse()
        {
            var registry = new ContentRegistry(new[]
            {
                new FakeSource
                {
                    Encounters = new List<IEncounterDefinition>
                    {
                        new FakeEncounter
                        {
                            Id = "two-slimes",
                            EnemyIds = new List<string> { "slime", "slime" },
                        },
                    },
                },
            });

            Assert.AreEqual(1, registry.Encounters.Count);
            Assert.AreEqual(2, registry.Encounter("two-slimes").EnemyIds.Count,
                "a room naming the same enemy twice is two enemies, not one");
        }

        [Test]
        public void SourcesAreCombinedAcrossKinds()
        {
            var registry = new ContentRegistry(new[]
            {
                new FakeSource { Cards = new List<ICardDefinition> { Card("strike") } },
                new FakeSource { Cards = new List<ICardDefinition> { Card("bash") } },
            });

            Assert.AreEqual(2, registry.Cards.Count);
            Assert.IsNotNull(registry.Card("bash"));
            Assert.IsNotNull(registry.Card("strike"));
        }
    }
}
