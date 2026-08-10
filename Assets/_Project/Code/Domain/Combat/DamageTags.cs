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
        /// Fired from a weapon rather than swung by the person holding it.
        ///
        /// **A new condition should leave this alone.** What a round does is
        /// settled by the weapon: not by how weakened the shooter is, not by how
        /// badly the target is reeling. That reliability is the character's
        /// whole strength, and it is only worth anything if it holds against
        /// everything rather than against most things.
        ///
        /// The exceptions are conditions that are *about* the weapon — a firing
        /// stance, say. Those opt in deliberately, and are the only reason this
        /// still runs through the condition hooks at all.
        /// </summary>
        public const string Shot = "Shot";
    }
}
