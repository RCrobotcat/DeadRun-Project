Shader "RCrobotcat/BezierGrassBlade"
{
    Properties
    {
        [Header(Shape)]
        //_Height ("Height", Float) = 1 // 草叶的长度
        //_Tilt ("Tilt", Float) = 0.9 // 草叶倾斜程度 用来乘以草叶的长度 来获取草叶到地面的垂直高度
        //_BladeWidth ("BladeWidth", Float) = 0.1 // 草叶底部的宽度 
        _TaperAmount ("Taper Amount", Float) = 0 // 控制随着草叶高度的增加，草叶宽度的衰减程度
        _p1Offset ("p1Offset", Float) = 1
        _p2Offset ("p2Offset", Float) = 1
        _CurvedNormalAmount("Curved Normal Amount", Range(0, 5)) = 1 // 两侧法线向外延伸的程度 用于模拟草叶的厚度

        [Header(Shading)]
        _TopColor ("Top Color", Color) = (.25, .5, .5, 1)
        _BottomColor ("Bottom Color", Color) = (.25, .5, .5, 1)
        _GrassAlbedo("Grass albedo", 2D) = "white" {}
        _GrassGloss("Grass gloss", 2D) = "white" {}

        [Header(Wind Animation)]
        _WaveAmplitude("Wave Amplitude", Float) = 1
        _WaveSpeed("Wave Speed", Float) = 1
        _SinOffsetRange("Phase Variation", Range(0, 10)) = 0.3 // 相位差：让p2和p3控制点的移动不要太一致
        _PushTipForward("Push Tip Forward", Range(0, 2)) = 0 // 单独控制p3控制点的移动属性
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "Simple Grass Blade"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            Cull Off

            HLSLPROGRAM
            // Required to compile gles3.0 on some platforms
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // Vertex Shader
            #pragma vertex vert
            // Fragment Shader
            #pragma fragment frag

            // Shadow 相关宏定义
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "CubicBezier.hlsl"

            struct GrassBlade
            {
                float3 position;
                float rotAngle;
                float hash;
                float height;
                float width;
                float tilt;
                float bend;
                float3 surfaceNorm;
                float windForce;
                float sideBend;
            };

            StructuredBuffer<GrassBlade> _GrassBlades;
            StructuredBuffer<int> Triangles;
            StructuredBuffer<float4> Colors;
            StructuredBuffer<float2> Uvs;

            //float _Height;
            //float _Tilt;
            //float _BladeWidth;
            float _TaperAmount;
            float _p1Offset;
            float _p2Offset;
            float _CurvedNormalAmount;

            float4 _TopColor;
            float4 _BottomColor;

            // Wind Animation
            float _WaveAmplitude;
            float _WaveSpeed;
            float _SinOffsetRange;
            float _PushTipForward;

            TEXTURE2D(_GrassAlbedo);
            SAMPLER(sampler_GrassAlbedo);
            TEXTURE2D(_GrassGloss);
            SAMPLER(sampler_GrassGloss);

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 curvedNorm : TEXCOORD1;
                float3 originalNorm : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float t : TEXCOORD4;
            };

            float3 GetP0()
            {
                return float3(0, 0, 0);
            }

            float3 GetP3(float height, float tilt)
            {
                float p3y = tilt * height; // 草叶到地面的垂直高度
                float p3x = sqrt(height * height - p3y * p3y);
                return float3(-p3x, p3y, 0);
            }

            void GetP1P2P3(float3 p0, inout float3 p3, float bend, float hash, float windForce, out float3 p1,
                           out float3 p2)
            {
                // 计算 p0 p3 两点之间连线的 1/3 和 2/3 处的点
                p1 = lerp(p0, p3, 0.33);
                p2 = lerp(p0, p3, 0.66);

                float3 bladeDir = normalize(p3 - p0);
                float3 bezCtrlOffsetDir = normalize(cross(bladeDir, float3(0, 0, 1)));

                p1 += bezCtrlOffsetDir * bend * _p1Offset;
                p2 += bezCtrlOffsetDir * bend * _p2Offset;

                float p2WindEffect = sin((_Time.y + hash * 2 * PI) * _WaveSpeed + 0.66 * 2 * PI * _SinOffsetRange) *
                    windForce;
                p2WindEffect *= 0.66 * _WaveAmplitude;

                // 弯曲 bend 越小的草叶，p3控制点的偏移越大
                // 即越硬的草叶 顶点移动的幅度就越小
                float p3WindEffect = sin((_Time.y + hash * 2 * PI) * _WaveSpeed + 1.0 * 2 * PI * _SinOffsetRange) *
                    windForce + _PushTipForward * (1 - bend);
                p3WindEffect *= _WaveAmplitude;

                p2 += bezCtrlOffsetDir * p2WindEffect;
                p3 += bezCtrlOffsetDir * p3WindEffect;
            }

            // 利用罗德里格斯旋转公式计算绕任意轴的旋转矩阵
            float3x3 RotAxis3x3(float angle, float3 axis)
            {
                axis = normalize(axis);

                float s, c;
                sincos(angle, s, c);

                // 1 - cos(angle)
                float t = 1.0 - c;

                // 轴的分量
                float x = axis.x;
                float y = axis.y;
                float z = axis.z;

                float xy = x * y;
                float xz = x * z;
                float yz = y * z;
                float xs = x * s;
                float ys = y * s;
                float zs = z * s;

                float m00 = t * x * x + c;
                float m01 = t * xy - zs;
                float m02 = t * xz + ys;

                float m10 = t * xy + zs;
                float m11 = t * y * y + c;
                float m12 = t * yz - xs;

                float m20 = t * xz - ys;
                float m21 = t * yz + xs;
                float m22 = t * z * z + c;

                return float3x3(
                    m00, m01, m02,
                    m10, m11, m12,
                    m20, m21, m22
                );
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                GrassBlade blade = _GrassBlades[IN.instanceID];
                float bend = blade.bend;
                float height = blade.height;
                float tilt = blade.tilt;
                float hash = blade.hash;
                float windForce = blade.windForce;

                float3 p0 = GetP0();

                float3 p3 = GetP3(height, tilt);

                float3 p1 = float3(0, 0, 0);
                float3 p2 = float3(0, 0, 0);
                GetP1P2P3(p0, p3, hash, windForce, bend, p1, p2);

                // 使用ComputeShader传递的顶点索引信息、颜色信息以及GrassBlade属性
                // 不再使用MVP变换计算
                int positionIndex = Triangles[IN.vertexID];
                float4 vertColor = Colors[positionIndex];
                float2 uv = Uvs[positionIndex];

                float t = vertColor.r;
                float3 centerPos = CubicBezier(p0, p1, p2, p3, t);

                float width = blade.width * (1 - _TaperAmount * t); // 草叶底部的宽度随着草叶高度的增加而衰减
                float side = vertColor.g * 2 - 1; // 颜色的绿色通道用来决定草叶的左右两侧 (-1 左侧, 1 右侧)
                float3 position = centerPos + float3(0, 0, side * width);

                float3 tangent = CubicBezierTangent(p0, p1, p2, p3, t); // 计算草叶顶点的切线方向
                float3 normal = normalize(cross(tangent, float3(0, 0, 1))); // 计算草叶顶点的法线方向

                float3 curvedNorm = normal;
                curvedNorm.z += side * _CurvedNormalAmount;
                curvedNorm = normalize(curvedNorm);

                float angle = blade.rotAngle;
                float sideBend = blade.sideBend;

                float3x3 rotMat = RotAxis3x3(-angle, float3(0, 1, 0)); // 绕Y轴旋转的矩阵
                float3x3 sideRot = RotAxis3x3(sideBend, normalize(tangent)); // 绕草叶切线方向旋转的矩阵

                position = position - centerPos;
                normal = mul(sideRot, normal);
                curvedNorm = mul(sideRot, curvedNorm);
                position = mul(sideRot, position);

                position = position + centerPos;
                normal = mul(rotMat, normal);
                curvedNorm = mul(rotMat, curvedNorm);
                position = mul(rotMat, position);

                position += blade.position; // 草叶顶点的世界位置

                OUT.positionCS = TransformObjectToHClip(position);
                OUT.curvedNorm = curvedNorm;
                OUT.originalNorm = normal;
                OUT.positionWS = position;
                OUT.uv = uv;
                OUT.t = t;

                return OUT;
            }

            // isFrontFace 用于判断当前片元是否为正面
            half4 frag(Varyings i, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                // Calculate normal
                float3 n = isFrontFace
                               ? normalize(i.curvedNorm)
                               : -reflect(-normalize(i.curvedNorm), normalize(i.originalNorm));

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float3 v = normalize(GetCameraPositionWS() - i.positionWS);

                float3 grassAlbedo = saturate(_GrassAlbedo.Sample(sampler_GrassAlbedo, i.uv));

                float4 grassCol = lerp(_BottomColor, _TopColor, i.t); // 插值计算草叶的附加颜色

                float3 albedo = grassCol.rgb * grassAlbedo;

                float gloss = (1 - _GrassGloss.Sample(sampler_GrassGloss, i.uv).r) * 0.2; // 光滑度

                half3 GI = SampleSH(n); // 用法线获取 global illumination: 环境光照

                BRDFData brdfData;
                half alpha = 1;

                InitializeBRDFData(albedo, 0, half3(1, 1, 1), gloss, alpha, brdfData);
                float3 directBRDF = DirectBRDF(brdfData, n, mainLight.direction, v) * mainLight.color; // 直接光照 BRDF 计算

                // Final color calculation => Rendering Equation
                float3 finalColor = GI * albedo + directBRDF * mainLight.shadowAttenuation * mainLight.
                    distanceAttenuation;

                float4 col;
                col = float4(finalColor, grassCol.a); // Alpha from grassCol

                return half4(col);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}