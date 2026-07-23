Shader "InnsmouthCafe/UI/Card Sparkle Overlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [HDR] _SparkleColor ("Sparkle Color", Color) = (1, 1, 1, 1)
        _SparkleIntensity ("Sparkle Intensity", Range(0, 6)) = 2
        _SparkleOpacity ("Sparkle Opacity", Range(0, 1)) = 0.8
        _SparkleDensity ("Sparkle Density", Range(8, 240)) = 100
        _SparkleAmount ("Sparkle Amount", Range(0, 1)) = 0.08
        _SparkleSize ("Sparkle Size", Range(0.01, 0.5)) = 0.12
        _SparkleSoftness ("Sparkle Softness", Range(0.001, 0.5)) = 0.08
        [NoScaleOffset] _SparkleShapeTex ("Sparkle Shape Texture", 2D) = "white" {}
        _ShapeTextureInfluence ("Shape Texture Influence", Range(0, 1)) = 0
        _ShapeRotationOffset ("Shape Rotation Offset", Range(-180, 180)) = 0
        _RandomRotation ("Random Rotation", Range(0, 1)) = 1
        _RandomScale ("Random Scale", Range(0, 0.8)) = 0.25
        _AngleSharpness ("Angle Sharpness", Range(1, 32)) = 10
        _IdleSparkleAmount ("Idle Sparkle Amount", Range(0, 1)) = 0.2
        _IdleBrightness ("Idle Brightness", Range(0, 1)) = 0.08
        _TwinkleSpeed ("Twinkle Speed", Range(0, 8)) = 0
        _TwinkleSharpness ("Twinkle Sharpness", Range(1, 16)) = 4

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
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            Name "CardSparkleOverlay"

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
            sampler2D _SparkleShapeTex;
            fixed4 _Color;
            fixed4 _SparkleColor;
            float4 _ClipRect;
            float _SparkleIntensity;
            float _SparkleOpacity;
            float _SparkleDensity;
            float _SparkleAmount;
            float _SparkleSize;
            float _SparkleSoftness;
            float _ShapeTextureInfluence;
            float _ShapeRotationOffset;
            float _RandomRotation;
            float _RandomScale;
            float _AngleSharpness;
            float _IdleSparkleAmount;
            float _IdleBrightness;
            float _TwinkleSpeed;
            float _TwinkleSharpness;
            float _CardHighlightAngle;
            float _CardReflectionStrength;

            float Hash21(float2 value)
            {
                return frac(sin(dot(value, float2(12.9898, 78.233))) * 43758.5453);
            }

            float2 Hash22(float2 value)
            {
                return frac(sin(float2(
                    dot(value, float2(127.1, 311.7)),
                    dot(value, float2(269.5, 183.3)))) * 43758.5453);
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
                float2 sparkleUv = input.texcoord * _SparkleDensity;
                float2 cell = floor(sparkleUv);
                float2 cellPosition = frac(sparkleUv) - 0.5;
                float2 sparkleOffset = (Hash22(cell) - 0.5) * 0.7;
                float2 sparkleLocalPosition = cellPosition - sparkleOffset;
                float randomScale = lerp(
                    1.0 - _RandomScale,
                    1.0 + _RandomScale,
                    Hash21(cell + 41.37));
                float sparkleRadius = max(_SparkleSize * randomScale, 0.001);
                float sparkleDistance = length(sparkleLocalPosition);
                float proceduralShape = 1.0 - smoothstep(
                    sparkleRadius,
                    sparkleRadius + _SparkleSoftness,
                    sparkleDistance);

                float randomAngle = (Hash21(cell + 83.91) * 360.0 - 180.0)
                    * _RandomRotation;
                float shapeAngle = radians(_ShapeRotationOffset + randomAngle);
                float shapeSin = sin(shapeAngle);
                float shapeCos = cos(shapeAngle);
                float2 rotatedShapePosition = float2(
                    shapeCos * sparkleLocalPosition.x + shapeSin * sparkleLocalPosition.y,
                    -shapeSin * sparkleLocalPosition.x + shapeCos * sparkleLocalPosition.y);
                float2 shapeUv = rotatedShapePosition / (sparkleRadius * 2.0) + 0.5;
                float shapeBounds = step(0.0, shapeUv.x)
                    * step(shapeUv.x, 1.0)
                    * step(0.0, shapeUv.y)
                    * step(shapeUv.y, 1.0);
                float textureShape = tex2D(_SparkleShapeTex, saturate(shapeUv)).a
                    * shapeBounds;
                float sparkleShape = lerp(
                    proceduralShape,
                    textureShape,
                    _ShapeTextureInfluence);
                float sparkleSelection = step(1.0 - _SparkleAmount, Hash21(cell));
                float idleSelection = step(
                    1.0 - _IdleSparkleAmount,
                    Hash21(cell + 127.53));
                float activeSelection = lerp(
                    idleSelection,
                    1.0,
                    _CardReflectionStrength);

                float sparkleAngle = Hash21(cell + 19.73) * 360.0;
                float angleDifference = radians(_CardHighlightAngle - sparkleAngle);
                float angleResponse = pow(
                    saturate(cos(angleDifference) * 0.5 + 0.5),
                    _AngleSharpness);
                float twinkle = 1.0;
                if (_TwinkleSpeed > 0.001)
                {
                    float twinklePhase = sin(
                        _Time.y * _TwinkleSpeed
                        + Hash21(cell + 7.13) * 6.28318531) * 0.5 + 0.5;
                    twinkle = pow(saturate(twinklePhase), _TwinkleSharpness);
                }

                float sparkle = sparkleShape * sparkleSelection
                    * activeSelection * twinkle;
                float brightness = _IdleBrightness
                    + angleResponse * _CardReflectionStrength;
                fixed4 result;
                result.rgb = _SparkleColor.rgb
                    * (sparkle * brightness * _SparkleIntensity);
                result.a = spriteColor.a * _SparkleColor.a
                    * (sparkle * brightness * _SparkleOpacity);

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
