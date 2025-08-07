// A simple lit shader for URP that uses vertex colors and supports multiple lights.
Shader "Custom/URPLitVertexColor"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }

        // Base Pass for main directional light and ambient light
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // --- Required for Main Light + Shadows ---
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            // --- Required for Additional Lights ---
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                half3  normalWS     : NORMAL;
                half4  color        : COLOR;
                float4 shadowCoord  : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionWS = positionInputs.positionWS;
                output.positionCS = positionInputs.positionCS;

                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInputs.normalWS;
                
                output.shadowCoord = GetShadowCoord(positionInputs);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // --- Main Light ---
                Light mainLight = GetMainLight(input.shadowCoord);
                half3 mainLightColor = mainLight.color * mainLight.shadowAttenuation;
                half NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 finalColor = NdotL * mainLightColor;

                // --- Additional Lights ---
                #ifdef _ADDITIONAL_LIGHTS
                    uint lightCount = GetAdditionalLightsCount();
                    for (uint i = 0u; i < lightCount; ++i)
                    {
                        Light light = GetAdditionalLight(i, input.positionWS);
                        half3 attenuatedLightColor = light.color * light.shadowAttenuation * light.distanceAttenuation;
                        NdotL = saturate(dot(input.normalWS, light.direction));
                        finalColor += NdotL * attenuatedLightColor;
                    }
                #endif
                
                // --- Final Color ---
                finalColor *= input.color.rgb;
                return half4(finalColor, input.color.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}