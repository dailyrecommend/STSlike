namespace STSlike.Domain.Run
{
    /// <summary>
    /// Lets the run flow start a fight without knowing how one is assembled
    /// (decks, RNG, Unity objects). The presentation layer supplies the
    /// implementation, so the flow states stay pure and testable.
    /// </summary>
    public interface ICombatLauncher
    {
        void StartCombat(CombatRequest request);
    }
}
