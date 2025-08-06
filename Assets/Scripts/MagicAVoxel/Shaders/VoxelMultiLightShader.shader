Shader "Custom/VoxelMultiLightShader"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        // Base pass - main light
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
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
                float4 color : COLOR;
                float3 worldNormal : TEXCOORD0;
            };

            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = max(0.2, dot(normal, lightDir));
                
                float3 lighting = UNITY_LIGHTMODEL_AMBIENT.xyz + _LightColor0.rgb * NdotL;
                return fixed4(i.color.rgb * lighting, i.color.a);
            }
            ENDCG
        }
        
        // Additional lights pass
        Pass
        {
            Tags { "LightMode"="ForwardAdd" }
            Blend One One
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
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
                float3 lightDir;
                float atten = 1.0;
                
                // Point light calculation
                if (_WorldSpaceLightPos0.w != 0.0)
                {
                    float3 toLight = _WorldSpaceLightPos0.xyz - i.worldPos;
                    lightDir = normalize(toLight);
                    float dist = length(toLight);
                    atten = 1.0 / (1.0 + dist * dist * 0.001);
                }
                else
                {
                    lightDir = normalize(_WorldSpaceLightPos0.xyz);
                }
                
                float NdotL = max(0, dot(normal, lightDir));
                float3 lighting = _LightColor0.rgb * NdotL * atten;
                
                return fixed4(i.color.rgb * lighting, 0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}