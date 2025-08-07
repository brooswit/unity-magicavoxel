Shader "Custom/SimpleMultiLight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 lighting = float3(0.2, 0.2, 0.2); // ambient
                
                // Main directional light
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = max(0, dot(i.worldNormal, lightDir));
                lighting += _LightColor0.rgb * NdotL;
                
                // Check for point lights at common positions
                float3 pointLightPositions[8] = {
                    float3(0, 3, 0),
                    float3(3, 3, 3),
                    float3(-3, 3, 3),
                    float3(3, 3, -3),
                    float3(-3, 3, -3),
                    float3(0, 0, 3),
                    float3(0, 0, -3),
                    float3(5, 2, 0)
                };
                
                for (int j = 0; j < 8; j++)
                {
                    float3 toLight = pointLightPositions[j] - i.worldPos;
                    float distance = length(toLight);
                    if (distance < 10.0)
                    {
                        float3 lightDirection = normalize(toLight);
                        float intensity = 1.0 / (1.0 + distance * distance * 0.1);
                        float lambert = max(0, dot(i.worldNormal, lightDirection));
                        lighting += float3(1, 0.8, 0.6) * lambert * intensity;
                    }
                }
                
                return fixed4(i.color.rgb * lighting, i.color.a);
            }
            ENDCG
        }
    }
}