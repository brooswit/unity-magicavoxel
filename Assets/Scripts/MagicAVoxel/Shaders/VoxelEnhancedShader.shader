Shader "Custom/VoxelEnhancedShader"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0.5, 2.0)) = 1.0
        _Contrast ("Contrast", Range(0.5, 2.0)) = 1.0
        _AOStrength ("Ambient Occlusion", Range(0.0, 1.0)) = 0.3
        _RimPower ("Rim Light Power", Range(0.1, 8.0)) = 2.0
        _RimColor ("Rim Color", Color) = (1,1,1,0.5)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                float3 viewDirWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Brightness;
                float _Contrast;
                float _AOStrength;
                float _RimPower;
                half4 _RimColor;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.color = input.color * _Color;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // Normalize vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // Get main light
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                float3 lightColor = mainLight.color;

                // Base color with brightness and contrast adjustment
                half4 baseColor = input.color;
                baseColor.rgb = pow(baseColor.rgb * _Brightness, _Contrast);

                // Basic diffuse lighting
                float NdotL = saturate(dot(normalWS, lightDir));
                float3 diffuse = baseColor.rgb * lightColor * NdotL;

                // Ambient lighting with slight color shift for depth
                float3 ambient = baseColor.rgb * unity_AmbientSky.rgb * 1.2;

                // Simple ambient occlusion simulation based on normal direction
                float ao = 1.0 - _AOStrength * (1.0 - abs(dot(normalWS, float3(0, 1, 0))));
                
                // Rim lighting for edge definition
                float rimFactor = 1.0 - saturate(dot(viewDirWS, normalWS));
                float rim = pow(rimFactor, _RimPower);
                float3 rimLight = rim * _RimColor.rgb * _RimColor.a;

                // Combine lighting
                float3 finalColor = ambient * ao + diffuse + rimLight;
                
                // Subtle color enhancement for voxel art vibrancy
                finalColor = lerp(finalColor, saturate(finalColor * 1.1), 0.3);

                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
        
        // Shadow casting pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}