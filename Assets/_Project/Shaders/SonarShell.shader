Shader "Evolve/SonarShell"
{
    // The sound wave seen as a thin expanding shell: no light is thrown. It shows only as a bright line wherever the shell cuts through the ground and the things standing
    // on it (read from the scene depth), plus a faint rim, like a ripple of sound passing through the cave. Additive.
    Properties
    {
        _Color ("Wave colour", Color) = (0.55, 0.85, 1.0, 1)
        _Intensity ("Line brightness", Float) = 2.2
        _Edge ("Line width (m)", Float) = 0.55
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
                float line_ = pow(saturate(1.0 - diff / max(0.01, _Edge)), 3.0); // bright exactly where the shell cuts a surface
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float rim = pow(1.0 - saturate(abs(dot(normalize(i.normalWS), v))), 3.0);
                half3 col = _Color.rgb * (line_ * _Intensity + rim * _Rim) * _Fade;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
