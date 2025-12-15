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
        _Transparency ("Transparency", Range(0, 1)) = 0.3 // 투명도 (0 = 투명, 1 = 불투명)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            // 반투명 블렌딩 설정
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

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
                float _Transparency;
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

                // 카메라의 XZ 평면 위치만 사용 (Y축 높이 무시)
                // 이렇게 하면 카메라가 위아래로 움직여도 페이드가 일정함
                float3 cameraPos = GetCameraPositionWS();
                float2 cameraXZ = cameraPos.xz;
                float2 pixelXZ = input.positionWS.xz;
                float distanceToCamera = length(pixelXZ - cameraXZ);

                // 부드러운 페이드 (smoothstep 사용으로 번쩍임 감소)
                float fadeStart = _FadeDistance * 0.5; // 페이드 시작 거리
                float fadeEnd = _FadeDistance;          // 완전 투명 거리
                float fade = 1.0 - smoothstep(fadeStart, fadeEnd, distanceToCamera);

                // 글로우 효과 (일정한 강도, 거리와 무관)
                float glow = pow(grid, _GlowIntensity);

                // 배경색과 그리드색 블렌딩
                half3 color = lerp(_BackgroundColor.rgb, _GridColor.rgb, grid);

                // Emission 추가 (네온 발광 효과) - 거리와 무관하게 일정
                half3 emission = _GridColor.rgb * glow * _EmissionStrength;
                color += emission;

                // 투명도 계산 (그리드 라인은 더 불투명, 배경은 더 투명)
                float alpha = lerp(_Transparency * 0.3, _Transparency, grid);
                alpha *= fade; // 거리에 따라 부드럽게 투명도 증가

                // 최종 색상 (Alpha 포함)
                half4 finalColor = half4(color, alpha);

                // Fog 적용
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
