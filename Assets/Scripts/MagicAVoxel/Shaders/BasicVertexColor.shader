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
                // DIAGNOSTIC: Show world position as color
                // This will tell us if world position calculation is working at all
                
                float3 worldPos = i.worldPos;
                
                // Convert world position to visible colors
                // X axis = red channel, Y axis = green channel, Z axis = blue channel
                float3 posColor = abs(worldPos) * 0.1; // Scale down to 0-1 range
                posColor = frac(posColor); // Keep only fractional part for cycling colors
                
                // Also show distance from origin as brightness
                float distance = length(worldPos);
                float brightness = 1.0 - saturate(distance * 0.1);
                
                // If very close to origin (within 2 units), override with pure red
                if (distance < 2.0)
                {
                    return fixed4(1, 0, 0, 1); // PURE RED
                }
                
                // Otherwise show position-based colors
                return fixed4(posColor * brightness, 1);
            }
            ENDCG
        }
    }
}