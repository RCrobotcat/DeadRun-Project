Shader "RCrobotcat/Ocean"
{
    Properties
    {
        _DepthScale ("Depth Scale", Range(1, 1000)) = 20 // �������ˮ���ܼ���
        _DistortionScale ("Distortion Scale", Range(0, 0.001)) = 0.0001 // ��������Ť���ĳ̶�
        _SSSDisplacement ("SSS Displacement Scale", Range(0.1, 20)) = 12 // �˵�λ�ƶ�ɢ��Ĺ���ϵ��
        _SSSBase ("SSS Base Scale", Range(0.1, 2)) = 0.8 // ������ɢ�乱��ϵ��
        _SSSScale ("SSS Scale", Range(0, 50)) = 20 // ɢ��ǿ��
        _CausticsTexture("Caustics", 2D) = "white" {} // ˮ�׽�ɢ��ͼ
        _NormalBase("Normal Base", 2D) = "white" {} // ������ͼ
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            // Blend SrcAlpha One // ���ģʽ

            HLSLPROGRAM
            #define _MAIN_LIGHT_SHADOWS
            #define _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            #include "GerstnerWaves.hlsl"
            #include "PlanarReflections.hlsl"
            #include "Lighting.hlsl"
            #include "Volume.hlsl"
            #include "Cascade.hlsl"

            #pragma vertex OceanMainVert
            #pragma fragment OceanMainFrag

            struct Attributes
            {
                float4 positionOS : POSITION; // ģ�Ϳռ��е�λ��
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION; // ���������ռ��е�λ��
                float3 positionWS : TEXCOORD0; // ����ռ��λ��
                float3 worldNormal : TEXCOORD1; // ����ռ�ķ���
                float4 positionNDC : TEXCOORD2;
                // �ڹ�һ���豸����ռ��е�λ��(NDC: normalized device coordinate => ���ڱ�ʾ��������Ļ�ϵ�λ�ã�ȡֵ��-1��1)
                float4 distanceData : TEXCOORD3; // x = distance to surface, y = distance to surface
                float2 worldUV : TEXCOORD4; // ����ռ��UV����
            };

            // depth
            float _DepthScale;
            float _DistortionScale;

            // SSS
            float _SSSDisplacement;
            float _SSSBase;
            float _SSSScale;

            // Caustics
            TEXTURE2D(_CausticsTexture);
            SAMPLER(sampler_CausticsTexture);

            // Normal
            TEXTURE2D(_NormalBase);
            SAMPLER(sampler_NormalBase);

            half3 ApplyCaustics(half2 screenUV)
            {
                float2 UV = screenUV;

                // ��ȡ���
                // UNITY_REVERSED_Z ��: �������ֲ�ͬӲ����API�Ĳ���
                #if UNITY_REVERSED_Z
                real depth = SampleSceneDepth(UV);
                #else
                // Adjust Z to match NDC for OpenGL ([-1, 1]) NDC=>normalized device coordinate
                real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
                #endif

                // UNITY_MATRIX_I_VP Ϊ VP�����
                float3 worldPosition = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);

                //Light mainLight = GetMainLight(TransformWorldToShadowCoord(worldPosition));
                //float backGroundShadow = mainLight.shadowAttenuation;//MainLightRealtimeShadow(TransformWorldToShadowCoord(worldPosition));

                float backGroundShadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(worldPosition));

                float2 depthRange = float2(-50, -1);
                float decayRange = 5; // ���ڵ�����ɢ

                float worldY = worldPosition.y;

                //caustics intensity
                float distanceToMin = abs(worldY - depthRange.x);
                float distanceToMax = abs(worldY - depthRange.y);

                float intensity = (worldY > depthRange.x && worldY < depthRange.y)
                                      ? saturate(
                                          min(distanceToMin, distanceToMax) /
                                          decayRange)
                                      : 0.0;
                intensity *= backGroundShadow;
                float scale = lerp(1, 0.2, saturate(-worldY / 500.0)); // ʹ�ý�ɢ������ǳˮС����ˮ��

                // �任��ɢ��ͼ������UV�ĳߴ��ƫ��
                float4 causticsUV_ST1 = float4(0.1, 0.1, 0.2, 0);
                float4 causticsUV_ST2 = float4(0.1, 0.1, 0, 0.3);

                float causticsSpeed1 = 0.08;
                float causticsSpeed2 = 0.03;

                float2 causticsUV1 = worldPosition.xz * scale * causticsUV_ST1.xy + causticsUV_ST1.zw;
                causticsUV1 += causticsSpeed1 * _Time.y;

                float2 causticsUV2 = worldPosition.xz * scale * causticsUV_ST2.xy + causticsUV_ST2.zw;
                causticsUV2 += causticsSpeed2 * _Time.y;

                float4 causticsColor1 = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_CausticsTexture, causticsUV1);
                float4 causticsColor2 = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_CausticsTexture, causticsUV2);

                return min(causticsColor1.xyz, causticsColor2.xyz) * intensity * 5;
            }

            half4 DistanceData(float3 postionWS, float displacement)
            {
                half4 data = half4(0.0, 0.0, 0.0, 0.0);
                float3 viewPos = TransformWorldToView(postionWS);
                data.x = length(viewPos / viewPos.z); // distance to surface
                data.y = length(GetCameraPositionWS().xyz - postionWS); // �ӵ㵽ˮ��ľ���
                data.z = displacement;
                return data;
            }

            // ���ˮ��
            float AdjustedDepth(half2 uvs, half4 additionalData)
            {
                float rawD = SampleSceneDepth(uvs); // ��ȡ���ͼ�е�ֵ
                float d = LinearEyeDepth(rawD, _ZBufferParams); // ��ȡView Space�е�ʵ�����ֵ

                return d * additionalData.x - additionalData.y;
            }

            // ����������ɫ
            float3 Refraction(float2 distortion, float depth)
            {
                float3 output = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, distortion).rgb;
                output += ApplyCaustics(distortion);
                output *= Absorption(saturate(depth / _DepthScale));
                return output;
            }

            // ����Ť����UV����
            half2 DistortionUVs(float depth, float3 normalWS)
            {
                // �����߱任����Ļ�ռ�
                half3 viewNormal = mul((float3x3)GetWorldToHClipMatrix(), -normalWS).xyz;
                float2 distortedUV = viewNormal.xz * saturate((depth) * _DistortionScale);
                return distortedUV;
            }

            // ���㷴��ķ������
            half CalculateFresnelTerm(half3 normalWS, half3 viewDirWS)
            {
                /*
                 * ���������: ���ߺ����ߵļн�ԽС�������Խǿ�������Խ��
                 * ��֮�������Խǿ�������Խ��
                 */
                float f = saturate(pow(1.0 - dot(normalWS, viewDirWS), 3));
                return f;
            }

            // ����ˮ������
            half4 WaterShading(half2 screenUV, float3 positionWS, float3 normal, float3 viewDir, half4 distanceData)
            {
                // Depth: ���
                float depth = AdjustedDepth(screenUV, distanceData);

                // Distortion: Ť��
                half2 distortionDelta = DistortionUVs(depth, normal);
                half2 distortion;
                distortion = screenUV + distortionDelta;
                float d = depth;
                depth = AdjustedDepth(distortion, distanceData);

                // �ж�Opaque��ͼ�ϵ�������λ��ˮ�ϻ���ˮ��
                distortion = depth < 0 ? screenUV : screenUV + distortionDelta;
                depth = depth < 0 ? d : depth;

                // ���� Refraction
                half3 refraction = Refraction(distortion, depth);

                // ���� Reflection
                half3 reflection = SampleReflections(normal, viewDir, distortion);
                half fresnelTerm = CalculateFresnelTerm(normal, viewDir);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS)); // ��ȡ�ƹ�
                half shadow = lerp(0.7, 1, mainLight.shadowAttenuation); // ��ֵ������Ӱ
                half3 GI = SampleSH(normal); // ȫ�ֹ���

                // BRDF������PBR�߹�ĺ���
                BRDFData brdfData;
                half alpha = 1;
                // parameters: albedo, metallic, specular, smoothness, alpha, (out) BRDFData
                InitializeBRDFData(half3(0, 0, 0), 0, half3(1, 1, 1), 0.98, alpha, brdfData);
                float spec = (DirectBRDF(brdfData, normal, mainLight.direction, viewDir) - brdfData.diffuse) * shadow *
                    mainLight.color;

                half3 directLighting = brdfData.diffuse * mainLight.color;

                float sssWaveFactor = max((distanceData.z * _SSSDisplacement), 0) * _SSSScale;
                directLighting += saturate(pow(saturate(dot(viewDir, -mainLight.direction)), 3)) * sssWaveFactor *
                    mainLight.color;
                half3 sss = directLighting * shadow + GI; // �ϲ���Ӱ��ȫ�ֹ���

                // ɢ�� Scattering
                sss *= Scattering(saturate(depth / _DepthScale));

                // ˮ����ɫ = ((������ɫ,������ɫ) ���� ������� �Ĳ�ֵ) * ��Ӱ + ɢ��
                half3 waterColor = (lerp(refraction - 0.1, reflection, fresnelTerm) + spec) * shadow + sss;

                return half4(waterColor, 1);
            }

            // ������ɫ��
            Varyings OceanMainVert(Attributes input)
            {
                Varyings output;

                float3 positionWS = mul(unity_ObjectToWorld, input.positionOS).xyz;
                output.worldUV = positionWS.xz;

                float3 displacement = 0;
                float3 normal;

                WaveStruct wave;
                SampleWaves(positionWS, wave);
                displacement += wave.position;
                output.worldNormal = wave.normal;

                displacement += SampleWaveDisplacement(output.worldUV).xyz;
                output.positionWS = positionWS + displacement;

                float3 positionOS = TransformWorldToObject(output.positionWS);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                output.positionHCS = positionInputs.positionCS;
                output.positionNDC = positionInputs.positionNDC;

                output.distanceData = DistanceData(output.positionWS, displacement.y);

                return output;
            }

            // ��ȡ���ߵ�ˮƽƫ����Ϊ����
            float2 GetWaves(float2 coords, half scale, half amplitude, half speed, float2 dir)
            {
                float time = _Time.y * speed;
                float2 offsetCoords = (coords + dir * time) / scale;

                // ���˵ķ���
                float3 normal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalBase, sampler_NormalBase, offsetCoords));
                return normal.xy * amplitude;
            }

            // ƬԪ��ɫ��
            float4 OceanMainFrag(Varyings input) : SV_Target
            {
                half2 screenUV = input.positionNDC.xy / input.positionNDC.w;

                float3 viewDir = _WorldSpaceCameraPos - input.positionWS;
                float viewDist = length(viewDir);
                viewDir = viewDir / viewDist;

                float3 worldNormal = normalize(input.worldNormal);

                worldNormal += SampleWaveNormal(input.worldUV, 0).xyz;
                worldNormal = normalize(worldNormal);

                float3 WaveScale = float3(20.18, 11.8, 8.63);
                float3 WaveAmplitude = float3(0.25, 0.175, 0.15);
                float3 WaveSpeed = float3(1.6, 1.5, 1);

                float2 nrm = GetWaves(input.worldUV, WaveScale.x, WaveAmplitude.x, WaveSpeed.x, half2(1, 0.5));
                nrm += GetWaves(input.worldUV, WaveScale.y, WaveAmplitude.y, WaveSpeed.y, half2(-1, -0.5));
                nrm += GetWaves(input.worldUV, WaveScale.z, WaveAmplitude.z, WaveSpeed.z, half2(0.5, -1));

                nrm /= 3;

                worldNormal.xz += nrm;
                worldNormal = normalize(worldNormal);

                float4 oceanColor = WaterShading(screenUV, input.positionWS, worldNormal, viewDir,
                                              input.distanceData);
                return oceanColor;
            }
            ENDHLSL
        }
    }
}