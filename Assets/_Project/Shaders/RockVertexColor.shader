Shader "Evolve/RockVertexColor"
{
    // Simple URP lit shader whose albedo is the mesh's vertex colour (stone and moss painted in Blender).
    // Main light with shadows + ambient; additional lights are not sampled (keep it cheap and compile-safe).
    Properties { _Tint ("Tint", Color) = (1,1,1,1) _Glow ("Glow (self-lit, fades with blindness)", Float) = 0 _Fill ("Fill light (a soft light independent of the scene, fades with blindness)", Float) = 0 }
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
                half _Glow;
                half _Fill;
            CBUFFER_END
            #include "EvolveSight.hlsl"   // _EvolveBlind, _EvolveSightCenter and EvolveSightVisible

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float4 shadowCoord : TEXCOORD1; float fogFactor : TEXCOORD2; float3 positionWS : TEXCOORD3; half4 color : COLOR; };

            // cheap 3D value noise: grain, patches and a rough surface so stone and bark are not smooth plastic
            float h31(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float vnoise(float3 x)
            {
                float3 i = floor(x), f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(h31(i), h31(i + float3(1,0,0)), f.x), lerp(h31(i + float3(0,1,0)), h31(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(h31(i + float3(0,0,1)), h31(i + float3(1,0,1)), f.x), lerp(h31(i + float3(0,1,1)), h31(i + float3(1,1,1)), f.x), f.y), f.z);
            }
            float fbm2(float3 p) { return vnoise(p * 5.5) * 0.65 + vnoise(p * 14.0) * 0.35; }

            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.shadowCoord = GetShadowCoord(p);
                o.color = i.color;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                float3 pw = i.positionWS;
                // rough surface: the gradient of the fine noise tilts the normal (the light then catches tiny ridges)
                float e = 0.06;
                float f0 = fbm2(pw);
                float3 grad = float3(fbm2(pw + float3(e, 0, 0)) - f0, fbm2(pw + float3(0, e, 0)) - f0, fbm2(pw + float3(0, 0, e)) - f0) / e;
                n = normalize(n - (grad - n * dot(grad, n)) * 0.07);
                Light l = GetMainLight(i.shadowCoord);
                half3 direct = l.color * (l.distanceAttenuation * l.shadowAttenuation * saturate(dot(n, l.direction)));
                // ambient is darker in the lower and sideways-facing hollows (a cheap occlusion), lighter on tops
                half occlusion = lerp(0.5, 1.0, saturate(n.y * 0.5 + 0.55)) * lerp(0.8, 1.0, f0);
                half3 ambient = SampleSH(n) * occlusion;
                // Blender writes colour attributes sRGB-encoded into the FBX: convert to linear so the stone is dark, not chalky
                half3 albedo = SRGBToLinear(i.color.rgb) * _Tint.rgb;
                // large patches (0.6 m) and fine grain: no two spots the same, stone never one flat colour
                float patch = vnoise(pw * 1.7) - 0.5, grain = fbm2(pw * 2.3) - 0.5;
                albedo *= saturate(1.0 + patch * 0.55 + grain * 0.5);
                half sight = EvolveSightVisible(pw);
                half3 col = albedo * (direct + ambient + _Fill * sight);   // _Fill keeps dark-painted plants readable in a dim cave, only while they can be seen
                col += albedo * _Glow * sight;   // luminous parts (mushroom caps, crystals, bulbs) shine only while you can see
                col = MixFog(col, i.fogFactor); // far rocks fade into the fog like everything else
                return half4(col, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
