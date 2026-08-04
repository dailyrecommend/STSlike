using System;

namespace STSlike.Domain.Map
{
    /// <summary>
    /// Knobs for <see cref="MapGenerator"/>. Everything an act's shape depends
    /// on lives here, so a second act — or a harder game mode — is a different
    /// settings object rather than different code.
    /// </summary>
    public sealed class MapGenerationSettings
    {
        /// <summary>Number of floors, including the boss floor.</summary>
        public int RowCount { get; set; } = 12;

        /// <summary>How many lanes wide the map may be.</summary>
        public int Columns { get; set; } = 6;

        /// <summary>
        /// How many routes are walked from bottom to top. More paths means a
        /// wider, busier map with more branching.
        /// </summary>
        public int PathCount { get; set; } = 5;

        /// <summary>Elites are held back so the opening floors stay approachable.</summary>
        public int FirstEliteRow { get; set; } = 4;

        /// <summary>Floor that is always treasure. Negative disables it.</summary>
        public int TreasureRow { get; set; } = 5;

        /// <summary>The floor below the boss is a rest stop, as a chance to prepare.</summary>
        public bool RestBeforeBoss { get; set; } = true;

        // Relative odds for the floors that are not fixed by the rules above.
        public int CombatWeight { get; set; } = 60;
        public int EventWeight { get; set; } = 18;
        public int EliteWeight { get; set; } = 10;
        public int ShopWeight { get; set; } = 6;
        public int RestWeight { get; set; } = 6;

        public void Validate()
        {
            if (RowCount < 2)
                throw new ArgumentOutOfRangeException(nameof(RowCount), "A map needs a start floor and a boss floor.");
            if (Columns < 1)
                throw new ArgumentOutOfRangeException(nameof(Columns));
            if (PathCount < 1)
                throw new ArgumentOutOfRangeException(nameof(PathCount));
            if (CombatWeight + EventWeight + EliteWeight + ShopWeight + RestWeight <= 0)
                throw new InvalidOperationException("At least one node weight must be positive.");
        }
    }
}
