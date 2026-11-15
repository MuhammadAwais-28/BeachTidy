// Inverted-hull outline (URP). Draws only the extruded back faces, so you see
// a stroke of constant pixel width around the object, whatever its size or distance.
// Reads smoothed normals from UV3 (written by PickupHighlighter) so boxy meshes
// like benches get a continuous stroke instead of gaps at the corners.
Shader "BeachTidy/OutlineHull"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0.1, 1.0, 0.2, 1)
        _Width("Width (pixels)", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _Width;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS     : POSITION;
                float3 normalOS       : NORMAL;
                float3 smoothNormalOS : TEXCOORD3;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Smoothed normal if the mesh has one, otherwise the normal it shipped with
                float3 n = IN.smoothNormalOS;
                if (dot(n, n) < 0.0001) n = IN.normalOS;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(n));
                float4 positionCS = TransformWorldToHClip(positionWS);

                // Push the vertex outwards by a fixed number of PIXELS
                float2 normalCS = mul((float3x3)UNITY_MATRIX_VP, normalWS).xy;
                float2 v = normalCS * _ScreenParams.xy;
                float2 dir = v / max(length(v), 0.00001);

                float2 offsetNDC = dir * _Width * 2.0 / _ScreenParams.xy;
                positionCS.xy += offsetNDC * positionCS.w;

                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
