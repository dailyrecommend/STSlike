using VContainer.Unity;

namespace STSlike.Presentation
{
    /// <summary>
    /// VContainer entry point: once the container is built, start the first
    /// combat. Runs after component injection, so subscribers (the view) are
    /// already listening when the opening events fire.
    /// </summary>
    public sealed class CombatBootEntryPoint : IStartable
    {
        private readonly CombatRunner _runner;

        public CombatBootEntryPoint(CombatRunner runner) => _runner = runner;

        public void Start() => _runner.StartNew();
    }
}
