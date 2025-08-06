Shader "Custom/VoxelFlatLitShader"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert
        
        fixed4 _Color;
        
        struct Input
        {
            float4 color : COLOR;
        };
        
        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.color = v.color;
        }
        
        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = IN.color.rgb * _Color.rgb;
            o.Alpha = IN.color.a * _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}