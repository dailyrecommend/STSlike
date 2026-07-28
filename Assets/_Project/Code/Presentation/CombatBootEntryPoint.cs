using VContainer.Unity;
using STSlike.Domain.Run;

namespace STSlike.Presentation
{
    /// <summary>
    /// VContainer entry point: once the container is built, start the run. Runs
    /// after component injection, so subscribers (the harness) are already
    /// listening when the opening events fire.
    /// </summary>
    public sealed class CombatBootEntryPoint : IStartable
    {
        private readonly RunFlow _flow;

        public CombatBootEntryPoint(RunFlow flow) => _flow = flow;

        public void Start() => _flow.Start();
    }
}
