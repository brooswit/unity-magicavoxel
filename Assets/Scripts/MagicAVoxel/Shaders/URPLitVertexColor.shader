// A simple lit shader for URP that uses vertex colors.
Shader "Custom/URPLitVertexColor"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "LightMode"="UniversalForward" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
                half3  normalWS   : NORMAL;
                half4  color      : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                half lambert = dot(normalWS, mainLight.direction);
                half3 color = lambert * mainLight.color;
                return half4(input.color.rgb * color, input.color.a);
            }
            ENDHLSL
        }
    }
}