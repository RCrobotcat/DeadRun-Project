Shader "Painting/OwnerIdPainter"
{
    Properties
    {
        _MainTex ("Mask Texture", 2D) = "white" {}
        _OwnerID ("Owner ID (0–255)", Range(0,255)) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        Pass
        {
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct app
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _OwnerID;

            v2f vert(app v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float m = (tex2D(_MainTex, i.uv).r + tex2D(_MainTex, i.uv).g + tex2D(_MainTex, i.uv).b) / 3.0;
                if (m < 0.01) return fixed4(0, 0, 0, 1); // 未喷涂区域 → ID = 0

                float idNorm = _OwnerID / 255.0; // 归一化玩家 ID
                return fixed4(idNorm, 0, 0, 1);
            }
            ENDCG
        }
    }
}