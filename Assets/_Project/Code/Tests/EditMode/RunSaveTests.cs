using System;
using System.Collections.Generic;
using NUnit.Framework;
using STSlike.Domain.Progression;
using STSlike.Infrastructure.Persistence;

namespace STSlike.Tests.EditMode
{
    public class RunSaveTests
    {
        private static RunSaveData MakeSave(
            int encounter = 2, int hp = 45, int rngCalls = 17, params string[] deck)
        {
            return new RunSaveData(
                characterId: "albino",
                seed: 4242,
                rngCallCount: rngCalls,
                currentEncounter: encounter,
                totalEncounters: 3,
                playerHp: hp,
                playerMaxHp: 70,
                stage: RunSaveStage.Combat,
                deckCardIds: (deck.Length > 0 ? deck : new[] { "strike", "strike", "defend" }),
                savedAtUtc: new DateTime(2026, 7, 30, 10, 0, 0, DateTimeKind.Utc));
        }

        [Test]
        public void NoSave_Initially()
        {
            var repo = new InMemoryRunSaveRepository();

            Assert.IsFalse(repo.HasSave);
            Assert.IsNull(repo.Load());
        }

        [Test]
        public void SaveThenLoad_RoundTripsEveryField()
        {
            var repo = new InMemoryRunSaveRepository();
            RunSaveData saved = MakeSave();

            repo.Save(saved);
            RunSaveData loaded = repo.Load();

            Assert.IsTrue(repo.HasSave);
            Assert.AreEqual("albino", loaded.CharacterId);
            Assert.AreEqual(4242, loaded.Seed);
            Assert.AreEqual(17, loaded.RngCallCount);   // the resume-accuracy field
            Assert.AreEqual(2, loaded.CurrentEncounter);
            Assert.AreEqual(45, loaded.PlayerHp);
            Assert.AreEqual(RunSaveStage.Combat, loaded.Stage);
            CollectionAssert.AreEqual(new[] { "strike", "strike", "defend" }, loaded.DeckCardIds);
        }

        [Test]
        public void Save_OverwritesTheSingleSlot()
        {
            var repo = new InMemoryRunSaveRepository();

            repo.Save(MakeSave(encounter: 1));
            repo.Save(MakeSave(encounter: 3));

            Assert.AreEqual(3, repo.Load().CurrentEncounter);
        }

        [Test]
        public void Clear_RemovesTheSave()
        {
            var repo = new InMemoryRunSaveRepository();
            repo.Save(MakeSave());

            repo.Clear();

            Assert.IsFalse(repo.HasSave);
            Assert.IsNull(repo.Load());
        }

        [Test]
        public void DuplicateCards_ArePreservedInOrder()
        {
            var repo = new InMemoryRunSaveRepository();
            repo.Save(MakeSave(deck: new[] { "a", "b", "a", "a" }));

            CollectionAssert.AreEqual(new[] { "a", "b", "a", "a" }, repo.Load().DeckCardIds);
        }

        // A run that ended is not a run you can return to.
        [Test]
        public void DeadPlayer_CannotBeSaved()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunSaveData(
                "albino", 1, 0, 1, 3, playerHp: 0, playerMaxHp: 70,
                stage: RunSaveStage.Combat, deckCardIds: new List<string>(),
                savedAtUtc: DateTime.UtcNow));
        }

        [Test]
        public void MissingCharacterId_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new RunSaveData(
                " ", 1, 0, 1, 3, 70, 70, RunSaveStage.Combat,
                new List<string>(), DateTime.UtcNow));
        }
    }
}
