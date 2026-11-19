Shader "Evolve/LightBeam"
{
    // A shaft of light through the roof, made to look like light in dusty air: soft silhouette (no hard cylinder edge), slowly drifting streaks of
    // brighter and darker air, tiny dust specks that twinkle as they cross the beam, strongest at the hole and thinning toward the water, and fading
    // out where it meets the water or the ground (scene depth), so there is no visible cut. Additive. The mesh's uv.x runs round the shaft, uv.y from
    // the top (0) to the bottom (1) (World/RootMeshes.LightShaft).
    Properties
    {
        _Color ("Beam colour", Color) = (1, 0.92, 0.7, 1)
        _Intensity ("Intensity", Float) = 0.4
        _Streaks ("Streaks round the beam", Float) = 12
        _Speed ("Drift speed", Float) = 0.06
        _Dust ("Dust specks", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" }
        Pass
        {
            Name "Beam"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "EvolveSight.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Intensity;
                float _Streaks;
                float _Speed;
                float _Dust;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 positionWS : TEXCOORD2; };

            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float hash31(float3 p) { p = frac(p * 0.1031); p += dot(p, p.zyx + 31.32); return frac((p.x + p.y) * p.z); }
            // value noise that wraps round the beam (period = the streak count) so there is no seam where uv.x goes from 1 to 0
            float wrapNoise(float x, float y, float period)
            {
                float xi = floor(x), yi = floor(y), xf = frac(x), yf = frac(y);
                xf = xf * xf * (3.0 - 2.0 * xf); yf = yf * yf * (3.0 - 2.0 * yf);
                float x0 = fmod(xi, period), x1 = fmod(xi + 1.0, period);
                float a = hash21(float2(x0, yi)), b = hash21(float2(x1, yi)), c = hash21(float2(x0, yi + 1.0)), d = hash21(float2(x1, yi + 1.0));
                return lerp(lerp(a, b, xf), lerp(c, d, xf), yf);
            }

            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = i.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                float h = saturate(i.uv.y);                                  // 0 at the hole, 1 at the water
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float facing = abs(dot(normalize(i.normalWS), viewDir));
                float edge = pow(saturate(facing), 2.6);                      // no outline: the light thins to nothing at the sides, brightest through the middle
                // streaks of denser and thinner air drifting round and down the beam
                float per = max(2.0, floor(_Streaks));
                float s1 = wrapNoise(i.uv.x * per + t * _Speed * 3.0, h * 2.5 - t * _Speed, per);
                float per2 = per * 2.0;
                float s2 = wrapNoise(i.uv.x * per2 - t * _Speed * 5.0 + 3.0, h * 6.0 + t * _Speed * 2.0, per2);
                float streak = 0.35 + 0.95 * (s1 * 0.65 + s2 * 0.35);
                // strongest at the hole, thinning toward the water
                float along = lerp(1.0, 0.3, h * h) * smoothstep(0.0, 0.22, h) * (1.0 - smoothstep(0.78, 1.0, h));   // it grows out of the hole and dissolves above the water: no start or end line
                // where the beam meets the water or the ground it fades out instead of ending in a hard line
                float2 suv = i.positionCS.xy / _ScaledScreenParams.xy;
                float sceneEye = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float soft = smoothstep(0.0, 1.0, saturate((sceneEye - i.positionCS.w) / 3.5));
                // dust specks: a few bright motes drifting down through the beam, each twinkling at its own pace
                float3 cell = floor(i.positionWS * 3.2 + float3(0.0, t * 0.25, 0.0));
                float r = hash31(cell);
                float speck = step(0.975, r) * pow(saturate(sin(t * (1.2 + r * 5.0) + r * 60.0) * 0.5 + 0.5), 4.0);
                float3 frac3 = frac(i.positionWS * 3.2 + float3(0.0, t * 0.25, 0.0)) - 0.5;
                speck *= saturate(1.0 - length(frac3) * 3.2);
                float body = _Intensity * streak * along * edge * soft;
                half3 col = _Color.rgb * (body + speck * _Dust * 1.6 * along * soft);
                col *= EvolveSightVisible(i.positionWS);   // blind: only the light round the slime is seen, spreading softly
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
