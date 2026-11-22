Shader "Evolve/LatchGelCoating"
{
    Properties
    {
        _GelColor ("Gel tint", Color) = (0.35, 0.85, 0.65, 1)
        _Anchor ("Contact point", Vector) = (0, 0, 0, 0)
        _Radius ("Spread distance", Float) = 1
        _Coverage ("Spread", Float) = 0
        _Silhouette ("Swallowed silhouette", Float) = 0
        _Dissolve ("Dissolve progress", Float) = 0
        _Opacity ("Opacity", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+30" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Shaders/EvolveSight.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _GelColor, _Anchor;
                float _Radius, _Coverage, _Silhouette, _Dissolve, _Opacity;
            CBUFFER_END

            struct Attributes { float3 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
            };

            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.positionWS = TransformObjectToWorld(i.positionOS) + o.normalWS * min(0.018, _Radius * 0.012);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.positionOS = i.positionOS;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float distance01 = distance(i.positionWS, _Anchor.xyz) / _Radius;
                float ripple = sin(i.positionOS.x * 23 + i.positionOS.y * 17 + _Time.y * 6)
                    * sin(i.positionOS.z * 19 - _Time.y * 4);
                float front = _Coverage - distance01 + ripple * 0.045;
                float coat = smoothstep(-0.025, 0.04, front);
                float edge = coat * (1.0 - smoothstep(0.04, 0.105, front));
                float3 n = normalize(i.normalWS);
                float rim = pow(1.0 - saturate(dot(n, normalize(GetWorldSpaceViewDir(i.positionWS)))), 2.6);
                float erosion = saturate(1.0 - distance01 + ripple * 0.14);
                float silhouette = coat * _Silhouette * smoothstep(_Dissolve * 1.15 - 0.09, _Dissolve * 1.15, erosion);
                float3 gel = _GelColor.rgb * (0.54 + rim * 0.65 + edge * 1.25);
                float3 color = lerp(gel, _GelColor.rgb * 0.13, silhouette * (1.0 - rim) * 0.78);
                float alpha = max(coat * (0.24 + rim * 0.46 + edge * 0.24), silhouette * 0.84);
                alpha *= _Opacity * EvolveSightBase(i.positionWS);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
