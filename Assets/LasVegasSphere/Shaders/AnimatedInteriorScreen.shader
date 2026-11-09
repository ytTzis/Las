Shader "LasVegasSphere/AnimatedInteriorScreen"
{
    Properties
    {
        _Brightness ("Brightness", Range(0, 6)) = 2.4
        _Density ("Symbol Density", Range(10, 120)) = 58
        _Speed ("Scroll Speed", Range(0, 2)) = 0.32
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        // Mesh winding and normals face the audience inside the spherical screen.
        Cull Back
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            float _Brightness, _Density, _Speed;
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * float2(_Density * 1.65, _Density);
                p.y += _Time.y * _Speed * (1.0 + fmod(floor(p.x), 7.0) * 0.08);
                float2 cell = floor(p);
                float2 local = frac(p);
                float rnd = hash21(cell);
                float glyph = step(0.18, local.x) * step(local.x, 0.82) * step(0.13, local.y) * step(local.y, 0.86);
                glyph *= step(0.32, frac(local.x * (2.0 + floor(rnd * 4.0)) + rnd));
                float3 palette = 0.55 + 0.45 * cos(6.28318 * (rnd + float3(0.0, 0.33, 0.67)));
                float3 background = float3(0.003, 0.006, 0.018);
                float scan = 0.75 + 0.25 * sin(i.uv.y * 900.0);
                return fixed4(background + palette * glyph * scan * _Brightness, 1);
            }
            ENDCG
        }
    }
}
