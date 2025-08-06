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
                
                // Calculate lighting using actual Unity main light
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                
                // Use Unity's main directional light
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = dot(worldNormal, lightDir);
                
                // Remap to 3 distinct levels
                if (NdotL > 0.3) o.lightLevel = 1.0;
                else if (NdotL > -0.3) o.lightLevel = 0.8;
                else o.lightLevel = 0.6;
                
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