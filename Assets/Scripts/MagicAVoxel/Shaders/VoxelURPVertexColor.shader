Shader "Custom/VoxelURPVertexColor"
{
    Properties
    {
        _Intensity ("Light Intensity", Range(0.1, 3.0)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Cull Off
        ZWrite On
        
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            HLSLPROGRAM
                        
            #pragma vertex vert
            #pragma fragment frag

            // Required multi_compile directives for URP lighting
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            
            // For Forward+ rendering path support
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 color        : COLOR;
            };
            
            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float4 color       : COLOR;
            };
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.color = IN.color;
                
                return OUT;
            }
            
            float3 MyLightingFunction(float3 normalWS, Light light, float3 vertexColor)
            {
                float NdotL = dot(normalWS, normalize(light.direction));
                NdotL = saturate(NdotL);
                
                return NdotL * light.color * light.distanceAttenuation * light.shadowAttenuation * vertexColor * _Intensity;
            }
            
            // Main lighting loop function
            float3 MyLightLoop(float3 vertexColor, InputData inputData)
            {
                float3 lighting = 0;
                
                // Add ambient lighting
                lighting += SampleSH(inputData.normalWS) * vertexColor * 0.3;
                
                // Get the main light
                Light mainLight = GetMainLight();
                lighting += MyLightingFunction(inputData.normalWS, mainLight, vertexColor);
                
                // Get additional lights (point lights, spot lights, etc.)
                #if defined(_ADDITIONAL_LIGHTS)

                // Additional light loop for non-main directional lights (Forward+ specific)
                #if defined(_CLUSTER_LIGHT_LOOP)
                UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                    lighting += MyLightingFunction(inputData.normalWS, additionalLight, vertexColor);
                }
                #endif
                
                // Additional light loop for point/spot lights
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                    lighting += MyLightingFunction(inputData.normalWS, additionalLight, vertexColor);
                LIGHT_LOOP_END
                
                #endif
                
                return lighting;
            }
            
            half4 frag(Varyings input) : SV_Target0
            {
                // InputData struct required for Forward+ light loop
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = input.normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                
                float3 lighting = MyLightLoop(input.color.rgb, inputData);
                
                half4 finalColor = half4(lighting, input.color.a);
                
                return finalColor;
            }
            
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}