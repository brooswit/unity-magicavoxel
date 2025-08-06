Shader "Custom/VoxelFlatLitShader"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Standard surface shader with full lighting support
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float4 color : COLOR;
        };

        half4 _Color;
        half _Metallic;
        half _Smoothness;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Use vertex colors multiplied by tint
            fixed4 c = IN.color * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}