// A simple lit shader for URP that uses vertex colors and supports multiple lights.
Shader "Custom/URPLitVertexColor"
{
    SubShader
    {
        // Base Pass for main directional light and ambient light
        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                half4  color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : NORMAL;
                half4  color      : COLOR;
                float4 shadowCoord : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionWS = positionInputs.positionWS;
                output.positionCS = positionInputs.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.shadowCoord = GetShadowCoord(positionInputs);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(input.shadowCoord);
                
                // Add ambient light
                half3 ambient = SampleSH(normalWS);
                
                // Calculate main light contribution
                half lambert = dot(normalWS, mainLight.direction);
                half3 color = (ambient + lambert * mainLight.color) * input.color.rgb;
                
                return half4(color, input.color.a);
            }
            ENDHLSL
        }

        // Additive Pass for additional lights (point lights, etc.)
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend One One // Additive blending
            ZWrite Off // Don't write to depth buffer

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                half4  color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : NORMAL;
                half4  color      : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                
                // Loop through all additional lights
                uint lightCount = GetAdditionalLightsCount();
                half3 color = 0;
                for (uint i = 0u; i < lightCount; ++i)
                {
                    // Get the light data for the current light
                    Light light = GetAdditionalLight(i, input.positionWS);
                    
                    // Calculate the lighting contribution from this light
                    half3 attenuatedLightColor = light.color * light.distanceAttenuation;
                    half NdotL = saturate(dot(normalWS, light.direction));
                    color += NdotL * attenuatedLightColor;
                }
                
                // Apply the vertex color to the final calculated light
                return half4(color * input.color.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}