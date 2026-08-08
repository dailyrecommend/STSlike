using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace STSlike.Presentation
{
    /// <summary>
    /// One row of an inspector art table: "this kind looks like this".
    ///
    /// Generic so the same row serves cards, map nodes, intents and anything
    /// else keyed by a fixed set of kinds. Content keyed by a string id — cards,
    /// enemies, conditions — is described by its own asset instead, since a
    /// table per prefab drifts as that content grows.
    ///
    /// The field is called <c>type</c> rather than <c>key</c> because that is
    /// what every one of these tables already called it — renaming would have
    /// thrown away the art already assigned in the prefabs, and Unity matches
    /// serialized data by name.
    /// </summary>
    [Serializable]
    public struct ArtEntry<TKey>
    {
        public TKey type;
        public Sprite sprite;

        [Tooltip("Left fully transparent means 'no opinion' — the target keeps " +
                 "whatever colour it already had.")]
        public Color tint;
    }

    /// <summary>
    /// Looks art up by kind.
    ///
    /// Five binders were each carrying their own copy of this: a little struct,
    /// a linear search, and the same two rules written out again — replace the
    /// sprite only when one was authored, and treat a fully transparent tint as
    /// "not set". Those rules are the whole point of the table, and they belong
    /// in one place. Repeated, they drift: a table that blanks the image for an
    /// unauthored kind reads as broken art rather than as missing art.
    ///
    /// Tables stay small (a handful of kinds), so a linear search is the right
    /// shape — a dictionary would cost more to build than it ever saves.
    /// </summary>
    internal static class ArtTable
    {
        public static bool TryFind<TKey>(ArtEntry<TKey>[] entries, TKey type,
            out ArtEntry<TKey> found)
        {
            found = default;
            if (entries == null)
                return false;

            foreach (ArtEntry<TKey> entry in entries)
            {
                if (!EqualityComparer<TKey>.Default.Equals(entry.type, type))
                    continue;

                found = entry;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Puts this kind's art on the image, leaving anything the table has no
        /// opinion about exactly as the prefab had it.
        /// </summary>
        public static void Apply<TKey>(ArtEntry<TKey>[] entries, TKey type, Image target)
        {
            if (target == null || !TryFind(entries, type, out ArtEntry<TKey> entry))
                return;

            UiSlot.SetSprite(target, entry.sprite);

            if (entry.tint.a > 0f)
                target.color = entry.tint;
        }

        /// <summary>
        /// This kind's colour, or the fallback when the table does not name one.
        /// For callers that colour something other than the image they applied.
        /// </summary>
        public static Color Tint<TKey>(ArtEntry<TKey>[] entries, TKey type, Color fallback)
        {
            return TryFind(entries, type, out ArtEntry<TKey> entry) && entry.tint.a > 0f
                ? entry.tint
                : fallback;
        }
    }
}
