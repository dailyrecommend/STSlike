namespace STSlike.Domain.Randomness
{
    /// <summary>
    /// Deterministic random source. Injecting it (rather than calling a global
    /// RNG) makes shuffles reproducible for a given seed — the basis for
    /// replays, debugging, and reproducible runs stored with a run's seed.
    /// </summary>
    public interface IRng
    {
        /// <summary>Returns a non-negative integer in [0, maxExclusive).</summary>
        int Next(int maxExclusive);
    }
}
