Shader "Evolve/LakeWater"
{
    // Translucent lake water for URP. Colour and opacity deepen with the water's depth (read from the scene depth
    // texture, so URP "Depth Texture" must be on: Evolve -> Dress Lake: Light and Water Look turns it on), the edge
    // fades out where water meets ground, and a foam line shows where it touches rocks and banks.
    // Realistic water: refraction (the bed is seen through the surface, bent by the ripples; needs URP "Opaque Texture" on), light absorbed
    // with depth (red goes first, so deep water is blue-green), moving caustics on the shallow bed, a Fresnel reflection of the surroundings
    // (weak looking straight down, strong at grazing angles), glints from the sun and every nearby light (glowing plants shimmer on the water),
    // and animated foam at the banks. Visual only.
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
        _Absorb ("Light absorbed per metre (r,g,b)", Color) = (0.60, 0.16, 0.10, 1)
        _Refract ("Refraction strength", Float) = 0.07
        _ReflectTint ("Reflection of the surroundings", Color) = (0.10, 0.17, 0.16, 1)
        _CausticStrength ("Caustics on the bed", Float) = 0.55
        _GlintStrength ("Glints from lights", Float) = 1.0
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
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

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
                half4 _Absorb;
                float _Refract;
                half4 _ReflectTint;
                float _CausticStrength;
                float _GlintStrength;
            CBUFFER_END

            // set by MonochromeVision: 0 = full sight (default when unset), 1 = blind; centre + radius of the lit area around the character
            float _EvolveBlind;
            float4 _EvolveSightCenter;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float fogFactor : TEXCOORD1; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            // thin bright lines where ripples focus the light: two ridged noise layers sliding over each other
            float Caustics(float2 p, float t)
            {
                float2 a = p * 0.9 + float2(t * 0.05, t * 0.03);
                float2 b = p * 1.35 - float2(t * 0.04, -t * 0.06);
                float v1 = sin(a.x * 3.1 + sin(a.y * 2.3 + t * 0.5) * 1.7) * sin(a.y * 2.7 + sin(a.x * 1.9 - t * 0.4) * 1.5);
                float v2 = sin(b.x * 2.6 + sin(b.y * 3.1 - t * 0.45) * 1.4) * sin(b.y * 3.3 + sin(b.x * 2.2 + t * 0.35) * 1.6);
                float c = 1.0 - abs(v1 * 0.5 + v2 * 0.5);
                return pow(saturate(c), 8.0);
            }

            half4 frag(Varyings i) : SV_Target
            {
                // water depth behind this fragment, in metres (scene depth minus the water surface's own depth)
                float2 screenUV = i.positionCS.xy / _ScaledScreenParams.xy;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceEye = i.positionCS.w;
                float depth = max(0.0, sceneEye - surfaceEye);
                // no usable depth (texture off, or nothing behind the water): treat it as one metre of water instead of vanishing
                if (rawDepth == UNITY_RAW_FAR_CLIP_VALUE || sceneEye <= surfaceEye + 0.0001) depth = 1.0;

                // ripples: three normal map layers drifting in different directions (a broad slow swell and two finer ripples)
                float2 uv = i.positionWS.xz / max(0.1, _RippleSize);
                float3 n1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv + _Drift.xy * _Time.y), _BumpScale);
                float3 n2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv * 1.7 - _Drift.yx * _Time.y), _BumpScale);
                float3 n3 = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv * 0.45 + float2(_Drift.y, -_Drift.x) * _Time.y * 0.6), _BumpScale * 0.8);
                float3 n = normalize(float3(n1.x + n2.x + n3.x, 2.0, n1.y + n2.y + n3.y));

                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float ndv = saturate(dot(n, viewDir));

                // refraction: the bed behind the surface, bent by the ripples (less where the water is thin, so the banks do not smear)
                float2 offset = n.xz * _Refract * saturate(depth * 2.0);
                float2 uvR = screenUV + offset;
                // never refract to something in front of the water: fall back to the straight view there
                float depthR = LinearEyeDepth(SampleSceneDepth(uvR), _ZBufferParams);
                if (depthR < surfaceEye) uvR = screenUV;
                half3 bed = SampleSceneColor(uvR);

                // caustics dance on the shallow bed and fade out with depth
                float shallow = 1.0 - saturate(depth / 1.6);
                Light l = GetMainLight();
                float caustic = Caustics(i.positionWS.xz + n.xz * 1.5, _Time.y) * shallow * _CausticStrength;
                bed += (l.color * 0.5 + 0.35) * caustic * saturate(bed + 0.15);

                // Beer-Lambert: red light is absorbed fastest, so water goes blue-green with depth
                half3 transmit = exp(-_Absorb.rgb * depth);
                float fade = saturate(depth / max(0.01, _DeepDistance));
                fade = fade * fade * (3.0 - 2.0 * fade);
                half3 deepCol = lerp(_ShallowColor.rgb, _DeepColor.rgb, fade);
                float body = 1.0 - exp(-depth * 1.6 / max(0.5, _DeepDistance));   // how much of the colour is the water's own and not the bed
                half3 col = bed * transmit * lerp(half3(1, 1, 1), _ShallowColor.rgb * 1.6, 0.35) * (1.0 - body * 0.85) + deepCol * body * 0.85;

                // Fresnel reflection (Schlick, water): weak looking down, strong at the grazing angles near the bank
                float fresnel = 0.02 + 0.98 * pow(1.0 - ndv, 5.0);
                half3 reflection = GlossyEnvironmentReflection(reflect(-viewDir, n), 0.08, 1.0) + SampleSH(n) * 0.8 + _ReflectTint.rgb;
                col = lerp(col, reflection, saturate(fresnel * 0.9));

                // glints: the sun, then every nearby light (shimmering streaks under glowing plants and crystals)
                float3 h = normalize(l.direction + viewDir);
                float sparkle = 0.6 + 0.8 * saturate(n2.x * 3.0 + 0.5);
                col += l.color * pow(saturate(dot(n, h)), lerp(32.0, 512.0, _Smoothness)) * 1.4 * sparkle * _GlintStrength;
                #if defined(_ADDITIONAL_LIGHTS) || USE_FORWARD_PLUS
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = screenUV;
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light al = GetAdditionalLight(lightIndex, i.positionWS);
                    float3 ah = normalize(al.direction + viewDir);
                    float spec = pow(saturate(dot(n, ah)), 160.0) * 1.6;
                    float glow = saturate(dot(n, al.direction)) * 0.12;     // the pool of light the lamp throws on the water
                    col += al.color * al.distanceAttenuation * (spec * sparkle + glow) * _GlintStrength;
                LIGHT_LOOP_END
                #endif
                col += _Emission.rgb;

                // foam: ragged and moving, where the water is shallow (meets rocks and banks)
                float foamNoise = saturate(0.55 + n1.x * 2.2 + n3.y * 1.5);
                float foam = (1.0 - saturate(depth / max(0.01, _FoamWidth))) * foamNoise;
                col = lerp(col, _FoamColor.rgb, foam * _FoamColor.a);

                // soft edge: transparent exactly where the water touches ground
                half alpha = saturate(depth / max(0.01, _EdgeSoft));
                alpha = max(alpha, foam * _FoamColor.a);
                half4 result = half4(col, alpha);
                result.rgb = MixFog(result.rgb, i.fogFactor);

                // blind: the water is invisible except near the character (and inside a vibration scan), like everything else
                float near = _EvolveSightCenter.w > 0.0 ? saturate(1.0 - distance(i.positionWS, _EvolveSightCenter.xyz) / _EvolveSightCenter.w) : 0.0;
                float visible = 1.0 - _EvolveBlind * (1.0 - near * near);
                result.rgb *= visible;
                result.a *= visible;
                return result;
            }
            ENDHLSL
        }
    }
}
