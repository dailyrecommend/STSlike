using System.Collections.Generic;

namespace STSlike.Domain.Actors
{
    /// <summary>How dangerous an encounter is meant to be.</summary>
    public enum EnemyTier
    {
        Normal = 0,
        Elite = 1,
        Boss = 2,
    }

    /// <summary>
    /// The rules' view of an enemy: how tough it is and how it behaves. Art,
    /// models and display names stay out, exactly as with cards and characters.
    ///
    /// The numbers here are exactly what the enemy is — there is no per-floor
    /// scaling applied on top. Difficulty comes from which enemies a floor is
    /// allowed to draw, which keeps every asset readable on its own.
    /// </summary>
    public interface IEnemyDefinition
    {
        string Id { get; }
        EnemyTier Tier { get; }
        int MaxHp { get; }

        /// <summary>The routine it cycles through, in order.</summary>
        IReadOnlyList<EnemyMove> Pattern { get; }
    }
}
