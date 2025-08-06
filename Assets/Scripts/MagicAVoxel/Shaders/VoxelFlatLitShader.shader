Shader "Custom/VoxelFlatLitShader"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                
                // Start with ambient
                float3 lighting = float3(0.2, 0.2, 0.2);
                
                // Add main directional light
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = max(0, dot(normal, lightDir));
                lighting += float3(0.8, 0.8, 0.8) * NdotL;
                
                // Simple point light approximation at fixed positions
                float3 pointLightPositions[3] = {
                    float3(5, 5, 5),
                    float3(-5, 5, 5), 
                    float3(0, 5, -5)
                };
                
                for (int j = 0; j < 3; j++)
                {
                    float3 toLight = pointLightPositions[j] - i.worldPos;
                    float dist = length(toLight);
                    if (dist < 15.0)
                    {
                        float3 pointLightDir = normalize(toLight);
                        float pointNdotL = max(0, dot(normal, pointLightDir));
                        float attenuation = 1.0 / (1.0 + dist * dist * 0.1);
                        lighting += float3(0.3, 0.3, 0.3) * pointNdotL * attenuation;
                    }
                }
                
                return fixed4(i.color.rgb * lighting, i.color.a);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}