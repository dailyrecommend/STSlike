using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Material inspector for Custom/ToonLit. The shader's blend state, ZWrite and
/// keywords are exposed as plain properties, but they only produce correct
/// results in specific combinations — and the render queue and RenderType tag
/// live on the material, not the shader, so a transparent material must set
/// them explicitly or it sorts as opaque. This drives all of that from one
/// Surface Type dropdown.
///
/// Global namespace on purpose: the shader references it by bare name
/// (CustomEditor "ToonLitShaderGUI").
/// </summary>
public class ToonLitShaderGUI : ShaderGUI
{
    private enum SurfaceType
    {
        Opaque = 0,
        Cutout = 1,
        Transparent = 2,
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        var material = (Material)materialEditor.target;
        MaterialProperty surface = FindProperty("_Surface", properties, false);

        if (surface != null)
        {
            var current = (SurfaceType)surface.floatValue;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = surface.hasMixedValue;
            var selected = (SurfaceType)EditorGUILayout.EnumPopup("Surface Type", current);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo("Surface Type");
                surface.floatValue = (float)selected;

                foreach (Object target in materialEditor.targets)
                    Apply((Material)target, selected);
            }

            EditorGUILayout.Space();
        }

        base.OnGUI(materialEditor, properties);

        // Keep keywords in sync when individual toggles are edited directly.
        foreach (Object target in materialEditor.targets)
            SyncKeywords((Material)target);
    }

    /// <summary>Called by Unity whenever a property changes; keeps material variants correct.</summary>
    public override void ValidateMaterial(Material material)
    {
        if (material.HasProperty("_Surface"))
            Apply(material, (SurfaceType)material.GetFloat("_Surface"));
        else
            SyncKeywords(material);
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);
        ValidateMaterial(material);
    }

    private static void Apply(Material material, SurfaceType surface)
    {
        switch (surface)
        {
            case SurfaceType.Opaque:
                SetBlend(material, BlendMode.One, BlendMode.Zero, zWrite: true);
                material.SetFloat("_AlphaClip", 0f);
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = (int)RenderQueue.Geometry;
                break;

            case SurfaceType.Cutout:
                SetBlend(material, BlendMode.One, BlendMode.Zero, zWrite: true);
                material.SetFloat("_AlphaClip", 1f);
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.renderQueue = (int)RenderQueue.AlphaTest;
                break;

            case SurfaceType.Transparent:
                // Straight alpha blending; ZWrite off so overlapping faces of the
                // same object don't clip each other.
                SetBlend(material, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, zWrite: false);
                material.SetFloat("_AlphaClip", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
                break;
        }

        SyncKeywords(material);
    }

    private static void SetBlend(Material material, BlendMode src, BlendMode dst, bool zWrite)
    {
        material.SetFloat("_SrcBlend", (float)src);
        material.SetFloat("_DstBlend", (float)dst);
        material.SetFloat("_ZWrite", zWrite ? 1f : 0f);
    }

    private static void SyncKeywords(Material material)
    {
        if (material.HasProperty("_AlphaClip"))
            SetKeyword(material, "_ALPHATEST_ON", material.GetFloat("_AlphaClip") >= 0.5f);

        if (material.HasProperty("_ReceiveShadows"))
            SetKeyword(material, "_RECEIVE_SHADOWS_OFF", material.GetFloat("_ReceiveShadows") < 0.5f);
    }

    private static void SetKeyword(Material material, string keyword, bool enabled)
    {
        if (enabled)
            material.EnableKeyword(keyword);
        else
            material.DisableKeyword(keyword);
    }
}
