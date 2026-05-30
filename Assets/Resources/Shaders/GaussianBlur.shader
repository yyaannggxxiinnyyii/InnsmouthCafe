Shader "InnsmouthCafe/GaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0, 10)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        // Pass 1: 水平模糊
        Pass
        {
            Name "HorizontalBlur"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy * _BlurSize;

                // 9-tap 高斯权重（sigma ≈ 1.5）
                fixed4 col = fixed4(0, 0, 0, 0);
                col += tex2D(_MainTex, i.uv + float2(-4.0 * texel.x, 0)) * 0.0162;
                col += tex2D(_MainTex, i.uv + float2(-3.0 * texel.x, 0)) * 0.0540;
                col += tex2D(_MainTex, i.uv + float2(-2.0 * texel.x, 0)) * 0.1216;
                col += tex2D(_MainTex, i.uv + float2(-1.0 * texel.x, 0)) * 0.1945;
                col += tex2D(_MainTex, i.uv)                              * 0.2270;
                col += tex2D(_MainTex, i.uv + float2( 1.0 * texel.x, 0)) * 0.1945;
                col += tex2D(_MainTex, i.uv + float2( 2.0 * texel.x, 0)) * 0.1216;
                col += tex2D(_MainTex, i.uv + float2( 3.0 * texel.x, 0)) * 0.0540;
                col += tex2D(_MainTex, i.uv + float2( 4.0 * texel.x, 0)) * 0.0162;
                return col;
            }
            ENDCG
        }

        // Pass 2: 垂直模糊
        Pass
        {
            Name "VerticalBlur"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy * _BlurSize;

                fixed4 col = fixed4(0, 0, 0, 0);
                col += tex2D(_MainTex, i.uv + float2(0, -4.0 * texel.y)) * 0.0162;
                col += tex2D(_MainTex, i.uv + float2(0, -3.0 * texel.y)) * 0.0540;
                col += tex2D(_MainTex, i.uv + float2(0, -2.0 * texel.y)) * 0.1216;
                col += tex2D(_MainTex, i.uv + float2(0, -1.0 * texel.y)) * 0.1945;
                col += tex2D(_MainTex, i.uv)                              * 0.2270;
                col += tex2D(_MainTex, i.uv + float2(0,  1.0 * texel.y)) * 0.1945;
                col += tex2D(_MainTex, i.uv + float2(0,  2.0 * texel.y)) * 0.1216;
                col += tex2D(_MainTex, i.uv + float2(0,  3.0 * texel.y)) * 0.0540;
                col += tex2D(_MainTex, i.uv + float2(0,  4.0 * texel.y)) * 0.0162;
                return col;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
