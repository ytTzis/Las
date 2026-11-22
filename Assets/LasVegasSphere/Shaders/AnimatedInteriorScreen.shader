Shader "LasVegasSphere/AnimatedInteriorScreen"
{
    Properties
    {
        [NoScaleOffset] _VideoTex ("Spherical Video", 2D) = "black" {}
        _UseVideo ("Use Video", Range(0, 1)) = 0
        _VideoBrightness ("Video Brightness", Range(0, 4)) = 1
        _FlipVideoY ("Flip Video Vertically", Range(0, 1)) = 0
        _VideoProjectionMode ("Video Projection Mode", Range(0, 2)) = 0
        _VisibleDomeAngle ("Visible Dome Angle", Range(30, 180)) = 77.37
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
            sampler2D _VideoTex;
            float _UseVideo, _VideoBrightness, _FlipVideoY;
            float _VideoProjectionMode, _VisibleDomeAngle;
            float _Brightness, _Density, _Speed;
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            fixed4 frag(v2f i) : SV_Target
            {
                // 0: stretch a normal rectangular frame across the visible dome.
                // 1: sample the physically correct upper portion of a 2:1 360 video.
                // 2: unwrap a square 180-degree fulldome/fisheye master.
                float2 videoUV = i.uv;
                if (_VideoProjectionMode > 1.5)
                {
                    float phi = (i.uv.x - 0.5) * 6.2831853;
                    float radius = (1.0 - i.uv.y) * 0.5;
                    videoUV = 0.5 + float2(sin(phi), -cos(phi)) * radius;
                }
                else if (_VideoProjectionMode > 0.5)
                {
                    float polarDegrees = (1.0 - i.uv.y) * _VisibleDomeAngle;
                    videoUV = float2(i.uv.x, 1.0 - polarDegrees / 180.0);
                }
                videoUV.y = lerp(videoUV.y, 1.0 - videoUV.y, _FlipVideoY);
                fixed4 videoColor = tex2D(_VideoTex, videoUV) * _VideoBrightness;

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
                fixed4 fallbackColor = fixed4(background + palette * glyph * scan * _Brightness, 1);
                return lerp(fallbackColor, fixed4(videoColor.rgb, 1), _UseVideo);
            }
            ENDCG
        }
    }
}
