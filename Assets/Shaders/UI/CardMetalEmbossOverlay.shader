Shader "InnsmouthCafe/UI/Card Metal Emboss Overlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [HDR] _MetalColor ("Metal Color", Color) = (0.72, 0.52, 0.2, 1)
        [HDR] _HighlightColor ("Highlight Color", Color) = (1, 0.9, 0.55, 1)
        _ShadowColor ("Shadow Color", Color) = (0.12, 0.035, 0.015, 1)
        _EmbossOpacity ("Emboss Opacity", Range(0, 1)) = 0.9
        _ReliefStrength ("Relief Strength", Range(0, 12)) = 4
        _EdgeWidth ("Edge Width", Range(0.25, 6)) = 1.5
        _HighlightSharpness ("Highlight Sharpness", Range(1, 32)) = 8
        _LightDepth ("Light Depth", Range(0.05, 2)) = 0.45
        _ReflectionStrength ("Reflection Strength", Range(0, 3)) = 1
        _SpriteColorInfluence ("Sprite Color Influence", Range(0, 1)) = 0
        _PreserveOriginalColor ("Preserve Original Color", Range(0, 1)) = 0
        _OriginalColorShading ("Original Color Shading", Range(0, 2)) = 0.45

        [NoScaleOffset] _HeightMap ("Height Map", 2D) = "gray" {}
        _LuminanceHeightInfluence ("Luminance Height Influence", Range(0, 1)) = 0.5
        _HeightMapInfluence ("Height Map Influence", Range(0, 1)) = 0
        _HeightContrast ("Height Contrast", Range(0.1, 4)) = 1.5

        [HideInInspector] _CardPointerPosition ("Card Pointer Position", Vector) = (0, 0, 0, 0)
        [HideInInspector] _CardTiltOffset ("Card Tilt Offset", Vector) = (0, 0, 0, 0)
        [HideInInspector] _CardLightDirection ("Card Light Direction", Vector) = (0.707, 0.707, 0, 0)
        [HideInInspector] _CardHighlightAngle ("Card Highlight Angle", Float) = 45
        [HideInInspector] _CardHighlightPosition ("Card Highlight Position", Vector) = (0, 0, 0, 0)
        [HideInInspector] _CardReflectionStrength ("Card Reflection Strength", Range(0, 1)) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "CardMetalEmbossOverlay"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _HeightMap;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _MetalColor;
            fixed4 _HighlightColor;
            fixed4 _ShadowColor;
            float4 _ClipRect;
            float _EmbossOpacity;
            float _ReliefStrength;
            float _EdgeWidth;
            float _HighlightSharpness;
            float _LightDepth;
            float _ReflectionStrength;
            float _SpriteColorInfluence;
            float _PreserveOriginalColor;
            float _OriginalColorShading;
            float _LuminanceHeightInfluence;
            float _HeightMapInfluence;
            float _HeightContrast;
            float4 _CardLightDirection;
            float _CardReflectionStrength;

            float GetEmbossHeight(float2 uv)
            {
                fixed4 spriteSample = tex2D(_MainTex, uv);
                float luminance = dot(
                    spriteSample.rgb,
                    float3(0.2126, 0.7152, 0.0722)) * spriteSample.a;
                float height = lerp(
                    spriteSample.a,
                    luminance,
                    _LuminanceHeightInfluence);
                float heightMap = tex2D(_HeightMap, uv).r * spriteSample.a;
                height = lerp(height, heightMap, _HeightMapInfluence);
                return saturate((height - 0.5) * _HeightContrast + 0.5);
            }

            v2f vert(appdata_t input)
            {
                v2f output;
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 spriteColor = tex2D(_MainTex, input.texcoord) * input.color;
                float2 texelOffset = _MainTex_TexelSize.xy * _EdgeWidth;
                float heightLeft = GetEmbossHeight(
                    input.texcoord - float2(texelOffset.x, 0));
                float heightRight = GetEmbossHeight(
                    input.texcoord + float2(texelOffset.x, 0));
                float heightDown = GetEmbossHeight(
                    input.texcoord - float2(0, texelOffset.y));
                float heightUp = GetEmbossHeight(
                    input.texcoord + float2(0, texelOffset.y));
                float2 heightGradient = float2(
                    heightRight - heightLeft,
                    heightUp - heightDown) * _ReliefStrength;
                float3 embossNormal = normalize(float3(-heightGradient, 1.0));
                float3 lightDirection = normalize(float3(
                    _CardLightDirection.xy,
                    max(_LightDepth, 0.001)));
                float diffuseLight = dot(embossNormal, lightDirection) * 0.5 + 0.5;
                float highlight = pow(
                    saturate(dot(embossNormal, lightDirection)),
                    _HighlightSharpness);
                float edgeStrength = saturate(length(heightGradient));

                float3 metalColor = lerp(
                    _ShadowColor.rgb,
                    _MetalColor.rgb,
                    diffuseLight);
                metalColor += _HighlightColor.rgb
                    * (highlight * edgeStrength * _ReflectionStrength
                    * (0.35 + _CardReflectionStrength * 0.65));
                metalColor *= lerp(1.0, spriteColor.rgb, _SpriteColorInfluence);

                float reflectionBoost = highlight * edgeStrength * _ReflectionStrength
                    * (0.35 + _CardReflectionStrength * 0.65);
                float2 reliefDirection = normalize(-heightGradient + float2(0.0001, 0.0001));
                float directionalRelief = dot(
                    reliefDirection,
                    normalize(_CardLightDirection.xy + float2(0.0001, 0.0001)));
                float originalColorLighting = 1.0
                    + directionalRelief * edgeStrength * _OriginalColorShading;
                originalColorLighting += reflectionBoost * _OriginalColorShading;
                float3 originalLitColor = spriteColor.rgb * max(0.0, originalColorLighting);

                fixed4 result;
                result.rgb = lerp(
                    metalColor,
                    originalLitColor,
                    _PreserveOriginalColor);
                result.a = spriteColor.a * _EmbossOpacity
                    * lerp(_MetalColor.a, 1.0, _PreserveOriginalColor);

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
