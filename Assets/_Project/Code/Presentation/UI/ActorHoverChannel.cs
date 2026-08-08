using System;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Which figure on stage the pointer is over.
    ///
    /// Separate from <see cref="CardSelection"/> because pointing at somebody is
    /// not the same as aiming at them: a name should appear whenever the pointer
    /// finds a figure, whether or not a card is waiting to be thrown. Aiming
    /// then reads this rather than tracking the pointer a second time.
    ///
    /// The same shape as <see cref="CardHoverChannel"/>, one layer out.
    /// </summary>
    public sealed class ActorHoverChannel
    {
        public ActorRef Hovered { get; private set; } = ActorRef.None;

        public bool IsShowing => Hovered.IsValid;

        public event Action Changed;

        public void Set(ActorRef actor)
        {
            if (Same(Hovered, actor))
                return;

            Hovered = actor;
            Changed?.Invoke();
        }

        public void Clear() => Set(ActorRef.None);

        /// <summary>True when this is the figure currently under the pointer.</summary>
        public bool Is(ActorRef actor) => Hovered.IsValid && Same(Hovered, actor);

        private static bool Same(ActorRef a, ActorRef b) =>
            a.IsPlayer == b.IsPlayer && a.EnemyIndex == b.EnemyIndex;
    }
}
