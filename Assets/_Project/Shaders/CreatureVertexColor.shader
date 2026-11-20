Shader "Evolve/CreatureVertexColor"
{
    // Clean URP lit shader for rigged wildlife using Blender vertex colours.
    // No world-space rock noise; preserves vibrant Blender colours with studio fill, subsurface wrap, and crisp rim/specular.
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _LinearBlend ("SRGB to Linear Blend", Range(0, 1)) = 0.25
        _Fill ("Studio Fill (fades with blindness)", Range(0, 1)) = 0.42
        _Subsurface ("Subsurface Wrap", Range(0, 1)) = 0.22
        _Smoothness ("Smoothness", Range(0, 1)) = 0.32
        _SpecStrength ("Specular Strength", Range(0, 1)) = 0.18
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.28
        _Glow ("Glow (self-lit, fades with blindness)", Float) = 0
        _ScanTint ("Food scan tint (set per object by the game; alpha 0 = not food)", Color) = (0,0,0,0)
    }
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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _LinearBlend;
                half _Fill;
                half _Subsurface;
                half _Smoothness;
                half _SpecStrength;
                half _RimStrength;
                half _Glow;
                half4 _ScanTint;
            CBUFFER_END
            #include "EvolveSight.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float4 shadowCoord : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                half4 color : COLOR;
            };

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
                half3 vdir = normalize(GetWorldSpaceViewDir(pw));

                // Preserve vibrant Blender vertex colours without crushing midtones to muddy darks
                half3 rawCol = saturate(i.color.rgb);
                half3 albedo = lerp(rawCol, SRGBToLinear(rawCol), _LinearBlend) * _Tint.rgb;

                Light l = GetMainLight(i.shadowCoord);
                half ndotl = dot(n, l.direction);
                half wrap = saturate((ndotl + _Subsurface) / (1.0 + _Subsurface));
                half3 direct = l.color * (l.distanceAttenuation * l.shadowAttenuation * wrap);

                // Soft hemisphere ambient + subtle studio fill so creatures read clearly in dim caves
                half hemi = lerp(0.68, 1.05, saturate(n.y * 0.5 + 0.5));
                half3 ambient = SampleSH(n) * hemi;

                // Specular highlight (organic skin vs glossy chitin)
                half3 halfVec = normalize(l.direction + vdir);
                half specExp = lerp(12.0, 96.0, _Smoothness);
                half spec = pow(saturate(dot(n, halfVec)), specExp) * _SpecStrength * l.distanceAttenuation * l.shadowAttenuation;

                // Additional point/spot lights in caves
                half3 extra = 0;
                half3 extraSpec = 0;
                #if defined(_ADDITIONAL_LIGHTS) || USE_FORWARD_PLUS
                InputData inputData = (InputData)0;
                inputData.positionWS = pw;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light al = GetAdditionalLight(lightIndex, pw);
                    half aWrap = saturate((dot(n, al.direction) + _Subsurface * 0.5) / (1.0 + _Subsurface * 0.5));
                    half atten = min(al.distanceAttenuation, 3.0);
                    extra += al.color * atten * aWrap;
                    half3 aHalf = normalize(al.direction + vdir);
                    extraSpec += al.color * atten * pow(saturate(dot(n, aHalf)), specExp) * _SpecStrength * 0.6;
                LIGHT_LOOP_END
                #endif

                half sight = EvolveSightVisible(pw);
                half rim = pow(1.0 - saturate(dot(n, vdir)), 2.5) * _RimStrength * sight;

                // Studio fill scales down softly on downward faces to keep 3D volume
                half fillTerm = _Fill * lerp(0.55, 1.0, saturate(n.y * 0.5 + 0.5)) * sight;
                half3 col = albedo * (direct + ambient + extra + fillTerm);
                col += (l.color * spec + extraSpec) * sight;
                col += lerp(albedo, half3(1.0, 1.0, 1.0), 0.25) * rim;
                col += albedo * _Glow * sight;

                if (_ScanTint.a > 0.0)
                {
                    float echo = EvolveEdibleEcho(pw);
                    float rimv = pow(1.0 - saturate(abs(dot(n, vdir))), 2.0);
                    float bands = smoothstep(0.55, 0.95, 0.5 + 0.5 * sin(dot(pw, float3(7.0, 11.0, 5.0)) * 1.3 - _Time.y * 5.0));
                    col += _ScanTint.rgb * echo * (0.35 + 1.1 * bands + 1.4 * rimv);
                }

                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            float3 _LightDirection;

            Varyings ShadowVert(Attributes i)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(i.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(i.normalOS);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    o.positionCS.z = min(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    o.positionCS.z = max(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }

            half4 ShadowFrag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
