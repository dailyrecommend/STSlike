#if STSLIKE_SQLITE
using SQLite;

namespace STSlike.Infrastructure.Persistence.Sqlite
{
    /// <summary>
    /// Storage shape for a finished run. Kept separate from the domain's
    /// RunRecord: persistence concerns (primary keys, indexes, how a DateTime is
    /// stored) belong to the database, not to the game rules.
    /// </summary>
    [Table("RunHistory")]
    public sealed class RunHistoryRow
    {
        [PrimaryKey]
        public string RunId { get; set; }

        [Indexed]
        public string CharacterId { get; set; }

        public int Seed { get; set; }
        public bool PlayerWon { get; set; }
        public int EncountersCleared { get; set; }
        public int TotalEncounters { get; set; }
        public int DeckSize { get; set; }

        /// <summary>UTC ticks: unambiguous across platforms and time zones.</summary>
        [Indexed]
        public long CompletedAtUtcTicks { get; set; }
    }

    /// <summary>
    /// The single in-progress run. Deck card ids are stored as one delimited
    /// column rather than a child table: it is always read and written whole,
    /// never queried by card, so a join would add cost without buying anything.
    /// </summary>
    [Table("RunSave")]
    public sealed class RunSaveRow
    {
        /// <summary>Always 1 — one save slot, enforced by the primary key.</summary>
        [PrimaryKey]
        public int Id { get; set; }

        public string CharacterId { get; set; }
        public int Seed { get; set; }
        public int RngCallCount { get; set; }
        public int CurrentEncounter { get; set; }
        public int TotalEncounters { get; set; }
        public int PlayerHp { get; set; }
        public int PlayerMaxHp { get; set; }
        public int Stage { get; set; }
        public string DeckCardIds { get; set; }
        public long SavedAtUtcTicks { get; set; }
    }

    /// <summary>Storage shape for per-card balancing totals.</summary>
    [Table("CardPickStats")]
    public sealed class CardStatsRow
    {
        [PrimaryKey]
        public string CardId { get; set; }

        public int PickCount { get; set; }
        public int RunsWithCard { get; set; }
        public int WinsWithCard { get; set; }
    }
}
#endif
