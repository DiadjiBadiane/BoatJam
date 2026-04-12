Shader "Custom/OceanWater"
{
    Properties
    {
        _DeepColor    ("Deep Color",    Color) = (0.035, 0.145, 0.180, 1)
        _MidColor     ("Mid Color",     Color) = (0.055, 0.220, 0.260, 1)
        _ShallowColor ("Shallow Color", Color) = (0.090, 0.330, 0.350, 1)
        _FoamColor    ("Foam / Crest",  Color) = (0.700, 0.820, 0.800, 1)
        _CausticColor ("Caustic Tint",  Color) = (0.120, 0.350, 0.320, 0.3)

        _WaveScale1   ("Wave Scale 1",       Float) = 0.18
        _WaveScale2   ("Wave Scale 2",       Float) = 0.4
        _WaveScale3   ("Wave Scale 3 (ripple)", Float) = 1.2
        _WaveSpeed1   ("Wave Speed 1",       Float) = 0.02
        _WaveSpeed2   ("Wave Speed 2",       Float) = 0.015
        _WaveSpeed3   ("Wave Speed 3 (ripple)", Float) = 0.03
        _FoamThreshold("Foam Threshold",     Float) = 0.72
        _FoamSoftness ("Foam Softness",      Float) = 0.18
        _CausticScale ("Caustic Scale",      Float) = 1.4
        _CausticSpeed ("Caustic Speed",      Float) = 0.025
        _CausticIntensity ("Caustic Intensity", Float) = 0.04
        _SpecularPower("Specular Highlight", Float) = 0.04
        _SpecularTight("Specular Tightness", Float) = 6.0
        _WaveHeight   ("Wave Height (vertex)", Float) = 0.003
        _Murkiness    ("Murkiness",          Float) = 0.06
        _RippleStrength("Ripple Strength",   Float) = 0.06
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry-1" }
        LOD 200

        Pass
        {
            Name "OceanForward"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── Properties ────────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                half4  _DeepColor;
                half4  _MidColor;
                half4  _ShallowColor;
                half4  _FoamColor;
                half4  _CausticColor;
                float  _WaveScale1;
                float  _WaveScale2;
                float  _WaveScale3;
                float  _WaveSpeed1;
                float  _WaveSpeed2;
                float  _WaveSpeed3;
                float  _FoamThreshold;
                float  _FoamSoftness;
                float  _CausticScale;
                float  _CausticSpeed;
                float  _CausticIntensity;
                float  _SpecularPower;
                float  _SpecularTight;
                float  _WaveHeight;
                float  _Murkiness;
                float  _RippleStrength;
            CBUFFER_END

            // ── Structs ──────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
            };

            // ── Hash / Noise helpers ─────────────────────────────────────
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Gradient noise for more natural water patterns
            float2 hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float gradientNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(dot(hash22(i + float2(0, 0)), f - float2(0, 0)),
                                 dot(hash22(i + float2(1, 0)), f - float2(1, 0)), u.x),
                            lerp(dot(hash22(i + float2(0, 1)), f - float2(0, 1)),
                                 dot(hash22(i + float2(1, 1)), f - float2(1, 1)), u.x), u.y);
            }

            // FBM with domain warping for realistic water
            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                float2 shift = float2(100.0, 100.0);
                float2x2 rot = float2x2(cos(0.5), sin(0.5), -sin(0.5), cos(0.5));
                for (int i = 0; i < 5; i++)
                {
                    v += a * valueNoise(p);
                    p = mul(rot, p) * 2.0 + shift;
                    a *= 0.5;
                }
                return v;
            }

            // Domain-warped FBM for organic swirling patterns
            float warpedFbm(float2 p, float t)
            {
                float2 q = float2(fbm(p + float2(0.0, 0.0)),
                                  fbm(p + float2(5.2, 1.3)));
                float2 r = float2(fbm(p + 4.0 * q + float2(1.7, 9.2) + t * 0.015),
                                  fbm(p + 4.0 * q + float2(8.3, 2.8) + t * 0.012));
                return fbm(p + 4.0 * r);
            }

            // Voronoi for caustics
            float voronoi(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float minDist = 1.0;
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 pt = float2(hash21(i + neighbor),
                                           hash21(i + neighbor + float2(37.0, 17.0)));
                        float2 diff = neighbor + pt - f;
                        float dist = dot(diff, diff);
                        minDist = min(minDist, dist);
                    }
                }
                return sqrt(minDist);
            }

            // ── Vertex ───────────────────────────────────────────────────

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 posOS = IN.positionOS.xyz;

                // Multi-layered vertex wave displacement
                float t = _Time.y;
                // Large slow swell
                float wave  = sin(posOS.x * 0.8 + posOS.z * 0.6 + t * 0.5) * _WaveHeight;
                // Medium cross-wave
                      wave += sin(posOS.x * 1.5 - posOS.z * 1.2 + t * 0.35) * _WaveHeight * 0.5;
                // Small choppy waves
                      wave += sin(posOS.x * 3.0 + posOS.z * 2.5 + t * 0.8) * _WaveHeight * 0.15;
                posOS.y += wave;

                OUT.worldPos   = TransformObjectToWorld(posOS);
                OUT.positionCS = TransformWorldToHClip(OUT.worldPos);
                OUT.uv         = IN.uv;
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            // ── Fragment ─────────────────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y;
                float2 worldUV = IN.worldPos.xz;

                // ── 1. Large slow-moving swell ───────────────────────────
                float2 uv1 = worldUV * _WaveScale1 + float2(t * _WaveSpeed1, t * _WaveSpeed1 * 0.7);
                float swell = warpedFbm(uv1, t);

                // ── 2. Medium wave layer ─────────────────────────────────
                float2 uv2 = worldUV * _WaveScale2 + float2(-t * _WaveSpeed2 * 0.6, t * _WaveSpeed2);
                float midWave = fbm(uv2 + float2(5.2, 1.3));

                // ── 3. Fine ripples (wind chop) ──────────────────────────
                float2 uv3 = worldUV * _WaveScale3 + float2(t * _WaveSpeed3 * 1.1, -t * _WaveSpeed3 * 0.8);
                float ripple = gradientNoise(uv3) * 0.5 + 0.5;
                float2 uv3b = worldUV * _WaveScale3 * 1.3 + float2(-t * _WaveSpeed3 * 0.7, t * _WaveSpeed3);
                ripple = (ripple + (gradientNoise(uv3b) * 0.5 + 0.5)) * 0.5;

                // Combine layers — swell-dominated for uniformity
                float combinedWave = swell * 0.55 + midWave * 0.30 + ripple * 0.15;

                // ── 4. Base water color ──────────────────────────────────
                // Harbor water: dark, slightly green-tinted, murky
                half3 baseColor;
                float colorT = saturate(combinedWave);
                if (colorT < 0.45)
                    baseColor = lerp(_DeepColor.rgb, _MidColor.rgb, colorT / 0.45);
                else
                    baseColor = lerp(_MidColor.rgb, _ShallowColor.rgb, (colorT - 0.45) / 0.55);

                // Murky variation - subtle swirling darker patches
                float murkNoise = warpedFbm(worldUV * 0.15 + float2(t * 0.005, 0), t);
                baseColor = lerp(baseColor, _DeepColor.rgb * 0.7, murkNoise * _Murkiness);

                // ── 5. Foam / whitecaps ──────────────────────────────────
                float foamMask = smoothstep(_FoamThreshold - _FoamSoftness,
                                            _FoamThreshold + _FoamSoftness,
                                            combinedWave);
                // Subtle foam breakup
                float foamDetail = valueNoise(worldUV * 3.0 + t * 0.06);
                foamMask *= smoothstep(0.35, 0.70, foamDetail);

                baseColor = lerp(baseColor, _FoamColor.rgb, foamMask * 0.25);

                // ── 6. Underwater caustics ───────────────────────────────
                float2 causticUV = worldUV * _CausticScale + float2(t * _CausticSpeed, -t * _CausticSpeed * 0.5);
                float caustic1 = voronoi(causticUV);
                float caustic2 = voronoi(causticUV * 1.3 + float2(t * _CausticSpeed * 0.3, 0));
                float caustics = (1.0 - caustic1) * (1.0 - caustic2);
                caustics = pow(saturate(caustics), 2.0) * _CausticIntensity;
                // Caustics only visible in calmer/shallower areas
                caustics *= smoothstep(0.3, 0.55, combinedWave);
                baseColor += _CausticColor.rgb * caustics;

                // ── 7. Specular highlights (sun glints) ──────────────────
                float spec = pow(saturate(combinedWave), _SpecularTight) * _SpecularPower;
                baseColor += half3(0.9, 0.95, 0.85) * spec;

                // ── 8. Vignette — darker toward edges ────────────────────
                float2 centreOffset = IN.uv - 0.5;
                float vignette = 1.0 - saturate(dot(centreOffset, centreOffset) * 0.5);
                baseColor *= lerp(0.80, 1.0, vignette);

                // ── Apply fog ────────────────────────────────────────────
                half3 finalColor = MixFog(baseColor, IN.fogFactor);

                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
