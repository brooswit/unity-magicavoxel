Shader "Custom/VoxelEnhancedShader"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0.5, 2.0)) = 1.0
        _Contrast ("Contrast", Range(0.5, 2.0)) = 1.0
        _AOStrength ("Ambient Occlusion", Range(0.0, 1.0)) = 0.3
        _RimPower ("Rim Light Power", Range(0.1, 8.0)) = 2.0
        _RimColor ("Rim Color", Color) = (1,1,1,0.5)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                fixed4 color : COLOR;
                float3 viewDir : TEXCOORD2;
                SHADOW_COORDS(3)
            };

            fixed4 _Color;
            float _Brightness;
            float _Contrast;
            float _AOStrength;
            float _RimPower;
            fixed4 _RimColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color * _Color;
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                TRANSFER_SHADOW(o)
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Normalize vectors
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.viewDir);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                // Base color with brightness and contrast adjustment
                fixed4 baseColor = i.color;
                baseColor.rgb = pow(baseColor.rgb * _Brightness, _Contrast);

                // Basic diffuse lighting
                float NdotL = max(0, dot(normal, lightDir));
                float3 diffuse = baseColor.rgb * _LightColor0.rgb * NdotL;

                // Ambient lighting with slight color shift for depth
                float3 ambient = baseColor.rgb * UNITY_LIGHTMODEL_AMBIENT.rgb * 1.2;

                // Simple ambient occlusion simulation based on normal direction
                float ao = 1.0 - _AOStrength * (1.0 - abs(dot(normal, float3(0, 1, 0))));
                
                // Rim lighting for edge definition
                float rimFactor = 1.0 - saturate(dot(viewDir, normal));
                float rim = pow(rimFactor, _RimPower);
                float3 rimLight = rim * _RimColor.rgb * _RimColor.a;

                // Shadow attenuation
                float shadow = SHADOW_ATTENUATION(i);

                // Combine lighting
                float3 finalColor = ambient * ao + (diffuse * shadow) + rimLight;
                
                // Subtle color enhancement for voxel art vibrancy
                finalColor = lerp(finalColor, saturate(finalColor * 1.1), 0.3);

                return fixed4(finalColor, baseColor.a);
            }
            ENDCG
        }
        
        // Shadow casting pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f {
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "VertexLit"
}