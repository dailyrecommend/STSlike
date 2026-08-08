using NUnit.Framework;
using STSlike.Presentation.UI;

namespace STSlike.Tests.EditMode
{
    /// <summary>
    /// The ledger that keeps the screen from running ahead of the animation.
    ///
    /// This is the subtlest logic in the project and the only kind that fails
    /// silently: get it wrong and nothing throws, health just disagrees with
    /// what the player is watching. Worth pinning down properly.
    ///
    /// The rules settle a whole action at once, so a flurry arrives as: several
    /// results deferred back to back, then released one at a time as the stage
    /// plays each blow.
    /// </summary>
    public sealed class CombatDisplayStateTests
    {
        private static readonly ActorRef Enemy = ActorRef.Enemy(0);

        [Test]
        public void WithNothingOwedTheRealValuesPassStraightThrough()
        {
            var display = new CombatDisplayState();

            CombatDisplayState.Vitals shown = display.Of(Enemy, realHp: 40, realBlock: 5);

            Assert.AreEqual(40, shown.Hp);
            Assert.AreEqual(5, shown.Block);
        }

        [Test]
        public void DeferredDamageIsHeldBackUntilItIsRevealed()
        {
            var display = new CombatDisplayState();

            // The screen has been showing 40 for a while.
            display.Of(Enemy, 40, 0);

            // The rules apply a blow: health is really 34 now.
            display.Defer(Enemy, hp: 34, block: 0);

            Assert.AreEqual(40, display.Of(Enemy, 34, 0).Hp,
                "the blow has not been played yet, so it must not be visible");

            display.Reveal(Enemy);

            Assert.AreEqual(34, display.Of(Enemy, 34, 0).Hp);
        }

        [Test]
        public void AFlurryIsReleasedOneBlowAtATime()
        {
            var display = new CombatDisplayState();
            display.Of(Enemy, 40, 0);

            // Three hits, settled by the rules in one burst.
            display.Defer(Enemy, 34, 0);
            display.Defer(Enemy, 28, 0);
            display.Defer(Enemy, 22, 0);

            Assert.AreEqual(40, display.Of(Enemy, 22, 0).Hp);

            display.Reveal(Enemy);
            Assert.AreEqual(34, display.Of(Enemy, 22, 0).Hp);

            display.Reveal(Enemy);
            Assert.AreEqual(28, display.Of(Enemy, 22, 0).Hp);

            display.Reveal(Enemy);
            Assert.AreEqual(22, display.Of(Enemy, 22, 0).Hp);
        }

        [Test]
        public void SnapshotsCarryBlockSoAGuardDrainsInStepWithHealth()
        {
            var display = new CombatDisplayState();
            display.Of(ActorRef.Player, 70, 10);

            // Block absorbs the first blow entirely, then the second bites.
            display.Defer(ActorRef.Player, hp: 70, block: 4);
            display.Defer(ActorRef.Player, hp: 68, block: 0);

            Assert.AreEqual(10, display.Of(ActorRef.Player, 68, 0).Block);

            display.Reveal(ActorRef.Player);
            CombatDisplayState.Vitals afterFirst = display.Of(ActorRef.Player, 68, 0);
            Assert.AreEqual(70, afterFirst.Hp, "block ate it, so health has not moved yet");
            Assert.AreEqual(4, afterFirst.Block);

            display.Reveal(ActorRef.Player);
            Assert.AreEqual(68, display.Of(ActorRef.Player, 68, 0).Hp);
        }

        [Test]
        public void FiguresAreTrackedSeparately()
        {
            var display = new CombatDisplayState();
            display.Of(ActorRef.Enemy(0), 40, 0);
            display.Of(ActorRef.Enemy(1), 25, 0);

            display.Defer(ActorRef.Enemy(0), 30, 0);

            Assert.AreEqual(40, display.Of(ActorRef.Enemy(0), 30, 0).Hp);
            Assert.AreEqual(25, display.Of(ActorRef.Enemy(1), 25, 0).Hp,
                "a blow on one figure must not hold up another");
        }

        [Test]
        public void HealsAndBlockAppearAtOnce()
        {
            var display = new CombatDisplayState();
            display.Of(ActorRef.Player, 50, 0);

            // Nothing owed, so a heal is not something to hold back — only the
            // results of blows are paced.
            Assert.AreEqual(62, display.Of(ActorRef.Player, 62, 0).Hp);
            Assert.AreEqual(8, display.Of(ActorRef.Player, 62, 8).Block);
        }

        [Test]
        public void RevealingWithNothingOwedDoesNothing()
        {
            var display = new CombatDisplayState();
            display.Of(Enemy, 40, 0);

            display.Reveal(Enemy);

            Assert.AreEqual(40, display.Of(Enemy, 40, 0).Hp);
        }

        [Test]
        public void ClearDropsWhatANewFightWillNeverPlay()
        {
            var display = new CombatDisplayState();
            display.Of(Enemy, 40, 0);
            display.Defer(Enemy, 34, 0);

            display.Clear();

            // The models for that fight are gone, so those blows will never be
            // released — the next fight must not inherit a stale hold.
            Assert.AreEqual(45, display.Of(Enemy, 45, 0).Hp);
        }

        [Test]
        public void AnInvalidReferencePassesTheRealValuesThrough()
        {
            var display = new CombatDisplayState();

            CombatDisplayState.Vitals shown = display.Of(ActorRef.None, 12, 3);

            Assert.AreEqual(12, shown.Hp);
            Assert.AreEqual(3, shown.Block);
        }

        [Test]
        public void ChangedFiresOnReveal()
        {
            var display = new CombatDisplayState();
            int changes = 0;
            display.Changed += () => changes++;

            display.Of(Enemy, 40, 0);
            display.Defer(Enemy, 34, 0);

            // Deferring is invisible — the screen still shows the old value, so
            // there is nothing to redraw until the blow is released.
            Assert.AreEqual(0, changes);

            display.Reveal(Enemy);
            Assert.AreEqual(1, changes);
        }
    }
}
