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
                float3 lighting = float3(0.4, 0.4, 0.4); // ambient
                
                // Simple fixed directional light
                float3 lightDir = normalize(float3(0.3, -0.8, 0.5));
                float NdotL = max(0, dot(normalize(i.worldNormal), lightDir));
                lighting += float3(0.8, 0.8, 0.7) * NdotL;
                
                // Test point light at origin
                float3 toLight = float3(0, 2, 0) - i.worldPos;
                float distance = length(toLight);
                if (distance < 8.0)
                {
                    float3 lightDirection = normalize(toLight);
                    float intensity = 1.0 / (1.0 + distance * distance * 0.05);
                    float lambert = max(0, dot(normalize(i.worldNormal), lightDirection));
                    lighting += float3(1, 0.3, 0.1) * lambert * intensity * 2.0;
                }
                
                return fixed4(i.color.rgb * lighting, i.color.a);
            }
            ENDCG
        }
    }
}