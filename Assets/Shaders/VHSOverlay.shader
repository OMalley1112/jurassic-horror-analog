Shader "AnalogHorror/VHSOverlay"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Glitch ("Glitch Amount", Range(0, 1)) = 0
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Glitch;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float Rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 centered = uv - 0.5;
                float t = _Time.y;

                // --- horizontal tracking tear (strong during glitch bursts) ---
                float row = floor(uv.y * 70.0 + t * 24.0);
                float rowRand = Rand(float2(row, floor(t * 18.0)));
                float tear = (rowRand - 0.5) * 0.05 * _Glitch;

                // constant subtle tape wobble
                tear += sin(uv.y * 130.0 - t * 5.0) * 0.0007;

                // --- chromatic aberration ---
                float ca = 0.0016 + _Glitch * 0.006;
                float r = tex2D(_MainTex, uv + centered * ca + float2(tear, 0)).r;
                float g = tex2D(_MainTex, uv + float2(tear, 0)).g;
                float b = tex2D(_MainTex, uv - centered * ca + float2(tear, 0)).b;
                fixed3 col = fixed3(r, g, b);

                // --- scanlines ---
                float scan = 0.90 + 0.10 * sin(uv.y * _ScreenParams.y * 2.0944);
                col *= scan;

                // --- tape static / grain ---
                float noise = Rand(uv * _ScreenParams.xy * 0.5 + frac(t) * 137.0);
                col += (noise - 0.5) * (0.09 + _Glitch * 0.55);

                // --- occasional white spike line during glitch ---
                float spikeRow = Rand(float2(floor(uv.y * 160.0), floor(t * 12.0)));
                if (_Glitch > 0.4 && spikeRow > 0.96)
                    col += _Glitch * 0.6;

                // --- vignette + slow brightness breathing ---
                float vig = smoothstep(0.95, 0.35, length(centered));
                float breathe = 0.93 + 0.07 * sin(t * 2.7);
                col *= vig * breathe;

                // --- glitch blackout dip ---
                col *= 1.0 - 0.15 * _Glitch;

                return fixed4(saturate(col), 1);
            }
            ENDCG
        }
    }

    FallBack Off
}
