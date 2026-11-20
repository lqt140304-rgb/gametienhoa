Shader "Evolve/SonarShell"
{
    // The sound wave seen as an expanding shell: no light is thrown. Every surface within a band's thickness of the shell glows (read from the scene depth), brighter
    // along the outlines and creases of what it passes (where the depth jumps), so the wave draws the SHAPE of the ground and the things on it as it goes by,
    // plus a faint rim. Additive.
    Properties
    {
        _Color ("Wave colour", Color) = (0.55, 0.85, 1.0, 1)
        _Intensity ("Brightness", Float) = 1.6
        _Edge ("Thickness of the band that shows surfaces (m)", Float) = 1.5
        _Rim ("Faint rim", Float) = 0.06
        _Fade ("Fade (set by the game)", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+40" }
        Pass
        {
            Name "Sonar"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Intensity;
                float _Edge;
                float _Rim;
                float _Fade;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.positionCS.xy / _ScaledScreenParams.xy;
                float sceneEye = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float diff = abs(sceneEye - i.positionCS.w);                    // how far the shell is from the surface behind it
                float x = diff / max(0.01, _Edge);
                float band = exp(-x * x * 2.5);                                  // a soft band: surfaces within about _Edge of the shell glow
                // outlines and creases: where the depth beside a pixel jumps, that is the edge of an object
                float gx = ddx(sceneEye), gy = ddy(sceneEye);
                float edge = saturate(length(float2(gx, gy)) / (0.025 * sceneEye + 0.04));
                float shape = 0.35 + 1.1 * edge;
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float rim = pow(1.0 - saturate(abs(dot(normalize(i.normalWS), v))), 3.0);
                half3 col = _Color.rgb * (band * _Intensity * shape + rim * _Rim) * _Fade;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
