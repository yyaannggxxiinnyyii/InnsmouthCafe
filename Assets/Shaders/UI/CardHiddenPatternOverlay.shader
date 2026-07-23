Shader "InnsmouthCafe/UI/Card Hidden Pattern Overlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [HDR] _PatternColor ("Pattern Color", Color) = (0.7, 0.95, 1, 1)
        _PatternIntensity ("Pattern Intensity", Range(0, 6)) = 1.5
        _PatternOpacity ("Pattern Opacity", Range(0, 1)) = 0.8
        _IdleOpacity ("Idle Opacity", Range(0, 1)) = 0

        _RevealAngle ("Reveal Angle", Range(-180, 180)) = 45
        _RevealHalfRange ("Reveal Half Range", Range(0.1, 180)) = 20
        _RevealSoftness ("Reveal Softness", Range(0.1, 90)) = 12
        _ReflectionInfluence ("Reflection Influence", Range(0, 1)) = 1

        _SweepInfluence ("Sweep Influence", Range(0, 1)) = 0
        _SweepWidth ("Sweep Width", Range(0.01, 1)) = 0.22
        _SweepSoftness ("Sweep Softness", Range(0.001, 1)) = 0.16
        _SweepCenterOffset ("Sweep Center Offset", Range(-1, 1)) = 0
        _SweepTravel ("Sweep Travel", Range(0, 1)) = 0.25

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
            Name "CardHiddenPatternOverlay"

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
            fixed4 _Color;
            fixed4 _PatternColor;
            float4 _ClipRect;
            float _PatternIntensity;
            float _PatternOpacity;
            float _IdleOpacity;
            float _RevealAngle;
            float _RevealHalfRange;
            float _RevealSoftness;
            float _ReflectionInfluence;
            float _SweepInfluence;
            float _SweepWidth;
            float _SweepSoftness;
            float _SweepCenterOffset;
            float _SweepTravel;
            float4 _CardLightDirection;
            float _CardHighlightAngle;
            float4 _CardHighlightPosition;
            float _CardReflectionStrength;

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
                float angleDifferenceRadians = radians(_CardHighlightAngle - _RevealAngle);
                float angleDifference = abs(atan2(
                    sin(angleDifferenceRadians),
                    cos(angleDifferenceRadians))) * 57.2957795;
                float angleReveal = 1.0 - smoothstep(
                    _RevealHalfRange,
                    _RevealHalfRange + _RevealSoftness,
                    angleDifference);
                float reflectionResponse = lerp(
                    1.0,
                    _CardReflectionStrength,
                    _ReflectionInfluence);

                float2 centeredUv = input.texcoord - 0.5;
                float2 lightDirection = normalize(_CardLightDirection.xy + float2(0.0001, 0.0001));
                float sweepAxis = dot(centeredUv, lightDirection);
                float sweepCenter = _SweepCenterOffset
                    + dot(_CardHighlightPosition.xy, lightDirection) * _SweepTravel;
                float sweepDistance = abs(sweepAxis - sweepCenter);
                float sweepReveal = 1.0 - smoothstep(
                    _SweepWidth,
                    _SweepWidth + _SweepSoftness,
                    sweepDistance);

                float reveal = angleReveal * reflectionResponse;
                reveal *= lerp(1.0, sweepReveal, _SweepInfluence);
                float visibility = saturate(_IdleOpacity + reveal * (1.0 - _IdleOpacity));

                fixed4 result;
                result.rgb = _PatternColor.rgb * _PatternIntensity;
                result.a = spriteColor.a * _PatternColor.a
                    * (_PatternOpacity * visibility);

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
