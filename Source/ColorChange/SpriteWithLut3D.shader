Shader "Custom/SpriteWithLut3D"
{
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Lut3D ("3D LUT", 3D) = "" {}
        _LutSize ("LUT Size", Float) = 32
        _LutIntensity ("Intensity", Range(0,1)) = 1.0
    }

    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            UNITY_DECLARE_TEX3D(_Lut3D);
            float _LutSize;
            float _LutIntensity;

            struct appdata_t {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert (appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            // sample a Texture3D (works in modern Unity)
            inline float3 SampleLut3D(float3 c) {
                // Ensure lookup in [0,1]
                c = saturate(c);

                // Optionally offset by half texel to avoid boundary issues:
                // float half = 0.5 / _LutSize;
                // c = c * (1 - 1.0/_LutSize) + half;

                #if UNITY_NO_TEX3D
                    // Fallback not implemented here; prefer platforms with tex3D support.
                    return tex3D(_Lut3D, c).rgb;
                #else
                    return UNITY_SAMPLE_TEX3D(_Lut3D, c).rgb;
                #endif
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 s = tex2D(_MainTex, i.uv) * i.color;
                float3 src = s.rgb;

                float3 mapped = SampleLut3D(src);

                // mix between original and mapped based on intensity
                float3 outc = lerp(src, mapped, _LutIntensity);

                return fixed4(outc, s.a);
            }
            ENDCG
        }
    }
}
