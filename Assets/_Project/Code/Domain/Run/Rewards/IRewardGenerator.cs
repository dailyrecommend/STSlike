using System.Collections.Generic;

namespace STSlike.Domain.Run.Rewards
{
    /// <summary>Produces the choices offered after an encounter is cleared.</summary>
    public interface IRewardGenerator
    {
        IReadOnlyList<IRunReward> Generate(RunContext run);
    }
}
