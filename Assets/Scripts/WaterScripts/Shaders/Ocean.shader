Shader "RCrobotcat/Ocean"
{
    Properties
    {
        _DepthScale ("Depth Scale", Range(1, 1000)) = 20 // 整体控制水的能见度
        _DistortionScale ("Distortion Scale", Range(0, 0.001)) = 0.0001 // 设置折射扭曲的程度
        _SSSDisplacement ("SSS Displacement Scale", Range(0.1, 20)) = 12 // 浪的位移对散射的贡献系数
        _SSSBase ("SSS Base Scale", Range(0.1, 2)) = 0.8 // 基础的散射贡献系数
        _SSSScale ("SSS Scale", Range(0, 50)) = 20 // 散射强度
        _CausticsTexture("Caustics", 2D) = "white" {} // 水底焦散贴图
        _NormalBase("Normal Base", 2D) = "white" {} // 法线贴图
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            // Blend SrcAlpha One // 混合模式

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
                float4 positionOS : POSITION; // 模型空间中的位置
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION; // 在齐次坐标空间中的位置
                float3 positionWS : TEXCOORD0; // 世界空间的位置
                float3 worldNormal : TEXCOORD1; // 世界空间的法线
                float4 positionNDC : TEXCOORD2;
                // 在归一化设备坐标空间中的位置(NDC: normalized device coordinate => 用于表示顶点在屏幕上的位置，取值在-1到1)
                float4 distanceData : TEXCOORD3; // x = distance to surface, y = distance to surface
                float2 worldUV : TEXCOORD4; // 世界空间的UV坐标
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

                // 获取深度
                // UNITY_REVERSED_Z 宏: 可以区分不同硬件的API的差异
                #if UNITY_REVERSED_Z
                real depth = SampleSceneDepth(UV);
                #else
                // Adjust Z to match NDC for OpenGL ([-1, 1]) NDC=>normalized device coordinate
                real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
                #endif

                // UNITY_MATRIX_I_VP 为 VP逆矩阵
                float3 worldPosition = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);

                //Light mainLight = GetMainLight(TransformWorldToShadowCoord(worldPosition));
                //float backGroundShadow = mainLight.shadowAttenuation;//MainLightRealtimeShadow(TransformWorldToShadowCoord(worldPosition));

                float backGroundShadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(worldPosition));

                float2 depthRange = float2(-50, -1);
                float decayRange = 5; // 用于淡化焦散

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
                float scale = lerp(1, 0.2, saturate(-worldY / 500.0)); // 使得焦散现象在浅水小，深水大

                // 变换焦散贴图的两组UV的尺寸和偏移
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
                data.y = length(GetCameraPositionWS().xyz - postionWS); // 视点到水面的距离
                data.z = displacement;
                return data;
            }

            // 算出水深
            float AdjustedDepth(half2 uvs, half4 additionalData)
            {
                float rawD = SampleSceneDepth(uvs); // 获取深度图中的值
                float d = LinearEyeDepth(rawD, _ZBufferParams); // 获取View Space中的实际深度值

                return d * additionalData.x - additionalData.y;
            }

            // 计算折射颜色
            float3 Refraction(float2 distortion, float depth)
            {
                float3 output = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, distortion).rgb;
                output += ApplyCaustics(distortion);
                output *= Absorption(saturate(depth / _DepthScale));
                return output;
            }

            // 计算扭曲的UV坐标
            half2 DistortionUVs(float depth, float3 normalWS)
            {
                // 将法线变换到屏幕空间
                half3 viewNormal = mul((float3x3)GetWorldToHClipMatrix(), -normalWS).xyz;
                float2 distortedUV = viewNormal.xz * saturate((depth) * _DistortionScale);
                return distortedUV;
            }

            // 计算反射的菲尼尔项
            half CalculateFresnelTerm(half3 normalWS, half3 viewDirWS)
            {
                /*
                 * 菲尼尔现象: 法线和视线的夹角越小，反射光越强，折射光越弱
                 * 反之，折射光越强，反射光越弱
                 */
                float f = saturate(pow(1.0 - dot(normalWS, viewDirWS), 3));
                return f;
            }

            // 计算水面折射
            half4 WaterShading(half2 screenUV, float3 positionWS, float3 normal, float3 viewDir, half4 distanceData)
            {
                // Depth: 深度
                float depth = AdjustedDepth(screenUV, distanceData);

                // Distortion: 扭曲
                half2 distortionDelta = DistortionUVs(depth, normal);
                half2 distortion;
                distortion = screenUV + distortionDelta;
                float d = depth;
                depth = AdjustedDepth(distortion, distanceData);

                // 判断Opaque贴图上的像素是位于水上还是水下
                distortion = depth < 0 ? screenUV : screenUV + distortionDelta;
                depth = depth < 0 ? d : depth;

                // 折射 Refraction
                half3 refraction = Refraction(distortion, depth);

                // 反射 Reflection
                half3 reflection = SampleReflections(normal, viewDir, distortion);
                half fresnelTerm = CalculateFresnelTerm(normal, viewDir);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS)); // 获取灯光
                half shadow = lerp(0.7, 1, mainLight.shadowAttenuation); // 插值计算阴影
                half3 GI = SampleSH(normal); // 全局光照

                // BRDF函数是PBR高光的核心
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
                half3 sss = directLighting * shadow + GI; // 合并阴影和全局光照

                // 散射 Scattering
                sss *= Scattering(saturate(depth / _DepthScale));

                // 水面颜色 = ((折射颜色,反射颜色) 按照 菲尼尔项 的插值) * 阴影 + 散射
                half3 waterColor = (lerp(refraction - 0.1, reflection, fresnelTerm) + spec) * shadow + sss;

                return half4(waterColor, 1);
            }

            // 顶点着色器
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

            // 获取法线的水平偏移作为波浪
            float2 GetWaves(float2 coords, half scale, half amplitude, half speed, float2 dir)
            {
                float time = _Time.y * speed;
                float2 offsetCoords = (coords + dir * time) / scale;

                // 波浪的法线
                float3 normal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalBase, sampler_NormalBase, offsetCoords));
                return normal.xy * amplitude;
            }

            // 片元着色器
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