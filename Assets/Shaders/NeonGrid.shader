Shader "Custom/NeonGrid"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (0, 1, 1, 1) // 사이안 네온
        _BackgroundColor ("Background Color", Color) = (0, 0, 0.1, 1) // 어두운 배경
        _GridSize ("Grid Size", Float) = 1.0
        _LineWidth ("Line Width", Range(0.01, 0.5)) = 0.05
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 2.0
        _FadeDistance ("Fade Distance", Float) = 50.0
        _EmissionStrength ("Emission Strength", Range(0, 10)) = 3.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GridColor;
                float4 _BackgroundColor;
                float _GridSize;
                float _LineWidth;
                float _GlowIntensity;
                float _FadeDistance;
                float _EmissionStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            // 그리드 패턴 생성 함수
            float GridPattern(float2 uv, float size, float lineWidth)
            {
                float2 grid = abs(frac(uv * size - 0.5) - 0.5) / fwidth(uv * size);
                float gridLine = min(grid.x, grid.y);
                return 1.0 - saturate(gridLine - lineWidth);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // World Space UV 사용 (월드 공간 기준 그리드)
                float2 worldUV = input.positionWS.xz;

                // 그리드 패턴 생성
                float grid = GridPattern(worldUV, _GridSize, _LineWidth);

                // 카메라 거리 기반 페이드
                float3 cameraPos = GetCameraPositionWS();
                float distanceToCamera = length(input.positionWS - cameraPos);
                float fade = saturate(1.0 - (distanceToCamera / _FadeDistance));

                // 글로우 효과 (거리 기반)
                float glow = pow(grid, _GlowIntensity) * fade;

                // 배경색과 그리드색 블렌딩
                half3 color = lerp(_BackgroundColor.rgb, _GridColor.rgb, grid);

                // Emission 추가 (네온 발광 효과)
                half3 emission = _GridColor.rgb * glow * _EmissionStrength;
                color += emission;

                // 최종 색상
                half4 finalColor = half4(color, 1.0);

                // Fog 적용
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
