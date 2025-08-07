Shader "Custom/BasicVertexColor"
{
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
                float3 lighting = float3(0.2, 0.2, 0.2); // darker ambient to see point light better
                
                // Simple fixed directional light (weaker)
                float3 lightDir = normalize(float3(0.3, -0.8, 0.5));
                float NdotL = max(0, dot(normalize(i.worldNormal), lightDir));
                lighting += float3(0.4, 0.4, 0.4) * NdotL;
                
                // VERY OBVIOUS point light test
                float3 pointLightPos = float3(0, 0, 0); // Right at world origin
                float3 toLight = pointLightPos - i.worldPos;
                float distance = length(toLight);
                
                // Distance-based color for debugging
                if (distance < 5.0)
                {
                    // Close objects get bright red
                    lighting += float3(2, 0, 0) * (5.0 - distance) / 5.0;
                }
                else if (distance < 10.0)
                {
                    // Medium distance objects get orange
                    float3 lightDirection = normalize(toLight);
                    float intensity = (10.0 - distance) / 5.0; // Linear falloff
                    float lambert = max(0.1, dot(normalize(i.worldNormal), lightDirection));
                    lighting += float3(1, 0.5, 0) * lambert * intensity;
                }
                
                return fixed4(i.color.rgb * lighting, i.color.a);
            }
            ENDCG
        }
    }
}