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
                
                // Calculate distance-based lighting from main light position
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                // For directional lights, use a simple distance-based falloff
                // For point lights, calculate actual distance
                float lightDistance = length(_WorldSpaceLightPos0.xyz - worldPos);
                
                // If w component is 0, it's a directional light
                if (_WorldSpaceLightPos0.w == 0.0) {
                    // Directional light - use position-based variation
                    lightDistance = length(worldPos) * 0.1; // Scale factor for variation
                }
                
                // Convert distance to light levels (closer = brighter)
                float lightIntensity = 1.0 / (1.0 + lightDistance * 0.5);
                
                // Quantize to discrete levels
                if (lightIntensity > 0.8) o.lightLevel = 1.0;
                else if (lightIntensity > 0.6) o.lightLevel = 0.8;
                else if (lightIntensity > 0.4) o.lightLevel = 0.6;
                else o.lightLevel = 0.4;
                
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