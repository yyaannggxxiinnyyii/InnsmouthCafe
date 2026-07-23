Shader "InnsmouthCafe/UI/Holographic Card Overlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [HDR] _EffectTint ("Effect Tint", Color) = (1, 1, 1, 1)
        _HolographicIntensity ("Holographic Intensity", Range(0, 2)) = 0.8
        _HolographicOpacity ("Holographic Opacity", Range(0, 1)) = 0.45
        _BandScale ("Band Scale", Range(0.1, 20)) = 4
        _BandSpeed ("Band Speed", Range(-2, 2)) = 0.2
        _BandAngle ("Band Angle", Range(-2, 2)) = 0.6
        _EdgeReflection ("Edge Reflection", Range(0, 2)) = 0.5
        _IdleShimmer ("Idle Shimmer", Range(0, 1)) = 0.05
        [HideInInspector] _TiltOffset ("Tilt Offset", Vector) = (0, 0, 0, 0)

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
            Name "HolographicOverlay"

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
            fixed4 _EffectTint;
            float4 _ClipRect;
            float _HolographicIntensity;
            float _HolographicOpacity;
            float _BandScale;
            float _BandSpeed;
            float _BandAngle;
            float _EdgeReflection;
            float _IdleShimmer;
            float4 _TiltOffset;

            /// <summary>
            /// 根据循环相位生成连续的彩虹颜色。
            /// </summary>
            float3 GetSpectrum(float phase)
            {
                float3 shiftedPhase = phase + float3(0.0, 0.33333334, 0.66666669);
                return saturate(abs(frac(shiftedPhase) * 6.0 - 3.0) - 1.0);
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
                float tiltMagnitude = saturate(length(_TiltOffset.xy));
                float idleOffset = _Time.y * _BandSpeed * _IdleShimmer;
                float phase = input.texcoord.x + input.texcoord.y * _BandAngle;
                phase += dot(centeredUv, _TiltOffset.xy) * 0.18 + idleOffset;
                float band = sin(phase * _BandScale * 6.28318531) * 0.5 + 0.5;
                float3 spectrum = GetSpectrum(phase) * _EffectTint.rgb;
                float edgeDistance = max(abs(centeredUv.x), abs(centeredUv.y)) * 2.0;
                float edgeReflection = pow(saturate(edgeDistance), 4.0) * tiltMagnitude;

                fixed4 result;
                result.rgb = spectrum * (band * _HolographicIntensity);
                result.rgb += spectrum * (edgeReflection * _EdgeReflection);
                result.a = spriteColor.a * _HolographicOpacity
                    * (0.35 + band * 0.45 + edgeReflection * 0.2);

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
