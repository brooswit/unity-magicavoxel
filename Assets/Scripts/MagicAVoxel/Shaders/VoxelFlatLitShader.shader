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
            Tags { "LightMode"="ForwardBase" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                nointerpolation float4 color : COLOR;     // Flat color per face
                nointerpolation float3 normal : NORMAL;   // Flat normal per face
            };

            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Simple flat lighting - each face gets one lighting value
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = dot(i.normal, lightDir);
                
                // Remap to avoid pure black faces
                float lightIntensity = NdotL * 0.4 + 0.6;  // Range from 0.6 to 1.0
                
                // Apply lighting to vertex color
                fixed4 finalColor = i.color * lightIntensity;
                
                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "VertexLit"
}