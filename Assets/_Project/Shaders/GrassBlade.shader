Shader "Evolve/GrassBlade"
{
    // Grass blades for terrain details (GPU instanced meshes). Dark at the root, bright at the tip, swaying in a faint breeze, and bending
    // away from whoever walks through: _EvolvePushers (set by GrassPushers) holds up to 8 walkers (xyz position, w radius).
    // The mesh's uv.y is the blade's height from 0 (root) to 1 (tip); it weights every bend.
    Properties
    {
        _Root ("Root colour", Color) = (0.04, 0.10, 0.03, 1)
        _Tip ("Tip colour", Color) = (0.30, 0.52, 0.14, 1)
        _Height ("Blade height (m)", Float) = 0.8
        _Wind ("Wind sway (m)", Float) = 0.06
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "EvolveSight.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Root;
                half4 _Tip;
                half _Height;
                half _Wind;
            CBUFFER_END
            float4 _EvolvePushers[8];

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float4 shadowCoord : TEXCOORD1; float fogFactor : TEXCOORD2; float3 positionWS : TEXCOORD3; float2 uv : TEXCOORD4; };

            Varyings vert(Attributes i)
            {
                UNITY_SETUP_INSTANCE_ID(i);
                Varyings o;
                float3 wp = TransformObjectToWorld(i.positionOS.xyz);
                float w = i.uv.y * i.uv.y;                                   // the tip moves most
                float t = _Time.y;
                wp.x += sin(t * 1.3 + wp.x * 0.35 + wp.z * 0.2) * _Wind * w;
                wp.z += cos(t * 1.1 + wp.z * 0.3 + wp.x * 0.15) * _Wind * w;
                [unroll] for (int k = 0; k < 8; k++)
                {
                    float2 d = wp.xz - _EvolvePushers[k].xz;
                    float dist = length(d);
                    float push = saturate(1.0 - dist / max(_EvolvePushers[k].w, 0.01));
                    push *= push;
                    wp.xz += (d / max(dist, 0.001)) * push * w * _Height * 0.95;   // lean away
                    wp.y -= push * w * _Height * 0.45;                              // and duck
                }
                o.positionWS = wp;
                o.positionCS = TransformWorldToHClip(wp);
                float3 n = TransformObjectToWorldNormal(i.normalOS);
                o.normalWS = normalize(lerp(n, float3(0, 1, 0), 0.6));        // blades are lit like the ground below them: soft, no flat card look
                o.shadowCoord = TransformWorldToShadowCoord(wp);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                o.uv = i.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                Light l = GetMainLight(i.shadowCoord);
                half3 direct = l.color * (l.distanceAttenuation * l.shadowAttenuation * (0.35 + 0.65 * saturate(dot(n, l.direction))));
                half3 ambient = SampleSH(n);
                half3 extra = 0;
                #if defined(_ADDITIONAL_LIGHTS) || USE_FORWARD_PLUS
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light al = GetAdditionalLight(lightIndex, i.positionWS);
                    extra += al.color * al.distanceAttenuation * saturate(dot(n, al.direction));   // every lamp lights what is near it, less and less with distance
                LIGHT_LOOP_END
                #endif
                half vary = 0.85 + 0.3 * frac(sin(dot(floor(i.positionWS.xz * 1.7), float2(12.9898, 78.233))) * 43758.5453);   // blade patches differ a little
                half3 albedo = lerp(_Root.rgb, _Tip.rgb, saturate(i.uv.y)) * vary;
                half occlusion = lerp(0.35, 1.0, saturate(i.uv.y * 1.3));                                                    // dark at the root
                half3 col = albedo * (direct + ambient + extra) * occlusion + albedo * 0.12 * i.uv.y;                               // a little light through the tip
                col = MixFog(col, i.fogFactor);
                col *= EvolveSightVisible(i.positionWS);   // blind: only the grass round the slime can be felt
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
