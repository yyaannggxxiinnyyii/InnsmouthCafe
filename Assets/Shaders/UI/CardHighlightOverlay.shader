Shader "InnsmouthCafe/UI/Card Highlight Overlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [HDR] _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 1)
        _HighlightIntensity ("Highlight Intensity", Range(0, 6)) = 1.8
        _HighlightOpacity ("Highlight Opacity", Range(0, 1)) = 0.65
        _HighlightWidth ("Highlight Width", Range(0.01, 1)) = 0.18
        _HighlightSoftness ("Highlight Softness", Range(0.001, 1)) = 0.12
        _HighlightCenterOffset ("Highlight Center Offset", Range(-1, 1)) = -0.14
        _HighlightTravel ("Highlight Travel", Range(0, 1)) = 0.25

        [NoScaleOffset] _HeightMap ("Height Map", 2D) = "gray" {}
        _HeightMapInfluence ("Height Map Influence", Range(0, 1)) = 0
        _HeightContrast ("Height Contrast", Range(0.1, 4)) = 1.5
        _HeightSampleDistance ("Height Sample Distance", Range(0.25, 8)) = 1.5
        _SurfaceNormalStrength ("Surface Normal Strength", Range(0, 12)) = 4
        _SurfaceDistortion ("Surface Distortion", Range(0, 0.5)) = 0.08
        _SurfaceSpecularStrength ("Surface Specular Strength", Range(0, 4)) = 1.2
        _SurfaceSpecularSharpness ("Surface Specular Sharpness", Range(1, 64)) = 16
        _SurfaceLightDepth ("Surface Light Depth", Range(0.05, 2)) = 0.45

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
            Name "CardHighlightOverlay"

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
            float4 _HeightMap_TexelSize;
            fixed4 _Color;
            fixed4 _HighlightColor;
            float4 _ClipRect;
            float _HighlightIntensity;
            float _HighlightOpacity;
            float _HighlightWidth;
            float _HighlightSoftness;
            float _HighlightCenterOffset;
            float _HighlightTravel;
            float _HeightMapInfluence;
            float _HeightContrast;
            float _HeightSampleDistance;
            float _SurfaceNormalStrength;
            float _SurfaceDistortion;
            float _SurfaceSpecularStrength;
            float _SurfaceSpecularSharpness;
            float _SurfaceLightDepth;
            float4 _CardLightDirection;
            float4 _CardHighlightPosition;

            float GetSurfaceHeight(float2 uv)
            {
                float height = tex2D(_HeightMap, uv).r;
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
                float2 centeredUv = input.texcoord - 0.5;
                float2 lightDirection = normalize(_CardLightDirection.xy + float2(0.0001, 0.0001));
                float highlightAxis = dot(centeredUv, lightDirection);
                float surfaceResponse = 1.0;
                if (_HeightMapInfluence > 0.001)
                {
                    float2 heightTexelOffset = _HeightMap_TexelSize.xy
                        * _HeightSampleDistance;
                    float heightLeft = GetSurfaceHeight(
                        input.texcoord - float2(heightTexelOffset.x, 0));
                    float heightRight = GetSurfaceHeight(
                        input.texcoord + float2(heightTexelOffset.x, 0));
                    float heightDown = GetSurfaceHeight(
                        input.texcoord - float2(0, heightTexelOffset.y));
                    float heightUp = GetSurfaceHeight(
                        input.texcoord + float2(0, heightTexelOffset.y));
                    float2 heightGradient = float2(
                        heightRight - heightLeft,
                        heightUp - heightDown) * _SurfaceNormalStrength;

                    highlightAxis += dot(heightGradient, lightDirection)
                        * _SurfaceDistortion * _HeightMapInfluence;
                    float3 surfaceNormal = normalize(float3(-heightGradient, 1.0));
                    float3 surfaceLightDirection = normalize(float3(
                        lightDirection,
                        max(_SurfaceLightDepth, 0.001)));
                    float3 halfDirection = normalize(
                        surfaceLightDirection + float3(0.0, 0.0, 1.0));
                    float surfaceSpecular = pow(
                        saturate(dot(surfaceNormal, halfDirection)),
                        _SurfaceSpecularSharpness);
                    float flatSpecular = pow(
                        saturate(halfDirection.z),
                        _SurfaceSpecularSharpness);
                    surfaceResponse = max(
                        0.0,
                        1.0 + (surfaceSpecular - flatSpecular)
                        * _SurfaceSpecularStrength);
                }

                float highlightCenter = _HighlightCenterOffset
                    + dot(_CardHighlightPosition.xy, lightDirection) * _HighlightTravel;
                float highlightDistance = abs(highlightAxis - highlightCenter);
                float highlight = 1.0 - smoothstep(
                    _HighlightWidth,
                    _HighlightWidth + _HighlightSoftness,
                    highlightDistance);
                highlight *= lerp(1.0, surfaceResponse, _HeightMapInfluence);

                fixed4 result;
                result.rgb = _HighlightColor.rgb * (_HighlightIntensity * highlight);
                result.a = spriteColor.a * _HighlightColor.a
                    * (_HighlightOpacity * highlight);

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
