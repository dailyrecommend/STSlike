using UnityEditor;
using UnityEngine;

namespace STSlike.Data.Editor
{
    /// <summary>
    /// Adds the rescan button, and shows what the catalogue currently holds so
    /// an empty one is obvious at a glance rather than at runtime.
    /// </summary>
    [CustomEditor(typeof(ContentCatalog))]
    public sealed class ContentCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var catalog = (ContentCatalog)target;

            EditorGUILayout.HelpBox(
                $"{catalog.CardCount} cards, {catalog.EnemyCount} enemies, " +
                $"{catalog.CharacterCount} characters, {catalog.EncounterCount} rooms, " +
                $"{catalog.RelicCount} relics.\n\n" +
                "Rescan after adding or deleting content assets. The lists below " +
                "are what ships in the build.",
                catalog.CardCount == 0 ? MessageType.Warning : MessageType.Info);

            if (GUILayout.Button("Rescan Project", GUILayout.Height(28f)))
            {
                Undo.RecordObject(catalog, "Rescan content catalog");
                catalog.Rescan();
                AssetDatabase.SaveAssetIfDirty(catalog);
            }

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
}
