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
        _UseVertexColors ("Use Vertex Colors", Range(0,1)) = 1
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
                nointerpolation float3 normalWS : TEXCOORD1;  // Flat shading
                nointerpolation half4 color : COLOR;          // Flat color per face
                float3 viewDirWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Brightness;
                float _Contrast;
                float _AOStrength;
                float _RimPower;
                half4 _RimColor;
                float _UseVertexColors;
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
                // Use flat normals (nointerpolation should work now)
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // Get main light
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                float3 lightColor = mainLight.color;

                // Base color with brightness and contrast adjustment
                half4 baseColor = lerp(_Color, input.color * _Color, _UseVertexColors);
                baseColor.rgb = pow(baseColor.rgb * _Brightness, _Contrast);

                // Quantized diffuse lighting for pixelated look
                float NdotL = dot(normalWS, lightDir) * 0.5 + 0.5;  // Remap from [-1,1] to [0,1]
                
                // Quantize lighting into discrete steps for pixel art look
                NdotL = floor(NdotL * 3.0) / 3.0;  // 3 lighting levels: 0.33, 0.66, 1.0
                NdotL = max(NdotL, 0.33);  // Prevent completely black faces
                
                float3 diffuse = baseColor.rgb * lightColor * NdotL;

                // Stronger ambient to prevent black faces
                float ambientLevel = 0.6;
                float3 ambient = baseColor.rgb * unity_AmbientSky.rgb * ambientLevel;

                // Simplified AO based on face direction (flat per face)
                float ao = 1.0;
                if (abs(normalWS.y) < 0.9) // Side faces
                    ao = 1.0 - _AOStrength * 0.2;
                if (normalWS.y < -0.5) // Bottom faces
                    ao = 1.0 - _AOStrength * 0.5;
                
                // Sharp rim lighting cutoff for pixel look
                float rimFactor = 1.0 - saturate(abs(dot(viewDirWS, normalWS)));
                float rim = step(0.7, rimFactor) * _RimColor.a;  // Sharp cutoff
                float3 rimLight = rim * _RimColor.rgb;

                // Combine lighting with flat shading
                float3 finalColor = ambient * ao + diffuse + rimLight;

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