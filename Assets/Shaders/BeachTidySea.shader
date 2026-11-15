// BeachTidy sea shader (URP, Unity 6)
// - Waves and foam are mapped in WORLD space, so you can scale the plane freely.
// - Shore foam, soft edge and shallow->deep colour come from the scene depth,
//   so they follow the real shoreline of the terrain.
// - Needs "Depth Texture" enabled on the URP asset.
Shader "Universal Render Pipeline/Custom/BeachTidySea"
{
    Properties
    {
        _ShallowColor("Shallow Color", Color) = (0.30, 0.80, 0.80, 1)
        _DeepColor("Deep Color", Color) = (0.02, 0.28, 0.48, 1)
        _ShallowAlpha("Shallow Opacity", Range(0, 1)) = 0.35
        _DeepAlpha("Deep Opacity", Range(0, 1)) = 0.9

        _DepthMax("Depth For Full Deep Color (m)", Float) = 0.15
        _ShoreFade("Shore Soft Edge (m)", Float) = 0.03

        _WaveMap("Wave Derivative Map", 2D) = "gray" {}
        _WaveScale("Wave Scale (repeats per m)", Float) = 0.04
        _WaveSpeed("Wave Speed", Float) = 0.03
        _NormalStrength("Wave Strength", Range(0, 4)) = 1.5
        _NormalFadeDistance("Wave Fade Distance (m)", Float) = 80

        _HorizonColor("Sky Reflection Horizon", Color) = (0.62, 0.85, 1.0, 1)
        _ZenithColor("Sky Reflection Top", Color) = (0.25, 0.55, 0.95, 1)
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 3
        _ReflectionStrength("Reflection Strength", Range(0, 1.5)) = 0.8
        _SpecPower("Sun Glint Sharpness", Range(8, 512)) = 160
        _SpecIntensity("Sun Glint Intensity", Range(0, 5)) = 1.5

        _FoamTex("Foam Texture", 2D) = "white" {}
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _FoamDepth("Foam Depth (m)", Float) = 0.05
        _FoamScale("Foam Scale (repeats per m)", Float) = 0.15
        _FoamSpeed("Foam Speed", Float) = 0.03
        _FoamPulse("Foam Wash Speed", Float) = 1.2
        _FoamIntensity("Foam Intensity", Range(0, 3)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_WaveMap); SAMPLER(sampler_WaveMap);
            TEXTURE2D(_FoamTex); SAMPLER(sampler_FoamTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _WaveMap_ST;
                float4 _FoamTex_ST;

                half4 _ShallowColor;
                half4 _DeepColor;
                half  _ShallowAlpha;
                half  _DeepAlpha;

                float _DepthMax;
                float _ShoreFade;

                float _WaveScale;
                float _WaveSpeed;
                float _NormalStrength;
                float _NormalFadeDistance;

                half4 _HorizonColor;
                half4 _ZenithColor;
                float _FresnelPower;
                float _ReflectionStrength;
                float _SpecPower;
                float _SpecIntensity;

                half4 _FoamColor;
                float _FoamDepth;
                float _FoamScale;
                float _FoamSpeed;
                float _FoamPulse;
                float _FoamIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float4 positionNDC : TEXCOORD1;
                float  eyeDepth    : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS  = p.positionCS;
                OUT.positionWS  = p.positionWS;
                OUT.positionNDC = p.positionNDC;
                OUT.eyeDepth    = -p.positionVS.z;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y;
                float2 wp = IN.positionWS.xz;

                // ---------- Waves: two moving layers of the derivative map ----------
                float2 uv1 = wp * _WaveScale + t * _WaveSpeed * float2(1.0, 0.35);
                float2 uv2 = wp * _WaveScale * 1.9 + t * _WaveSpeed * float2(-0.6, 0.9) + 0.37;
                half4 w1 = SAMPLE_TEXTURE2D(_WaveMap, sampler_WaveMap, uv1);
                half4 w2 = SAMPLE_TEXTURE2D(_WaveMap, sampler_WaveMap, uv2);

                // x-derivative is in ALPHA, y-derivative in GREEN
                float2 d = float2(w1.a, w1.g) * 2.0 - 1.0;
                d += (float2(w2.a, w2.g) * 2.0 - 1.0) * 0.7;

                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS)); // surface -> camera
                float dist = length(_WorldSpaceCameraPos - IN.positionWS);
                float fade = saturate(dist / max(_NormalFadeDistance, 0.001));

                float3 n = normalize(float3(-d.x * _NormalStrength, 1.0, -d.y * _NormalStrength));
                n = normalize(lerp(n, float3(0, 1, 0), fade * 0.85)); // calm far water, no shimmer

                // ---------- Water depth below the surface (meters, vertical) ----------
                float2 screenUV = IN.positionNDC.xy / IN.positionNDC.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                float eyeDiff = max(0.0, sceneEye - IN.eyeDepth);

                float3 camFwd = -UNITY_MATRIX_V[2].xyz;
                float cosF = max(dot(-viewDirWS, camFwd), 0.05);
                float depthDiff = eyeDiff * saturate(viewDirWS.y) / cosF;

                // Nothing behind the water (open sea): treat as very deep
                #if UNITY_REVERSED_Z
                    if (rawDepth < 0.00001) depthDiff = 1000.0;
                #else
                    if (rawDepth > 0.99999) depthDiff = 1000.0;
                #endif

                float depth01 = saturate(depthDiff / max(_DepthMax, 0.0001));

                // ---------- Body colour + light ----------
                Light mainLight = GetMainLight();
                float ndl = saturate(dot(n, mainLight.direction));

                half3 body = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth01);
                half3 lit = body * (0.6 + 0.4 * ndl) * mainLight.color;

                // ---------- Fresnel sky reflection (no cubemap needed) ----------
                float fres = pow(1.0 - saturate(dot(n, viewDirWS)), _FresnelPower);
                float3 reflDir = reflect(-viewDirWS, n);
                half3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(saturate(reflDir.y), 0.6));
                float refl = saturate(fres * _ReflectionStrength + 0.04);
                half3 col = lerp(lit, sky, refl);

                // ---------- Sun glint ----------
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float spec = pow(saturate(dot(n, halfDir)), _SpecPower) * _SpecIntensity;
                col += mainLight.color * spec;

                // ---------- Opacity: clear when shallow, solid when deep, soft at the shore ----------
                half alpha = lerp(_ShallowAlpha, _DeepAlpha, depth01);
                alpha = saturate(alpha + fres * 0.4);
                alpha *= saturate(depthDiff / max(_ShoreFade, 0.0001));

                // ---------- Shore foam, washing in and out ----------
                float pulse = 0.5 + 0.5 * sin(t * _FoamPulse);
                float foamWidth = _FoamDepth * lerp(0.5, 1.5, pulse);
                float shoreMask = saturate(1.0 - depthDiff / max(foamWidth, 0.0001));

                float2 fuv = wp * _FoamScale + float2(t * _FoamSpeed, t * _FoamSpeed * 0.6) + d * 0.1;
                half foamTex = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, fuv).r;

                float foam = saturate(shoreMask * (0.45 + foamTex * 1.6) * _FoamIntensity);
                col = lerp(col, _FoamColor.rgb * (0.75 + 0.25 * ndl), foam);
                alpha = saturate(max(alpha, foam));

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    // No fallback on purpose: a fallback would add an opaque shadow caster for the sea plane.
    FallBack Off
}
