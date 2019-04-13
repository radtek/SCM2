Shader "SCM/TerrainBlend" {
    Properties {
        _Tex1("Tex1", 2D) = "" {}
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Lambert

        sampler2D _Tex1;

        struct Input {
            float2 uv_Tex1;
        };

        void surf (Input IN, inout SurfaceOutput o) {
            float4 c1 = tex2D(_Tex1, IN.uv_Tex1);
            o.Albedo = c1;
            o.Alpha = 0.5;
        }
        ENDCG
    } 
    FallBack "Diffuse"
}