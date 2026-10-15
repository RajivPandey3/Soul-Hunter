// The horde drawn from vertex animation textures with GPU instancing (2026-10-15, horde drawing groundwork). Each body
// of a model is one instance of a static mesh (HordeVatBaker); the vertex stage reads its pose from _VatPositions /
// _VatNormals, so ~300 bodies need no skinning and no Animator. The look is SoulHunter/HordeMerged's: model colours in
// the vertex colours, glow in UV1, _Color / _TintAmount for a type's tint, lit 0.38 ambient + 0.62 direct with shadows.
//
// Per-instance data comes from UNITY_INSTANCING_BUFFER, filled by MaterialPropertyBlock arrays of the same names
// (SetVectorArray / SetFloatArray) passed in RenderParams.matProps to Graphics.RenderMeshInstanced: that is the route
// RenderMeshInstanced and DrawMeshInstanced both feed. A StructuredBuffer indexed by unity_InstanceID would also work,
// but it needs a GraphicsBuffer per draw and its own index offset per 1023-instance batch; the arrays need neither.
// Because the per-instance properties live outside UnityPerMaterial, this shader is not SRP Batcher compatible when
// drawn by ordinary MeshRenderers. It is meant only for instanced draws, where the SRP Batcher does not apply.
//
// Per instance:
//   _VatClip   (start frame, frame count, fps, loop 1/0)          from HordeVatAsset.Clip.ShaderVector
//   _VatPlay   (start time, speed, phase 0-1 of the clip, unused)  start time is in _HordeVatTime's clock
//   _Color, _TintAmount, _FlashAmount
// Global (set once a frame by the runtime drawer): _HordeVatTime, game time in seconds. Game time, not _Time, so a pause
// or a level-up screen stops the horde with the game; a frozen enemy is speed 0.
Shader "SoulHunter/HordeVat"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _TintAmount ("Tint amount (0 = model colours)", Range(0,1)) = 0
        _FlashAmount ("Hit flash amount", Range(0,1)) = 0
        _FlashColor ("Hit flash colour", Color) = (1.8,1.8,1.8,1)
        _EmissionColor ("Extra glow", Color) = (0,0,0,0)
        _BaseMap ("Texture (optional)", 2D) = "white" {}
        [NoScaleOffset] _VatPositions ("VAT positions", 2D) = "black" {}
        [NoScaleOffset] _VatNormals ("VAT normals", 2D) = "black" {}
        _VatWidth ("VAT width (vertices per row)", Float) = 1
        _VatRowsPerFrame ("VAT rows per frame", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _FlashColor;
            half4 _EmissionColor;
            float _VatWidth;
            float _VatRowsPerFrame;
        CBUFFER_END

        UNITY_INSTANCING_BUFFER_START(HordeVatProps)
            UNITY_DEFINE_INSTANCED_PROP(float4, _VatClip)
            UNITY_DEFINE_INSTANCED_PROP(float4, _VatPlay)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(float, _TintAmount)
            UNITY_DEFINE_INSTANCED_PROP(float, _FlashAmount)
        UNITY_INSTANCING_BUFFER_END(HordeVatProps)

        float _HordeVatTime;

        TEXTURE2D(_VatPositions);
        TEXTURE2D(_VatNormals);

        // Texel of vertex `index` in baked frame `frame` (see HordeVatAsset for the layout).
        int2 VatTexel(uint index, uint frame)
        {
            uint width = (uint)max(_VatWidth, 1.0);
            uint rows = (uint)max(_VatRowsPerFrame, 1.0);
            return int2(index % width, frame * rows + index / width);
        }

        // The instance's pose for this vertex: two baked frames blended, in the model root's space.
        void VatPose(float vertexIndex, out float3 positionOS, out float3 normalOS)
        {
            float4 clip = UNITY_ACCESS_INSTANCED_PROP(HordeVatProps, _VatClip);
            float4 play = UNITY_ACCESS_INSTANCED_PROP(HordeVatProps, _VatPlay);
            float frameCount = max(clip.y, 1.0);
            float f = (_HordeVatTime - play.x) * play.y * clip.z + play.z * frameCount;
            float f0, f1, blend;
            if (clip.w > 0.5)
            {
                f = f - frameCount * floor(f / frameCount); // wraps negative times too
                f0 = floor(f);
                f1 = f0 + 1.0;
                if (f1 >= frameCount) f1 = 0.0; // a loop has no repeated last frame
            }
            else
            {
                f = clamp(f, 0.0, frameCount - 1.0); // a one-shot (Death) holds its last frame
                f0 = floor(f);
                f1 = min(f0 + 1.0, frameCount - 1.0);
            }
            blend = f - f0;
            uint index = (uint)(vertexIndex + 0.5);
            int2 t0 = VatTexel(index, (uint)(clip.x + f0));
            int2 t1 = VatTexel(index, (uint)(clip.x + f1));
            positionOS = lerp(LOAD_TEXTURE2D_LOD(_VatPositions, t0, 0).xyz, LOAD_TEXTURE2D_LOD(_VatPositions, t1, 0).xyz, blend);
            normalOS = normalize(lerp(LOAD_TEXTURE2D_LOD(_VatNormals, t0, 0).xyz, LOAD_TEXTURE2D_LOD(_VatNormals, t1, 0).xyz, blend));
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 glow : TEXCOORD1;
                float2 vertexIndex : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half3 albedo : TEXCOORD2;
                half3 glow : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float fog : TEXCOORD5;
            };

            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                float3 positionOS, normalOS;
                VatPose(v.vertexIndex.x, positionOS, normalOS);
                o.positionWS = TransformObjectToWorld(positionOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(normalOS);

                half4 tint = UNITY_ACCESS_INSTANCED_PROP(HordeVatProps, _Color);
                half tintAmount = UNITY_ACCESS_INSTANCED_PROP(HordeVatProps, _TintAmount);
                half flash = UNITY_ACCESS_INSTANCED_PROP(HordeVatProps, _FlashAmount);
                // As HordeMerged: at _TintAmount 0, _Color scales the model's colours; at 1 the body is _Color.
                half3 albedo = lerp(v.color.rgb * tint.rgb, tint.rgb, tintAmount);
                o.albedo = lerp(albedo, _FlashColor.rgb, flash);
                o.glow = v.glow + _EmissionColor.rgb;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 albedo = i.albedo * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(normalize(i.normalWS), light.direction));
                half3 rgb = albedo * (0.38h + 0.62h * ndl * light.shadowAttenuation) * light.color + i.glow;
                return half4(MixFog(rgb, i.fog), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 vertexIndex : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert(Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float3 positionOS, normalOS;
                VatPose(v.vertexIndex.x, positionOS, normalOS);
                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(normalOS);
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
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 vertexIndex : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert(Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float3 positionOS, normalOS;
                VatPose(v.vertexIndex.x, positionOS, normalOS);
                return TransformObjectToHClip(positionOS);
            }

            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
