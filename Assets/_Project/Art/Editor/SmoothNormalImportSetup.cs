using UnityEditor;
using UnityEngine;

namespace STSlike.ArtTools
{
    /// <summary>
    /// Forces a model to be imported with fully smoothed normals.
    ///
    /// Setting "Normals: Calculate" alone does nothing when Smoothness Source is
    /// left at its default (Prefer Smoothing Groups): the hard smoothing groups
    /// baked into the FBX win and the smoothing angle is ignored. That is the
    /// usual reason a low-poly model still shades facet-by-facet after the
    /// import settings "look" right.
    /// </summary>
    public static class SmoothNormalImportSetup
    {
        [MenuItem("Tools/Toon Shader/Smooth Model Normals (Selected FBX)")]
        private static void ApplySmoothNormals()
        {
            int changed = 0;

            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer))
                    continue;

                importer.importNormals = ModelImporterNormals.Calculate;
                importer.normalSmoothingSource = ModelImporterNormalSmoothingSource.FromAngle;
                importer.normalSmoothingAngle = 180f;
                importer.SaveAndReimport();

                Debug.Log($"[SmoothNormalImportSetup] Smoothed normals for {path}", obj);
                changed++;
            }

            if (changed == 0)
            {
                EditorUtility.DisplayDialog("Smooth Model Normals",
                    "Select a model asset (the .fbx itself in the Project window) and try again.", "OK");
            }
        }

        [MenuItem("Tools/Toon Shader/Restore Model Normals From File (Selected FBX)")]
        private static void RestoreImportedNormals()
        {
            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer))
                    continue;

                importer.importNormals = ModelImporterNormals.Import;
                importer.SaveAndReimport();
                Debug.Log($"[SmoothNormalImportSetup] Restored authored normals for {path}", obj);
            }
        }
    }
}
