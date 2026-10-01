Shader "InnsmouthCafe/Liquid Metaball"
{
    Properties
    {
        _DensityTex ("Density", 2D) = "black" {}
        _LiquidColor ("Liquid Color", Color) = (0.18, 0.42, 0.72, 1)
        _Threshold ("Threshold", Range(0, 1)) = 0.42
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_DensityTex); SAMPLER(sampler_DensityTex);
            float4 _DensityTex_ST;
            float4 _LiquidColor;
            float _Threshold;
            float _EdgeSoftness;
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _DensityTex);
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                half density = SAMPLE_TEXTURE2D(_DensityTex, sampler_DensityTex, input.uv).r;
                half alpha = smoothstep(_Threshold - _EdgeSoftness, _Threshold + _EdgeSoftness, density);
                return half4(_LiquidColor.rgb, alpha * _LiquidColor.a);
            }
            ENDHLSL
        }
    }
}
