using System.Collections.Generic;
using UnityEngine;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Characters;
using STSlike.Domain.Content;

namespace STSlike.Data
{
    /// <summary>
    /// The built-in content, as one asset.
    ///
    /// This is the project's own <see cref="IContentSource"/>: the cards,
    /// enemies and characters authored as ScriptableObjects. The lists are
    /// filled by scanning the project rather than by dragging assets in one at
    /// a time — a new card is then a matter of creating it, not of remembering
    /// to register it somewhere.
    ///
    /// Why an asset at all, rather than loading everything at runtime: a list
    /// that exists in the build is a list the build can be reasoned about, and
    /// it is the natural thing for Addressables to take over later. The scene
    /// ends up holding one reference to this instead of a reference to every
    /// piece of content in the game.
    /// </summary>
    [CreateAssetMenu(fileName = "ContentCatalog", menuName = "STSlike/Content Catalog", order = 100)]
    public sealed class ContentCatalog : ScriptableObject, IContentSource
    {
        [Tooltip("Filled by 'Rescan Project' in the inspector. Editing by hand " +
                 "works too, but a rescan will overwrite it.")]
        [SerializeField] private List<CardData> cards = new List<CardData>();

        [SerializeField] private List<EnemyData> enemies = new List<EnemyData>();
        [SerializeField] private List<CharacterData> characters = new List<CharacterData>();

        public string Name => name;

        public IEnumerable<ICardDefinition> Cards => cards;
        public IEnumerable<IEnemyDefinition> Enemies => enemies;
        public IEnumerable<ICharacterDefinition> Characters => characters;

        /// <summary>Counts for the inspector; cheaper than describing the lists.</summary>
        public int CardCount => cards.Count;
        public int EnemyCount => enemies.Count;
        public int CharacterCount => characters.Count;

#if UNITY_EDITOR
        /// <summary>
        /// Replaces the lists with everything of these kinds in the project.
        ///
        /// Editor-only on purpose: a build must ship a decided list, not go
        /// looking. Assets are sorted by id so the file's diff stays readable
        /// when content is added.
        /// </summary>
        public void Rescan()
        {
            cards = Collect<CardData>(d => d.Id);
            enemies = Collect<EnemyData>(d => d.Id);
            characters = Collect<CharacterData>(d => d.Id);

            UnityEditor.EditorUtility.SetDirty(this);
        }

        private static List<T> Collect<T>(System.Func<T, string> idOf) where T : ScriptableObject
        {
            var found = new List<T>();
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    found.Add(asset);
            }

            found.Sort((a, b) => string.CompareOrdinal(idOf(a), idOf(b)));
            return found;
        }
#endif
    }
}
