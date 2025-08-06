// A simple lit shader for the Universal Render Pipeline that uses vertex colors.
// It supports multiple lights, shadows, and instancing.
Shader "Custom/URPVertexColorLit"
{
    Properties
    {
        _Color("Color Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            // This is the main pass that handles lighting and shadows.
            // It's configured to work with URP's forward renderer.
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // -- URP Includes --
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // -- C# to Shader Interface --
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            // -- Input/Output Structs --
            // The 'Attributes' struct matches the data coming from the mesh.
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                half4  color        : COLOR; // Vertex color from the mesh
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // The 'Varyings' struct passes data from the vertex to the fragment shader.
            struct Varyings
            {
                float4 positionCS      : SV_POSITION;
                float3 positionWS      : TEXCOORD0;
                half3  normalWS        : TEXCOORD1;
                half4  color           : COLOR;
                
                // Fog and Shadow coordinates are required for URP lighting.
                half3 fogFactorAndVertexLight : TEXCOORD2;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord : TEXCOORD3;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // -- Vertex Shader --
            // This function processes each vertex of the mesh.
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // Transform vertex data from object space to world space and then to clip space.
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionWS = positionInputs.positionWS;
                output.positionCS = positionInputs.positionCS;
                
                // Transform normals for lighting calculations.
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInputs.normalWS;

                // Pass vertex color to fragment shader, tinted by the material color.
                output.color = input.color * _Color;
                
                // Get lighting information for this vertex.
                // This includes vertex lights and main light shadows.
                float3 vertexLight = GetVertexLight(output.positionWS, normalInputs.normalWS);
                
                // Get fog information.
                half fogFactor = ComputeFogFactor(output.positionCS.z);
                
                output.fogFactorAndVertexLight = half3(fogFactor, vertexLight);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = GetShadowCoord(positionInputs);
                #endif

                return output;
            }

            // -- Fragment Shader --
            // This function determines the final color of each pixel.
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // Get the main light and additional lights affecting this fragment.
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    Light mainLight = GetMainLight(input.shadowCoord);
                #else
                    Light mainLight = GetMainLight();
                #endif
                
                // Get the base color from vertex color.
                half4 albedo = input.color;

                // Calculate the final lighting by combining all light sources.
                half3 finalLighting = 0;
                
                // Main light contribution.
                finalLighting += mainLight.color * mainLight.attenuation;

                // Additional lights contribution.
                int additionalLightsCount = GetAdditionalLightsCount();
                for (int i = 0; i < additionalLightsCount; ++i)
                {
                    Light additionalLight = GetAdditionalLight(i, input.positionWS);
                    finalLighting += additionalLight.color * additionalLight.attenuation;
                }
                
                // Add vertex lights (lights baked into vertices).
                finalLighting += input.fogFactorAndVertexLight.yzz;

                // Combine base color with the calculated lighting.
                half3 finalColor = albedo.rgb * finalLighting;
                
                // Apply fog to the final color.
                finalColor = MixFog(finalColor, input.fogFactorAndVertexLight.x);

                return half4(finalColor, albedou.a);
            }
            ENDHLSL
        }

        // This pass is used for casting shadows.
        // It's a minimal pass that outputs depth information.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // URP shadow caster include.
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}