Shader "Custom/URP/EdgeGlow"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _GlowColor("Glow Color", Color) = (0, 1, 1, 1)
        _GlowPower("Glow Sharpness", Range(0.1, 16)) = 3
        _GlowIntensity("Glow Intensity", Range(0, 10)) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GlowColor;
                float  _GlowPower;
                float  _GlowIntensity;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.normalWS    = nrmInputs.normalWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(posInputs.positionWS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                float rim = 1.0 - saturate(dot(N, V));
                rim = pow(rim, _GlowPower);

                half3 color = _BaseColor.rgb + _GlowColor.rgb * rim * _GlowIntensity;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}