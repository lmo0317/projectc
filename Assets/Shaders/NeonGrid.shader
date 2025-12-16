Shader "Custom/NeonGrid"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (0, 1, 1, 1) // 사이안 네온
        _BackgroundColor ("Background Color", Color) = (0, 0, 0.1, 1) // 어두운 배경
        _GridSize ("Grid Size", Float) = 1.0
        _LineWidth ("Line Width", Range(0.001, 0.1)) = 0.015
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

            // 안정적인 그리드 라인 계산
            float GridLine(float coord, float lineWidth)
            {
                // 그리드 셀 내 위치 (0~1)
                float cellPos = frac(coord);
                // 셀 가장자리(0 또는 1)로부터의 거리 (0~0.5)
                float distFromLine = min(cellPos, 1.0 - cellPos);
                // 라인 영역: lineWidth/2 이내면 라인
                float halfWidth = lineWidth * 0.5;
                // smoothstep으로 부드러운 엣지 (얇은 안티앨리어싱)
                return 1.0 - smoothstep(halfWidth * 0.8, halfWidth, distFromLine);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // World Space UV 사용 (월드 공간 기준 그리드)
                float2 worldUV = input.positionWS.xz * _GridSize;

                // 각 축의 그리드 라인 계산
                float lineX = GridLine(worldUV.x, _LineWidth);
                float lineY = GridLine(worldUV.y, _LineWidth);

                // 두 라인 합성 (OR 연산)
                float grid = max(lineX, lineY);

                // 카메라의 XZ 평면 위치만 사용
                float3 cameraPos = GetCameraPositionWS();
                float2 cameraXZ = cameraPos.xz;
                float2 pixelXZ = input.positionWS.xz;
                float distanceToCamera = length(pixelXZ - cameraXZ);

                // 부드러운 페이드
                float fadeStart = _FadeDistance * 0.5;
                float fadeEnd = _FadeDistance;
                float fade = 1.0 - smoothstep(fadeStart, fadeEnd, distanceToCamera);

                // 글로우 효과 - 라인 주변에 부드러운 글로우 추가
                float glowWidth = _LineWidth * 3.0; // 글로우 범위
                float glowX = GridLine(worldUV.x, glowWidth);
                float glowY = GridLine(worldUV.y, glowWidth);
                float glowArea = max(glowX, glowY);

                // 글로우 강도 (라인에서 멀어질수록 약해짐)
                float glowStrength = glowArea * (1.0 - grid * 0.5) * _GlowIntensity * 0.3;

                // 배경색과 그리드색 블렌딩
                half3 color = lerp(_BackgroundColor.rgb, _GridColor.rgb, grid);

                // Emission 추가 (일정한 네온 발광)
                half3 emission = _GridColor.rgb * (grid + glowStrength) * _EmissionStrength;
                color += emission;

                // 투명도 계산
                float alpha = lerp(_Transparency * 0.3, _Transparency, saturate(grid + glowStrength * 0.5));
                alpha *= fade;

                // 최종 색상
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
