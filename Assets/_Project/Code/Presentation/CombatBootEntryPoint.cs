using System;
using VContainer.Unity;
using STSlike.Domain.Progression;
using STSlike.Domain.Run;

namespace STSlike.Presentation
{
    /// <summary>
    /// VContainer entry point: once the container is built, start recording
    /// progression and then start the run. Runs after component injection, so
    /// subscribers (the harness) are already listening when the opening events
    /// fire, and the recorder is listening before the first reward can be taken.
    /// </summary>
    public sealed class CombatBootEntryPoint : IStartable, IDisposable
    {
        private readonly RunFlow _flow;
        private readonly ProgressionRecorder _recorder;

        public CombatBootEntryPoint(RunFlow flow, ProgressionRecorder recorder)
        {
            _flow = flow;
            _recorder = recorder;
        }

        public void Start()
        {
            _recorder.Start();
            _flow.Start();
        }

        public void Dispose() => _recorder.Dispose();
    }
}
