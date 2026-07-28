using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace STSlike.ArtTools
{
    /// <summary>
    /// Bakes averaged ("smooth") normals so the inverted-hull outline shader can
    /// expand a watertight shell around a hard-edged low-poly character.
    ///
    /// Two things make this work on a real character:
    ///
    /// 1. The result is written to TANGENT. Unity skins only the position,
    ///    normal and tangent streams — UV and color pass through unskinned, so a
    ///    normal stored there points the wrong way as soon as a bone rotates and
    ///    the outline tears. Copies also go to color/UV3 for meshes that need
    ///    their real tangents for something else.
    ///
    /// 2. Every selected renderer is welded together in world space, so parts
    ///    that touch (head/neck/collar, hair against the head) agree on a shared
    ///    normal and the silhouette stays continuous across them. Bake the whole
    ///    character in one go — part-by-part baking reintroduces the seams.
    /// </summary>
    public static class SmoothNormalBaker
    {
        private const string OutputFolder = "Assets/_Project/Art/SmoothedMeshes";
        private const string Suffix = "_SmoothNormal";

        // Positions are snapped to this grid before welding: vertices that should
        // coincide usually differ by float error after FBX import, and exact
        // equality would leave them ungrouped, gapping the outline.
        private const float WeldPrecision = 1e-4f;

        private sealed class Target
        {
            public Renderer Renderer;
            public Transform Transform;
            public Mesh Mesh;
            public Vector3[] Vertices;
            public Vector3[] Normals;
            public Vector3Int[] Keys;
        }

        [MenuItem("Tools/Toon Shader/Bake Smooth Normals (Selected)")]
        private static void BakeSelected()
        {
            List<Target> targets = CollectTargets();
            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("Bake Smooth Normals",
                    "No MeshFilter or SkinnedMeshRenderer found in the selection.\n\n" +
                    "Select the character root and try again.", "OK");
                return;
            }

            // Pass 1: accumulate angle-weighted face normals in world space,
            // shared across every selected mesh.
            var accumulated = new Dictionary<Vector3Int, Vector3>();
            foreach (Target target in targets)
                Accumulate(target, accumulated);

            // Pass 2: resolve each vertex and write the channels.
            Directory.CreateDirectory(OutputFolder);
            foreach (Target target in targets)
                WriteAndSave(target, accumulated);

            AssetDatabase.SaveAssets();
            Debug.Log($"[SmoothNormalBaker] Baked {targets.Count} mesh(es) into {OutputFolder}. " +
                      "Set the outline material's Source to 'Tangent'.");
        }

        private static List<Target> CollectTargets()
        {
            var targets = new List<Target>();

            foreach (GameObject go in Selection.gameObjects)
            {
                foreach (var filter in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null)
                        continue;
                    targets.Add(MakeTarget(filter.GetComponent<Renderer>(), filter.transform, filter.sharedMesh));
                }

                foreach (var skinned in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (skinned.sharedMesh == null)
                        continue;
                    targets.Add(MakeTarget(skinned, skinned.transform, skinned.sharedMesh));
                }
            }

            return targets;
        }

        private static Target MakeTarget(Renderer renderer, Transform transform, Mesh source)
        {
            Mesh working = Object.Instantiate(source);
            working.name = StripSuffix(source.name) + Suffix;

            var target = new Target
            {
                Renderer = renderer,
                Transform = transform,
                Mesh = working,
                Vertices = working.vertices,
                Normals = working.normals,
            };

            // Weld in world space so separate parts that touch share a normal.
            Matrix4x4 toWorld = transform.localToWorldMatrix;
            target.Keys = new Vector3Int[target.Vertices.Length];
            for (int i = 0; i < target.Vertices.Length; i++)
                target.Keys[i] = Weld(toWorld.MultiplyPoint3x4(target.Vertices[i]));

            return target;
        }

        private static void Accumulate(Target target, Dictionary<Vector3Int, Vector3> accumulated)
        {
            Mesh mesh = target.Mesh;
            Vector3[] vertices = target.Vertices;
            Matrix4x4 toWorld = target.Transform.localToWorldMatrix;

            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                int[] triangles = mesh.GetTriangles(sub);
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    int i0 = triangles[t];
                    int i1 = triangles[t + 1];
                    int i2 = triangles[t + 2];

                    Vector3 p0 = toWorld.MultiplyPoint3x4(vertices[i0]);
                    Vector3 p1 = toWorld.MultiplyPoint3x4(vertices[i1]);
                    Vector3 p2 = toWorld.MultiplyPoint3x4(vertices[i2]);

                    Vector3 faceNormal = Vector3.Cross(p1 - p0, p2 - p0);
                    if (faceNormal.sqrMagnitude < 1e-12f)
                        continue; // degenerate triangle
                    faceNormal.Normalize();

                    AddCorner(accumulated, target.Keys[i0], faceNormal, p0, p1, p2);
                    AddCorner(accumulated, target.Keys[i1], faceNormal, p1, p2, p0);
                    AddCorner(accumulated, target.Keys[i2], faceNormal, p2, p0, p1);
                }
            }
        }

        // Angle weighting keeps the average stable when one side of a corner has
        // many small triangles and the other a single large one; a plain sum
        // would lean toward the dense side and dent the hull.
        private static void AddCorner(Dictionary<Vector3Int, Vector3> accumulated,
            Vector3Int key, Vector3 faceNormal, Vector3 corner, Vector3 a, Vector3 b)
        {
            Vector3 e0 = a - corner;
            Vector3 e1 = b - corner;
            if (e0.sqrMagnitude < 1e-12f || e1.sqrMagnitude < 1e-12f)
                return;

            float weight = Vector3.Angle(e0, e1) * Mathf.Deg2Rad;
            accumulated.TryGetValue(key, out Vector3 sum);
            accumulated[key] = sum + faceNormal * weight;
        }

        private static void WriteAndSave(Target target, Dictionary<Vector3Int, Vector3> accumulated)
        {
            Mesh mesh = target.Mesh;
            int count = target.Vertices.Length;
            Matrix4x4 toLocal = target.Transform.worldToLocalMatrix;

            var tangents = new Vector4[count];
            var colors = new Color[count];
            var uv3 = new List<Vector3>(count);

            for (int i = 0; i < count; i++)
            {
                accumulated.TryGetValue(target.Keys[i], out Vector3 worldNormal);

                Vector3 local;
                if (worldNormal.sqrMagnitude < 1e-8f)
                {
                    // Opposing normals cancelled (zero-thickness flap, enclosed
                    // seam). Fall back to the authored normal so the vertex still
                    // expands outward instead of collapsing.
                    local = i < target.Normals.Length && target.Normals[i].sqrMagnitude > 1e-8f
                        ? target.Normals[i].normalized
                        : Vector3.up;
                }
                else
                {
                    local = toLocal.MultiplyVector(worldNormal).normalized;
                }

                // Tangent stores the direction directly (signed, full precision)
                // and is skinned by the GPU along with position and normal.
                tangents[i] = new Vector4(local.x, local.y, local.z, 1f);

                // Encoded copies for meshes that need their real tangents.
                Vector3 encoded = local * 0.5f + new Vector3(0.5f, 0.5f, 0.5f);
                colors[i] = new Color(encoded.x, encoded.y, encoded.z, 1f);
                uv3.Add(encoded);
            }

            mesh.tangents = tangents;
            mesh.colors = colors;
            mesh.SetUVs(3, uv3);

            string path = AssetDatabase.GenerateUniqueAssetPath($"{OutputFolder}/{mesh.name}.asset");
            AssetDatabase.CreateAsset(mesh, path);

            if (target.Renderer is SkinnedMeshRenderer skinned)
                skinned.sharedMesh = mesh;
            else if (target.Transform.TryGetComponent(out MeshFilter filter))
                filter.sharedMesh = mesh;

            if (target.Renderer != null)
                EditorUtility.SetDirty(target.Renderer);
        }

        // Keeps re-bakes from producing Name_SmoothNormal_SmoothNormal_...
        private static string StripSuffix(string name)
        {
            while (name.EndsWith(Suffix))
                name = name.Substring(0, name.Length - Suffix.Length);
            return name;
        }

        private static Vector3Int Weld(Vector3 position)
        {
            return new Vector3Int(
                Mathf.RoundToInt(position.x / WeldPrecision),
                Mathf.RoundToInt(position.y / WeldPrecision),
                Mathf.RoundToInt(position.z / WeldPrecision));
        }
    }
}
