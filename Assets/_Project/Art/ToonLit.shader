Shader "Custom/ToonLit"
{
    Properties
    {
        [Header(Base)]
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        [Header(Surface)]
        // Set together by ToonLitShaderGUI's Surface Type dropdown, but left
        // editable so the material also works with the stock inspector.
        [HideInInspector] _Surface ("__surface", Float) = 0
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip (cutout)", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1   // One
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0   // Zero
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2             // Back
        [ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadows ("Receive Shadows", Float) = 1

        [Header(Shading Normal)]
        // Blends the real normal toward a sphere centered inside the mesh. The
        // cel terminator follows the normal field, so a low-poly field makes it
        // kink along triangle edges; a spherical field makes it a clean curve.
        // Silhouette, outline and texture are untouched.
        [Toggle(_SPHERICAL_NORMAL)] _UseSphericalNormal ("Smooth Shading Normal", Float) = 0
        _SphereNormalBlend ("Sphere Blend", Range(0,1)) = 0.6
        _SphereCenterOS ("Sphere Center (object space)", Vector) = (0,0,0,0)

        [Header(Toon Shading)]
        _ShadowColor ("Shadow Tint", Color) = (0.6,0.6,0.7,1)
        _ShadowThreshold ("Shadow Threshold", Range(0,1)) = 0.5
        _ShadowSmoothness ("Surface Edge Smoothness", Range(0.001,0.5)) = 0.02
        // Cast shadows get their own width: banding them as hard as the surface
        // terminator throws away URP's filtering and exposes shadow-map stair steps.
        _ShadowSoftness ("Cast Shadow Softness", Range(0,1)) = 0.35
        _AmbientStrength ("Ambient (SH) Strength", Range(0,1)) = 0.2

        [Header(Specular)]
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecSize ("Specular Size", Range(0.001,1)) = 0.1
        _SpecSmoothness ("Specular Edge Smoothness", Range(0.001,0.5)) = 0.02

        [Header(Rim Light)]
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimAmount ("Rim Amount", Range(0,1)) = 0.7
        _RimThreshold ("Rim Threshold", Range(0.001,1)) = 0.1
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
        LOD 300

        // Shared material CBUFFER - identical layout across every pass (SRP Batcher requirement)
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            float  _Surface;
            half   _AlphaClip;
            half   _Cutoff;
            float  _SrcBlend;
            float  _DstBlend;
            float  _ZWrite;
            float  _Cull;
            half   _ReceiveShadows;
            float  _UseSphericalNormal;
            half   _SphereNormalBlend;
            float4 _SphereCenterOS;
            half4  _ShadowColor;
            half   _ShadowThreshold;
            half   _ShadowSmoothness;
            half   _ShadowSoftness;
            half   _AmbientStrength;
            half4  _SpecularColor;
            half   _SpecSize;
            half   _SpecSmoothness;
            half4  _RimColor;
            half   _RimAmount;
            half   _RimThreshold;
        CBUFFER_END
        ENDHLSL

        // ---------- Main Toon Lit ----------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _SPHERICAL_NORMAL

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            // Per-pixel only: this shader has no vertex-light path, so compiling
            // the _ADDITIONAL_LIGHTS_VERTEX variant produced identical output.
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            // URP 17 (Unity 6) renamed the Forward+ keyword; _FORWARD_PLUS is
            // dead and silently costs you every additional light on this object.
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float  fogCoord   : TEXCOORD3;
                #if defined(_SPHERICAL_NORMAL)
                float3 positionOS : TEXCOORD5;
                #endif
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord: TEXCOORD4;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs  = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS   = normInputs.normalWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogCoord   = ComputeFogFactor(posInputs.positionCS.z);
                #if defined(_SPHERICAL_NORMAL)
                OUT.positionOS = IN.positionOS.xyz;
                #endif
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                OUT.shadowCoord = GetShadowCoord(posInputs);
                #endif
                return OUT;
            }

            // Anti-aliased cel step.
            //
            // A plain smoothstep with a tiny width is a binary cut, so the
            // boundary lands wherever the interpolated value crosses the
            // threshold -- on low-poly geometry that path is straight within
            // each triangle, which reads as angular, chunky patches. Widening
            // the ramp by the value's screen-space derivative keeps the
            // transition about one pixel wide no matter the distance or
            // resolution: still a crisp toon edge, but a clean one. The
            // authored smoothness stays available as a floor for a deliberately
            // softer look.
            half CelStep(half value, half threshold, half smoothness)
            {
                half aa = fwidth(value) * 0.5h;
                half width = max(smoothness, aa);
                return smoothstep(threshold - width, threshold + width, value);
            }

            // Surface shading: the cel terminator across a lit form.
            half BandSurface(half value)
            {
                return CelStep(value, _ShadowThreshold, _ShadowSmoothness);
            }

            // Cast shadows: same threshold, but a widened ramp so the filtered
            // gradient URP produces survives instead of being re-quantized into
            // the shadow map's texel staircase. Softness 0 == crisp.
            half BandShadow(half atten)
            {
                return CelStep(atten, _ShadowThreshold,
                               max(_ShadowSmoothness, _ShadowSoftness * 0.5h));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                #if defined(_ALPHATEST_ON)
                    clip(albedo.a - _Cutoff);
                #endif

                float3 normalWS  = NormalizeNormalPerPixel(IN.normalWS);

                // Swap in a smoother normal field for lighting only. Everything
                // else — silhouette, depth, outline — still uses the real mesh.
                #if defined(_SPHERICAL_NORMAL)
                {
                    float3 dirOS = IN.positionOS - _SphereCenterOS.xyz;
                    if (dot(dirOS, dirOS) > 1e-8)
                    {
                        float3 sphereWS = normalize(TransformObjectToWorldNormal(normalize(dirOS)));
                        normalWS = normalize(lerp(normalWS, sphereWS, _SphereNormalBlend));
                    }
                }
                #endif

                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float2 screenUV  = GetNormalizedScreenSpaceUV(IN.positionCS);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord = IN.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                #else
                    float4 shadowCoord = float4(0, 0, 0, 0);
                #endif

                Light mainLight = GetMainLight(shadowCoord);
                float3 lightDir = normalize(mainLight.direction);

                half shadowAtten = mainLight.shadowAttenuation;
                #if defined(_RECEIVE_SHADOWS_OFF)
                    shadowAtten = 1.0h;
                #endif

                // Screen-space AO, so the object reacts to the SSAO renderer
                // feature it already writes normals for.
                half directOcclusion   = 1.0h;
                half indirectOcclusion = 1.0h;
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(screenUV);
                    directOcclusion   = aoFactor.directAmbientOcclusion;
                    indirectOcclusion = aoFactor.indirectAmbientOcclusion;
                #endif

                // Half-lambert remap so threshold 0.5 sits on the real terminator
                half NdotL   = dot(normalWS, lightDir) * 0.5 + 0.5;
                half atten   = shadowAtten * mainLight.distanceAttenuation;
                half toonDiffuse = BandSurface(NdotL) * BandShadow(atten) * directOcclusion;

                half3 shadeCol = albedo.rgb * _ShadowColor.rgb;
                half3 lightCol = albedo.rgb * mainLight.color;
                half3 litColor = lerp(shadeCol, lightCol, toonDiffuse);
                litColor += albedo.rgb * SampleSH(normalWS) * _AmbientStrength * indirectOcclusion;

                float3 halfVec = normalize(lightDir + viewDirWS);
                half NdotH = saturate(dot(normalWS, halfVec));
                half specIntensity = CelStep(NdotH, 1 - _SpecSize, _SpecSmoothness) * toonDiffuse;
                half3 specular = specIntensity * _SpecularColor.rgb * mainLight.color;

                half rimDot = 1 - saturate(dot(viewDirWS, normalWS));
                half rimIntensity = CelStep(rimDot, _RimAmount, _RimThreshold) * toonDiffuse;
                half3 rim = rimIntensity * _RimColor.rgb;

                half3 additionalLightsColor = 0;
                #if defined(_ADDITIONAL_LIGHTS)
                    // LIGHT_LOOP_BEGIN needs these populated on Forward+/clustered.
                    InputData inputData = (InputData)0;
                    inputData.positionWS = IN.positionWS;
                    inputData.normalWS = normalWS;
                    inputData.viewDirectionWS = viewDirWS;
                    inputData.normalizedScreenSpaceUV = screenUV;

                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light addLight = GetAdditionalLight(lightIndex, IN.positionWS, half4(1,1,1,1));
                        half addShadow = addLight.shadowAttenuation;
                        #if defined(_RECEIVE_SHADOWS_OFF)
                            addShadow = 1.0h;
                        #endif
                        half addNdotL = dot(normalWS, addLight.direction) * 0.5 + 0.5;
                        half addAtten = addShadow * addLight.distanceAttenuation;
                        additionalLightsColor += addLight.color * albedo.rgb
                                               * BandSurface(addNdotL) * BandShadow(addAtten) * directOcclusion;
                    LIGHT_LOOP_END
                #endif

                half3 finalColor = litColor + specular + rim + additionalLightsColor;
                finalColor = MixFog(finalColor, IN.fogCoord);

                // Alpha reaches the framebuffer only when the material is set up
                // for blending; with the opaque default the Dst factor drops it.
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }

        // ---------- Shadow Caster ----------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma target 3.0
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            // Cutout must be applied here too, or a clipped mesh casts a solid
            // shadow that does not match what is drawn.
            half4 ShadowFrag(Varyings IN) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // ---------- Depth Only ----------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ZTest LEqual
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma target 3.0
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 DepthFrag(Varyings IN) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // ---------- Depth Normals (feeds _CameraNormalsTexture: SSAO, outlines, decals) ----------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma target 3.0
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float2 uv         : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthNormalsVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 DepthNormalsFrag(Varyings IN) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif

                float3 normalWS = NormalizeNormalPerPixel(IN.normalWS);

                // URP writes the RAW world normal here (the buffer is signed);
                // remapping to 0..1 is only correct for the deferred OCT path,
                // and getting it wrong quietly breaks SSAO and decals.
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                    half2 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
                    return half4(packedNormalWS, 0.0, 0.0);
                #else
                    return half4(normalWS, 0.0);
                #endif
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "ToonLitShaderGUI"
}
