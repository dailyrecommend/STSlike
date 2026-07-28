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
            _running = true;
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
                _running = false;
            }
        }
    }
}
