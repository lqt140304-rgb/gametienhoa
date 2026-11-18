Shader "Evolve/LakeWater"
{
    // Translucent lake water for URP. Colour and opacity deepen with the water's depth (read from the scene depth
    // texture, so URP "Depth Texture" must be on: Evolve -> Dress Lake: Light and Water Look turns it on), the edge
    // fades out where water meets ground, and a foam line shows where it touches rocks and banks.
    // No true reflection: a Fresnel sheen from the ambient light and a sun glint. Visual only.
    Properties
    {
        _ShallowColor ("Shallow colour (alpha = opacity)", Color) = (0.25, 0.84, 0.82, 0.45)
        _DeepColor ("Deep colour (alpha = opacity)", Color) = (0.03, 0.30, 0.32, 0.90)
        _DeepDistance ("Depth for full deep colour (m)", Float) = 3
        _EdgeSoft ("Edge fade distance (m)", Float) = 0.35
        _FoamWidth ("Foam width (m)", Float) = 0.45
        _FoamColor ("Foam colour", Color) = (0.95, 1, 1, 0.8)
        [NoScaleOffset] _BumpMap ("Ripple normal map", 2D) = "bump" {}
        _BumpScale ("Ripple strength", Float) = 0.3
        _RippleSize ("Ripple repeat (m)", Float) = 9
        _Drift ("Ripple drift", Vector) = (0.012, 0.007, 0, 0)
        _Smoothness ("Sun glint sharpness", Range(0, 1)) = 0.6
        _Emission ("Glow", Color) = (0.02, 0.10, 0.10, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                float _DeepDistance;
                float _EdgeSoft;
                float _FoamWidth;
                half4 _FoamColor;
                float _BumpScale;
                float _RippleSize;
                float4 _Drift;
                float _Smoothness;
                half4 _Emission;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // water depth behind this fragment, in metres (scene depth minus the water surface's own depth)
                float2 screenUV = i.positionCS.xy / _ScaledScreenParams.xy;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceEye = i.positionCS.w;
                float depth = max(0.0, sceneEye - surfaceEye);

                // ripples: two normal map layers drifting in different directions
                float2 uv = i.positionWS.xz / max(0.1, _RippleSize);
                float3 n1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv + _Drift.xy * _Time.y), _BumpScale);
                float3 n2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv * 1.7 - _Drift.yx * _Time.y), _BumpScale);
                float3 n = normalize(float3(n1.x + n2.x, 2.0, n1.y + n2.y));

                float fade = saturate(depth / max(0.01, _DeepDistance));
                fade = fade * fade * (3.0 - 2.0 * fade);
                half4 col = lerp(_ShallowColor, _DeepColor, fade);

                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float fresnel = pow(1.0 - saturate(dot(n, viewDir)), 4.0);
                col.rgb += SampleSH(n) * fresnel * 0.6;

                Light l = GetMainLight();
                float3 h = normalize(l.direction + viewDir);
                float glint = pow(saturate(dot(n, h)), lerp(16.0, 256.0, _Smoothness));
                col.rgb += l.color * glint * 0.6;
                col.rgb += _Emission.rgb;

                // foam where the water is shallow (meets rocks and banks), broken up by the ripple pattern
                float foam = (1.0 - saturate(depth / max(0.01, _FoamWidth))) * saturate(0.6 + n1.x * 2.0);
                col.rgb = lerp(col.rgb, _FoamColor.rgb, foam * _FoamColor.a);
                col.a = max(col.a, foam * _FoamColor.a);

                // soft edge: transparent exactly where the water touches ground
                col.a *= saturate(depth / max(0.01, _EdgeSoft));
                return col;
            }
            ENDHLSL
        }
    }
}
