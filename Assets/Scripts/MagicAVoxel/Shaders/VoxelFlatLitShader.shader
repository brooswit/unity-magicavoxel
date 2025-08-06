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
                nointerpolation float4 color : COLOR;
                nointerpolation float lightLevel : TEXCOORD0;
            };

            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                
                // Simple directional lighting that responds to scene lights
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                
                // Basic dot product lighting
                float NdotL = dot(worldNormal, lightDir);
                
                // Convert to simple 3-level system with good visibility
                if (NdotL > 0.2) o.lightLevel = 1.0;        // Facing light
                else if (NdotL > -0.5) o.lightLevel = 0.75;  // Side faces  
                else o.lightLevel = 0.6;                     // Away from light
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color * i.lightLevel;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}