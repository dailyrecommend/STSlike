namespace STSlike.Domain.Relics
{
    /// <summary>
    /// How often a relic turns up. Its own enum rather than the card one: they
    /// happen to have the same three values today, but a card's rarity is wired
    /// into an art table in every card prefab, and sharing would mean re-authoring
    /// those to gain nothing.
    /// </summary>
    public enum RelicRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Boss = 3,
    }

    /// <summary>
    /// The design data behind a relic: what it is called, how often it turns up,
    /// and how to make one.
    ///
    /// A relic keeps its own state, so a run cannot share the object the content
    /// was authored on — every run asks for a fresh one. That is the whole reason
    /// this is a separate thing from <see cref="Relic"/> rather than the relic
    /// simply being the asset.
    /// </summary>
    public interface IRelicDefinition
    {
        /// <summary>Stable key, for saves, drops and looking up its art.</summary>
        string Id { get; }

        RelicRarity Rarity { get; }

        /// <summary>
        /// Whether this may be found during a run. False for one handed out by
        /// a character's starting kit, or one still being worked on.
        /// </summary>
        bool Obtainable { get; }

        /// <summary>
        /// Empty for a relic anyone may find; a character id to keep it to that
        /// character, the way a revolver's relic means nothing to a swordsman.
        /// </summary>
        string CharacterId { get; }

        /// <summary>A new one, ready to be carried. Null if unfinished.</summary>
        Relic Create();
    }
}
