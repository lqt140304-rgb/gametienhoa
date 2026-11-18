Shader "Evolve/ScannedFoliage"
{
    // Two-sided, alpha-clipped foliage for the scanned plants (Poly Haven). Unlike URP Lit with Cull Off, the normal is flipped on back faces,
    // so a leaf seen from the shaded side is lit like the front instead of going black. Main light with shadows, ambient, the AO from the
    // packed ARM map (R channel), fog. Colour map alpha = the leaf mask.
    Properties
    {
        _BaseMap ("Colour (alpha = leaf mask)", 2D) = "white" {}
        _BumpMap ("Normal map", 2D) = "bump" {}
        _ArmMap ("AO / rough / metal (R = AO)", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.45
        _Translucency ("Light through leaves", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" }
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_ArmMap); SAMPLER(sampler_ArmMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half _Cutoff;
                half _Translucency;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 tangentOS : TANGENT; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float4 tangentWS : TEXCOORD2; float3 positionWS : TEXCOORD3; float fogFactor : TEXCOORD4; };

            Varyings vert(Attributes i)
            {
                UNITY_SETUP_INSTANCE_ID(i);
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(i.normalOS, i.tangentOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;
                o.tangentWS = float4(n.tangentWS, i.tangentOS.w * GetOddNegativeScale());
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                clip(albedo.a - _Cutoff);
                float sgn = front ? 1.0 : -1.0;                       // back face: flip the normal so it is lit like the front
                float3 nWS = normalize(i.normalWS) * sgn;
                float3 tWS = normalize(i.tangentWS.xyz);
                float3 bWS = cross(nWS, tWS) * i.tangentWS.w;
                half3 nTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv));
                float3 n = normalize(nTS.x * tWS + nTS.y * bWS + nTS.z * nWS);
                half ao = SAMPLE_TEXTURE2D(_ArmMap, sampler_ArmMap, i.uv).r;
                Light l = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half atten = l.distanceAttenuation * l.shadowAttenuation;
                half3 direct = l.color * atten * saturate(dot(n, l.direction));
                half3 through = l.color * atten * saturate(dot(-n, l.direction)) * _Translucency;   // sunlight glowing through a thin leaf
                half3 col = albedo.rgb * (direct + through + SampleSH(n) * ao);
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Off
            ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half _Cutoff;
                half _Translucency;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A i) { UNITY_SETUP_INSTANCE_ID(i); V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = TRANSFORM_TEX(i.uv, _BaseMap); return o; }
            half4 frag(V i) : SV_Target { clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a - _Cutoff); return 0; }
            ENDHLSL
        }
    }
}
