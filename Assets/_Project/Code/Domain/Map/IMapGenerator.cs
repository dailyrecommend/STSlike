using STSlike.Domain.Randomness;

namespace STSlike.Domain.Map
{
    /// <summary>Produces an act map. Seeded, so a run's map is reproducible.</summary>
    public interface IMapGenerator
    {
        RunMap Generate(IRng rng, MapGenerationSettings settings);
    }
}
