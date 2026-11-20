Shader "Evolve/SkillGel"
{
    // Skill effects: a soft glassy glow (bright rim, faint body) that fades by dissolving through a noise with a glowing edge.
    // Additive. Meshes with normals (shells, crystals) use the rim; flat things (rings, tongue, trails) set _RimGain to 0 and _Fill to 1.
    // _Color is the tint, vertex colour multiplies it (trails and rings fade through it), _Fade runs 1 (whole) to 0 (gone).
    Properties
    {
        [HDR] _Color ("Colour", Color) = (0.7, 0.9, 1, 1)
        _Fill ("Body glow", Range(0, 1)) = 0.12
        _RimGain ("Rim gain (0 = flat)", Range(0, 4)) = 1.6
        _RimPower ("Rim power", Range(0.5, 8)) = 2.5
        _Intensity ("Intensity", Float) = 1.5
        _Fade ("Fade (1 whole, 0 gone)", Range(0, 1)) = 1
        _Grain ("Dissolve grain size", Float) = 7
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+35" }
        Pass
        {
            Name "SkillGel"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "EvolveSight.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Fill;
                float _RimGain;
                float _RimPower;
                float _Intensity;
                float _Fade;
                float _Grain;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                half4 color : COLOR;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.positionOS = i.positionOS.xyz;
                o.color = i.color;
                return o;
            }

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float Noise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(lerp(Hash(i), Hash(i + float3(1, 0, 0)), f.x), lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
                    lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x), lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y),
                    f.z);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float rim = pow(1.0 - saturate(abs(dot(n, v))), _RimPower) * _RimGain;
                float glow = _Fill + rim;

                // dissolve: a noise threshold sweeps from nothing (fade 1) to everything (fade 0); the band at the threshold glows
                float noise = Noise(i.positionOS * _Grain);
                float cut = (1.0 - _Fade) * 1.12 - 0.12;
                float keep = smoothstep(0.0, 0.12, noise - cut);
                float edge = keep * (1.0 - keep) * 4.0;

                float3 rgb = _Color.rgb * i.color.rgb * _Intensity * (glow + edge * 1.5);
                float a = saturate(glow + edge) * keep * i.color.a * _Color.a * saturate(_Fade * 4.0);
                a *= EvolveSightBase(i.positionWS);   // blind: only what is near the slime shows
                return half4(rgb, saturate(a));
            }
            ENDHLSL
        }
    }
}
