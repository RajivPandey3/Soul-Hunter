// Draw calls (owner, 2026-10-15: "sab ko single mesh or single material mein convert karo"): the props and pickups
// (Soul Lantern, castle wall, Rune Plate, rail cart, chest, Soul Gems and Soul Coins) were built from parts with their
// own URP Lit material each: no textures, only base colour, metallic, smoothness and emission differed. PropMeshMerger
// bakes those four into the mesh (vertex colour = linear base colour, UV1 = linear emission, UV2.xy = metallic and
// smoothness) so a whole prop is one mesh drawn with one shared material of this shader, lit by URP's own PBR function
// exactly as URP Lit lit each part.
// UV2.z marks parts a script recolours (0 none, 1 = mask A, 2 = mask B): ChestLook's iron bands (A) and soul seam (B),
// PressurePlate's runes (A). _MaskX_Base / _MaskX_Emission with alpha 1 replace that part's colour / glow, and
// _MaskX_Dim multiplies both, set through a property block as the scripts did on the old materials.
Shader "SoulHunter/PropMerged"
{
    Properties
    {
        _BaseColor ("Colour multiplier", Color) = (1,1,1,1)
        _MaskA_Base ("Mask A colour (alpha = use)", Color) = (0,0,0,0)
        _MaskA_Emission ("Mask A glow (alpha = use)", Color) = (0,0,0,0)
        _MaskA_Dim ("Mask A brightness", Float) = 1
        _MaskB_Base ("Mask B colour (alpha = use)", Color) = (0,0,0,0)
        _MaskB_Emission ("Mask B glow (alpha = use)", Color) = (0,0,0,0)
        _MaskB_Dim ("Mask B brightness", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _MaskA_Base;
            half4 _MaskA_Emission;
            half4 _MaskB_Base;
            half4 _MaskB_Emission;
            half _MaskA_Dim;
            half _MaskB_Dim;
            half _Cull;
        CBUFFER_END

        // The colour and glow of one vertex after the masks.
        void ApplyMasks(half mask, inout half3 albedo, inout half3 glow)
        {
            half a = step(0.5h, mask) * step(mask, 1.5h);
            half b = step(1.5h, mask);
            albedo = lerp(albedo, _MaskA_Base.rgb, a * _MaskA_Base.a);
            glow = lerp(glow, _MaskA_Emission.rgb, a * _MaskA_Emission.a);
            albedo = lerp(albedo, _MaskB_Base.rgb, b * _MaskB_Base.a);
            glow = lerp(glow, _MaskB_Emission.rgb, b * _MaskB_Emission.a);
            half dim = lerp(1.0h, _MaskA_Dim, a) * lerp(1.0h, _MaskB_Dim, b);
            albedo *= dim;
            glow *= dim;
        }
        ENDHLSL

        Pass
        {
            // URP Lit's own forward pass (LitForwardPass.hlsl, URP 17) step for step, except that the surface comes
            // from the mesh (colour, glow, metallic, smoothness, mask) instead of textures and material numbers.
            // Lighting, GI (OUTPUT_SH4 / SAMPLE_GI, probe volumes), reflections, shadows and fog are URP Lit's, with
            // its keywords. Props are never lightmapped, so TEXCOORD1/2 carry the glow and the surface.
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex PropVertex
            #pragma fragment PropFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_fog
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float3 emission : TEXCOORD1;
                float4 surface : TEXCOORD2; // x metallic, y smoothness, z mask
            };

            struct Varyings
            {
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half3 albedo : TEXCOORD3;
                half3 emission : TEXCOORD4;
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                half4 fogFactorAndVertexLight : TEXCOORD5;
            #else
                half fogFactor : TEXCOORD5;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord : TEXCOORD6;
            #endif
                half2 surface : TEXCOORD7;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);
            #ifdef USE_APV_PROBE_OCCLUSION
                float4 probeOcclusion : TEXCOORD10;
            #endif
                float4 positionCS : SV_POSITION;
            };

            Varyings PropVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
                half fogFactor = 0;
            #if !defined(_FOG_FRAGMENT)
                fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
            #endif
                output.normalWS = normalInput.normalWS;
                half3 albedo = input.color.rgb * _BaseColor.rgb;
                half3 glow = input.emission;
                ApplyMasks(input.surface.z, albedo, glow);
                output.albedo = albedo;
                output.emission = glow;
                output.surface = input.surface.xy;
                OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
            #else
                output.fogFactor = fogFactor;
            #endif
                output.positionWS = vertexInput.positionWS;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(vertexInput);
            #endif
                output.positionCS = vertexInput.positionCS;
                return output;
            }

            void PropFragment(Varyings input, out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out float4 outRenderingLayers : SV_Target1
            #endif
            )
            {
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = input.albedo;
                surfaceData.metallic = input.surface.x;
                surfaceData.smoothness = input.surface.y;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.occlusion = 1;
                surfaceData.emission = input.emission;
                surfaceData.alpha = 1;
                surfaceData.normalTS = half3(0, 0, 1);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                inputData.shadowCoord = input.shadowCoord;
            #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
            #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
            #endif
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
                inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
            #else
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
            #endif
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
            #if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
                inputData.bakedGI = SAMPLE_GI(input.vertexSH, GetAbsolutePositionWS(inputData.positionWS), inputData.normalWS,
                    inputData.viewDirectionWS, input.positionCS.xy, input.probeOcclusion, inputData.shadowMask);
            #else
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
            #endif

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                outColor = half4(color.rgb, 1);
            #ifdef _WRITE_RENDERING_LAYERS
                uint renderingLayers = GetMeshRenderingLayer();
                outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
            #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };

            float4 vert(Attributes v) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct Attributes { float4 positionOS : POSITION; };
            float4 vert(Attributes v) : SV_POSITION { return TransformObjectToHClip(v.positionOS.xyz); }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 frag(Varyings i) : SV_Target { return half4(NormalizeNormalPerPixel(i.normalWS), 0); }
            ENDHLSL
        }
    }
}
