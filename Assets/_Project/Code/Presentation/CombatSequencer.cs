using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace STSlike.Presentation
{
    /// <summary>
    /// Plays presentation steps one at a time. The domain resolves combat
    /// instantly and fires events; those events enqueue async steps here, and
    /// this runner awaits each before starting the next — so animations never
    /// overlap. This is where UniTask sequencing lives, kept out of the domain.
    /// </summary>
    public sealed class CombatSequencer
    {
        private readonly Queue<Func<UniTask>> _steps = new Queue<Func<UniTask>>();
        private bool _running;

        /// <summary>
        /// True while the screen is still catching up with the rules.
        ///
        /// The domain settles a whole turn the moment it is asked to, so "the
        /// enemy's turn" does not exist as a state it can be asked about — by
        /// the time the first blow animates it is already the player's turn
        /// again. This queue draining is what that stretch of time actually is,
        /// and it is what input has to wait for.
        /// </summary>
        public bool IsBusy => _running;

        /// <summary>Raised when <see cref="IsBusy"/> flips, so views can rebind.</summary>
        public event Action BusyChanged;

        public void Enqueue(Func<UniTask> step)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));

            _steps.Enqueue(step);
            if (!_running)
                Run().Forget();
        }

        private async UniTask Run()
        {
            SetRunning(true);
            try
            {
                while (_steps.Count > 0)
                {
                    Func<UniTask> step = _steps.Dequeue();
                    await step();
                }
            }
            finally
            {
                SetRunning(false);
            }
        }

        private void SetRunning(bool running)
        {
            if (_running == running)
                return;

            _running = running;
            BusyChanged?.Invoke();
        }
    }
}
