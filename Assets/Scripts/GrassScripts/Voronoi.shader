Shader "RCrobotcat/Voronoi"
{
    Properties
    {
        _NumClumpTypes("Clump Types", Range(1, 40)) = 40 // 草丛种类数量
        _NumClumps("Clump Count", Range(1, 100)) = 2 // 草丛个数
    }
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _NumClumpTypes;
            float _NumClumps;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float2 Hash22(float2 p)
            {
                float3 a = frac(float3(p.x, p.y, p.x) * float3(123.34, 234.34, 345.65));
                a += dot(a, a + 34.45);
                return frac(float2(a.x * a.y, a.y * a.z));
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float minDist = 100000.0;
                float id = 0.0;
                float2 clumpCentre = float2(0.0, 0.0);

                int clumpLimit = min(100, (int)_NumClumps); // 限制草丛个数的上限

                for (int j = 1; j < clumpLimit; j++)
                {
                    float2 jj = float2(float(j), float(j));
                    float2 p = Hash22(jj); // 计算出一个随机的二维值作为种子点的位置
                    float d = distance(p, i.uv); // 计算uv坐标和种子点之间的距离

                    // 每次取到最小的距离和对应的ID
                    if (d < minDist)
                    {
                        minDist = d;
                        id = fmod(float(j), _NumClumpTypes);
                        clumpCentre = p;
                    }
                }

                // r: 草丛种类id; g: 草丛中心x坐标; b: 草丛中心y坐标
                float3 col = float3(id, clumpCentre.x, clumpCentre.y);
                return float4(col.xyz, 1.0);
            }
            ENDHLSL
        }
    }
}