namespace STSlike.Domain.Cards
{
    /// <summary>
    /// Keywords a card carries.
    ///
    /// Strings rather than an enum for the same reason damage kinds are: tags
    /// belong to characters, and a character can arrive from content this build
    /// was never compiled against.
    ///
    /// A tag is a noun the rules can ask about — it says what a card *is*, not
    /// what it does. What it does is its effects.
    /// </summary>
    public static class CardTags
    {
        /// <summary>
        /// Fired from the weapon. Marks the cards a firing stance will allow,
        /// and the ones that spend a round.
        /// </summary>
        public const string Shot = "Shot";
    }
}
