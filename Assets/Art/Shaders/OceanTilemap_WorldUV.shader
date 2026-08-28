Shader "InnsmouthCafe/OceanTilemap_WorldUV"
{
    Properties
    {
        [Header(Textures)]
        _GroundTex ("Ground Layer (海底层)", 2D) = "white" {}
        _NoiseTex ("Noise Layer (海面噪声层)", 2D) = "white" {}

        [Header(World Space Tiling)]
        _WorldTiling ("World Tiling (世界平铺倍数)", Float) = 0.125

        [Header(Blending)]
        _NoiseOpacity ("Noise Opacity (噪声不透明度)", Range(0, 1)) = 0.6
        _EdgePreserve ("Edge Preserve (保留边缘贴图强度)", Range(0, 1)) = 0.8

        [HideInInspector] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "OceanWorldUV"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 color : COLOR;
            };

            TEXTURE2D(_GroundTex);
            SAMPLER(sampler_GroundTex);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _WorldTiling;
                float _NoiseOpacity;
                float _EdgePreserve;
                float4 _Color;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = input.uv;
                output.color = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 采样原始瓦片贴图（边缘泡沫）
                float2 tileUV = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                half4 tileColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, tileUV);

                // 世界坐标 XZ 作为 UV（忽略 Y 轴）
                float2 worldUV = input.positionWS.xz * _WorldTiling;

                // 采样海底层
                half4 groundColor = SAMPLE_TEXTURE2D(_GroundTex, sampler_GroundTex, worldUV);

                // 采样海面噪声层
                half4 noiseColor = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, worldUV);

                // 混合：海面叠加在海底上（Overlay 混合模式）
                half3 oceanFill = lerp(groundColor.rgb,
                                       groundColor.rgb * noiseColor.rgb * 2.0,
                                       _NoiseOpacity);

                // 关键：如果瓦片贴图有内容（白色泡沫），保留它；否则用海底填充
                // 判断依据：瓦片亮度 > 阈值 = 有泡沫，保留
                float tileBrightness = dot(tileColor.rgb, float3(0.299, 0.587, 0.114));
                float isEdge = step(0.85, tileBrightness);  // 亮度 > 0.85 = 边缘泡沫（只保留很亮的白色）

                // 最终颜色：边缘保留瓦片颜色，填充区用海底+噪声
                half3 finalColor = lerp(oceanFill, tileColor.rgb, isEdge * _EdgePreserve);

                // 用 Tilemap 的 Vertex Color Alpha 作为遮罩（裁剪形状）
                half alpha = input.color.a * tileColor.a;

                return half4(finalColor * _Color.rgb, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
