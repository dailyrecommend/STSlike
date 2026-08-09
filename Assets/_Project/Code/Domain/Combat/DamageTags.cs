namespace STSlike.Domain.Combat
{
    /// <summary>
    /// What kind of blow this is, so a condition can care about some and not
    /// others.
    ///
    /// A string rather than an enum: kinds belong to characters, and a character
    /// can arrive from content the rules were never compiled against. An enum is
    /// the one thing a mod cannot add a case to.
    ///
    /// Untagged damage — <see cref="None"/> — is ordinary damage that every
    /// condition applies to. Only mark a blow when something is meant to treat
    /// it differently.
    /// </summary>
    public static class DamageTags
    {
        public const string None = null;

        /// <summary>
        /// Fired from a weapon rather than swung by the person holding it. The
        /// gun's output does not care how weakened its owner is, though the
        /// stance they are firing from very much does.
        /// </summary>
        public const string Shot = "Shot";
    }
}
