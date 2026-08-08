using System;
using System.Collections.Generic;
using UnityEngine;

namespace STSlike.Data
{
    /// <summary>How one condition presents itself.</summary>
    [Serializable]
    public struct StatusArt
    {
        [Tooltip("The id the condition is saved under — \"Weakness\", \"Broken\". " +
                 "Must match the status class exactly, case included.")]
        public string id;

        public Sprite icon;

        [Tooltip("Shown in a tooltip. Leave empty to fall back to the id.")]
        public string displayName;

        [TextArea(2, 4)] public string description;
    }

    /// <summary>
    /// What every condition looks like, in one asset.
    ///
    /// The rules know a condition only by its id — icons and wording are no
    /// business of a damage formula. This is where that id becomes something to
    /// look at, and it is deliberately one shared asset rather than a table on
    /// each prefab: with a dozen conditions, a per-prefab list is a dozen rows
    /// to maintain in every panel that ever shows one, and they drift.
    ///
    /// Adding a condition is then two files — the status class and a row here.
    /// </summary>
    [CreateAssetMenu(fileName = "StatusCatalog", menuName = "STSlike/Status Catalog", order = 101)]
    public sealed class StatusCatalog : ScriptableObject
    {
        [SerializeField] private List<StatusArt> statuses = new List<StatusArt>();

        /// <summary>
        /// False when nothing describes that id, which is a normal state while
        /// art is still being made — the caller keeps whatever it already had.
        /// </summary>
        public bool TryGet(string id, out StatusArt art)
        {
            art = default;
            if (string.IsNullOrEmpty(id))
                return false;

            foreach (StatusArt entry in statuses)
            {
                if (entry.id != id)
                    continue;

                art = entry;
                return true;
            }
            return false;
        }

        /// <summary>Falls back to the id so an undescribed condition still reads.</summary>
        public string NameOf(string id) =>
            TryGet(id, out StatusArt art) && !string.IsNullOrWhiteSpace(art.displayName)
                ? art.displayName
                : id;
    }
}
