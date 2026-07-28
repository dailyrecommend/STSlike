namespace STSlike.Domain.Run.Rewards
{
    /// <summary>
    /// Strategy: one thing the player can pick between encounters. Adding a new
    /// reward kind (relic, max-HP up, card removal) means adding a class, not
    /// editing the reward flow.
    /// </summary>
    public interface IRunReward
    {
        void Apply(RunContext run);
    }
}
