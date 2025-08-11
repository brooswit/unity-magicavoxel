Shader "Custom/VoxelURPFlatLitPerVoxel"
{
    Properties
    {
        _VoxelSize("Voxel Size (object units)", Float) = 1.0
        _Ambient("Ambient Strength", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            // Lighting variants similar to URP Lit
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _VoxelSize;
                float _Ambient;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 voxelCenterWS : TEXCOORD0;
                float4 color : COLOR;
            };

            // Quantize object space position to voxel center (object units)
            float3 QuantizeToVoxelCenterOS(float3 posOS)
            {
                float inv = 1.0 / max(_VoxelSize, 1e-5);
                float3 idx = floor(posOS * inv);
                return (idx + 0.5) * _VoxelSize;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 centerOS = QuantizeToVoxelCenterOS(IN.positionOS.xyz);
                float3 centerWS = TransformObjectToWorld(centerOS);
                OUT.voxelCenterWS = centerWS;
                OUT.positionHCS = TransformWorldToHClip(TransformObjectToWorld(IN.positionOS.xyz));
                OUT.color = IN.color;
                return OUT;
            }

            // Compute brightness from a light using max across ±axis normals
            float3 EvaluateLightUniform(Light light)
            {
                float3 L = normalize(light.direction);
                float maxComp = max(abs(L.x), max(abs(L.y), abs(L.z)));
                return light.color * light.distanceAttenuation * light.shadowAttenuation * maxComp;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 color = IN.color.rgb;

                // Ambient term
                float3 lighting = color * _Ambient;

                // Main light (URP helper provides shadow attenuation internally)
                Light mainLight = GetMainLight();
                lighting += EvaluateLightUniform(mainLight) * color;

                // Additional lights
                #if defined(_ADDITIONAL_LIGHTS)
                // Forward+ directional loop (if enabled)
                #if defined(_CLUSTER_LIGHT_LOOP)
                UNITY_LOOP for (uint li = 0; li < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); li++)
                {
                    Light addDir = GetAdditionalLight(li, IN.voxelCenterWS, half4(1,1,1,1));
                    lighting += EvaluateLightUniform(addDir) * color;
                }
                #endif

                // Point/spot lights
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light add = GetAdditionalLight(lightIndex, IN.voxelCenterWS, half4(1,1,1,1));
                    lighting += EvaluateLightUniform(add) * color;
                LIGHT_LOOP_END
                #endif

                return half4(lighting, 1.0);
            }
            ENDHLSL
        }

        // Basic shadow caster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionHCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}


