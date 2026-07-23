using System;

namespace STSlike.Domain.Randomness
{
    /// <summary>
    /// Default <see cref="IRng"/> backed by <see cref="System.Random"/>.
    /// The seed is kept so it can be persisted (e.g. with a run's history)
    /// and replayed later.
    /// </summary>
    public sealed class SystemRng : IRng
    {
        private readonly Random _random;

        public int Seed { get; }

        public SystemRng(int seed)
        {
            Seed = seed;
            _random = new Random(seed);
        }

        public int Next(int maxExclusive) => _random.Next(maxExclusive);
    }
}
