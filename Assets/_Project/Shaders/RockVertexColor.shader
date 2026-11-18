Shader "Evolve/RockVertexColor"
{
    // Simple URP lit shader whose albedo is the mesh's vertex colour (stone and moss painted in Blender).
    // Main light with shadows + ambient; additional lights are not sampled (keep it cheap and compile-safe).
    Properties { _Tint ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float4 shadowCoord : TEXCOORD1; float fogFactor : TEXCOORD2; half4 color : COLOR; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.shadowCoord = GetShadowCoord(p);
                o.color = i.color;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                Light l = GetMainLight(i.shadowCoord);
                half3 direct = l.color * (l.distanceAttenuation * l.shadowAttenuation * saturate(dot(n, l.direction)));
                half3 ambient = SampleSH(n);
                // Blender writes colour attributes sRGB-encoded into the FBX: convert to linear so the stone is dark, not chalky
                half3 albedo = SRGBToLinear(i.color.rgb) * _Tint.rgb;
                half3 col = albedo * (direct + ambient);
                col = MixFog(col, i.fogFactor); // far rocks fade into the fog like everything else
                return half4(col, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
