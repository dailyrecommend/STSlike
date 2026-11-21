using System.Collections.Generic;
using STSlike.Domain.Actors;

namespace STSlike.Domain.Content
{
    /// <summary>
    /// A room the player can walk into: which enemies stand in it, and where it
    /// is allowed to appear.
    ///
    /// The opposition is named by id rather than held by reference, the same as
    /// everywhere else — a room can then be described by content the build never
    /// compiled against, and a saved run can record which room it was.
    ///
    /// This is what turns "one enemy of the right tier" into a designed fight.
    /// Two slimes and a brute is a different problem from one brute with the
    /// same total health, and only one of those can be authored without it.
    /// </summary>
    public interface IEncounterDefinition
    {
        string Id { get; }

        /// <summary>Which kind of node this room can fill.</summary>
        EnemyTier Tier { get; }

        /// <summary>
        /// The enemies standing in it, in the order they are placed. An id with
        /// nothing behind it is skipped rather than fatal.
        /// </summary>
        IReadOnlyList<string> EnemyIds { get; }

        /// <summary>
        /// The stretch of the act this room belongs to, 1-based and inclusive.
        /// A maximum of zero means "as far as the act goes" — the usual case
        /// for a room that never stops being appropriate.
        /// </summary>
        int MinFloor { get; }
        int MaxFloor { get; }
    }
}
