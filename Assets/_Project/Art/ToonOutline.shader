Shader "Custom/ToonOutline"
{
    Properties
    {
        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidthPx ("Width (pixels)", Range(0,16)) = 3

        [Header(Transparency)]
        // Match the lit material's cutout, or the hull stays solid where the
        // texture is punched through and leaves outline blobs.
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip (cutout)", Float) = 0
        _BaseMap ("Base Texture (for cutout)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5

        [Header(Smooth Normals)]
        // Tangent is the only channel Unity skins, so it is the correct source
        // for skinned characters. Color/UV3 are for static meshes whose real
        // tangents are needed elsewhere.
        [KeywordEnum(None, Tangent, VertexColor, UV3)] _SmoothNormal ("Source", Float) = 1

        [Header(Artifact Fixes)]
        _DepthBias ("Depth Bias (kills inner smudges)", Range(0,0.02)) = 0.002

        [Header(Distance)]
        [Toggle(_WIDTH_WORLD_SPACE)] _WidthWorldSpace ("Use World Width (thins with distance)", Float) = 0
        _WidthWorld ("World Width", Range(0,0.2)) = 0.02
        _FadeStart ("Fade Start Distance", Float) = 0
        _FadeEnd ("Fade End Distance", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
            "IgnoreProjector"="True"
        }
        LOD 200

        // ---------- Outline (inverted hull) ----------
        // Assigned as a SECOND material on the renderer: the mesh is drawn again
        // with front faces culled and back faces pushed outward, leaving a solid
        // silhouette ring around the toon-lit mesh.
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="UniversalForward" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma target 3.0

            #pragma shader_feature_local_vertex _SMOOTHNORMAL_NONE _SMOOTHNORMAL_TANGENT _SMOOTHNORMAL_VERTEXCOLOR _SMOOTHNORMAL_UV3
            #pragma shader_feature_local_vertex _WIDTH_WORLD_SPACE
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _OutlineColor;
                half   _OutlineWidthPx;
                half   _DepthBias;
                half   _AlphaClip;
                float4 _BaseMap_ST;
                half   _Cutoff;
                float  _SmoothNormal;
                float  _WidthWorldSpace;
                half   _WidthWorld;
                float  _FadeStart;
                float  _FadeEnd;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                float3 uv3        : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  fogCoord   : TEXCOORD0;
                half   fade       : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Hard-edged low-poly meshes have split normals at every crease, which
            // tears the expanded hull apart. Baked averaged normals keep it
            // watertight, so the silhouette stays a single unbroken line.
            float3 GetExpansionNormalOS(Attributes IN)
            {
                #if defined(_SMOOTHNORMAL_TANGENT)
                    // Stored unencoded; skinned by the GPU along with the normal.
                    float3 packedTangent = IN.tangentOS.xyz;
                    if (dot(packedTangent, packedTangent) < 0.25)
                        return IN.normalOS;
                    return normalize(packedTangent);
                #elif defined(_SMOOTHNORMAL_VERTEXCOLOR)
                    // A mesh with no vertex colors is fed pure white, which decodes
                    // to a meaningless uniform diagonal and shifts the whole hull
                    // sideways instead of expanding it. Detect that and fall back.
                    float3 packed = IN.color.rgb;
                    if (all(packed > 0.999))
                        return IN.normalOS;
                    return normalize(packed * 2.0 - 1.0);
                #elif defined(_SMOOTHNORMAL_UV3)
                    // Likewise, a missing UV3 stream arrives as zero.
                    float3 packed = IN.uv3;
                    if (dot(packed, packed) < 1e-6)
                        return IN.normalOS;
                    return normalize(packed * 2.0 - 1.0);
                #else
                    return IN.normalOS;
                #endif
            }

            Varyings OutlineVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 normalOS   = GetExpansionNormalOS(IN);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = normalize(TransformObjectToWorldNormal(normalOS));

                // Distance fade (optional): 0 width past _FadeEnd.
                half fade = 1.0h;
                if (_FadeEnd > _FadeStart)
                {
                    float dist = length(GetCameraPositionWS() - positionWS);
                    fade = 1.0h - saturate((dist - _FadeStart) / max(1e-4, _FadeEnd - _FadeStart));
                }
                OUT.fade = fade;

            #if defined(_WIDTH_WORLD_SPACE)
                // World-space expansion: thickness shrinks with distance.
                positionWS += normalWS * (_WidthWorld * fade);
                float4 positionCS = TransformWorldToHClip(positionWS);
            #else
                // Screen-space expansion: exact, constant pixel thickness at any
                // distance, FOV, aspect ratio or resolution. Offsetting in clip XY
                // along the view-space normal is what keeps the line even — scaling
                // world position by clip w does not (it varies with FOV/aspect).
                float4 positionCS = TransformWorldToHClip(positionWS);
                float3 normalVS   = TransformWorldToViewDir(normalWS);

                // Normals pointing straight at/away from the camera have no stable
                // screen direction; leave those verts unexpanded instead of
                // amplifying numeric noise into speckles.
                float2 dir = normalVS.xy;
                float  len = length(dir);
                dir = len > 1e-5 ? dir / len : float2(0.0, 0.0);

                // 1 pixel = 2 / screenSize in NDC; multiply by w to survive the
                // perspective divide.
                float2 ndcOffset = dir * (_OutlineWidthPx * fade * 2.0 / _ScreenParams.xy);
                positionCS.xy += ndcOffset * positionCS.w;
            #endif

                // Push the hull slightly away from the camera so that wherever it
                // intersects the character's own front faces (concave creases, the
                // underside of a nose, overlapping parts) the front surface wins the
                // depth test. This is what removes the smudges inside the silhouette
                // while leaving the outer ring — which tests against the background —
                // fully intact.
                #if UNITY_REVERSED_Z
                    positionCS.z -= _DepthBias * positionCS.w;
                #else
                    positionCS.z += _DepthBias * positionCS.w;
                #endif

                OUT.positionCS = positionCS;
                OUT.fogCoord   = ComputeFogFactor(positionCS.z);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 OutlineFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // Fully faded verts contribute nothing.
                clip(IN.fade - 1e-4h);

                #if defined(_ALPHATEST_ON)
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a - _Cutoff);
                #endif

                half3 color = MixFog(_OutlineColor.rgb, IN.fogCoord);
                return half4(color, _OutlineColor.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
