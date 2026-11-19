Shader "Evolve/GlowParticle"
{
    // Soft glowing dot for particles (fireflies, dust motes, spores, water glints): additive, a bright core and a soft halo drawn in the shader (no texture),
    // optional blinking. The particle's start colour carries the colour (rgb) and also seeds each particle's own blink pace and phase (r and g),
    // its alpha the brightness.
    Properties
    {
        _Intensity ("Intensity", Float) = 1.5
        _Blink ("Blink amount (0 = steady, 1 = full)", Range(0, 1)) = 0
        _Halo ("Halo", Range(0, 2)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+30" }
        Pass
        {
            Name "Glow"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
                float _Blink;
                float _Halo;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                o.color = i.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float d = length(i.uv - 0.5) * 2.0;                       // 0 in the middle, 1 at the quad's edge
                float halo = pow(saturate(1.0 - d), 2.2) * _Halo;
                float core = pow(saturate(1.0 - d * 2.2), 3.0);
                float pulse = lerp(1.0, 0.25 + 0.75 * (0.5 + 0.5 * sin(_Time.y * (1.4 + i.color.g * 3.2) + i.color.r * 57.0)), _Blink);
                float a = (halo * 0.7 + core * 1.3) * i.color.a * pulse;
                return half4(i.color.rgb * _Intensity, saturate(a));
            }
            ENDHLSL
        }
    }
}
