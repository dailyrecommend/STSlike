namespace STSlike.Domain.Commands
{
    /// <summary>
    /// A single, queued unit of combat execution (Command pattern). Reifying
    /// "do this now" as an object lets an invoker order it, pace it for
    /// animation, and record it for replay.
    /// </summary>
    public interface ICommand
    {
        void Execute();
    }
}
