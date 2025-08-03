Shader "Painting/OwnerIdPainter"
{
    Properties
    {
        _MainTex ("Mask Texture", 2D) = "white" {}
        _OldOwner ("Old Owner Texture", 2D) = "white" {}
        _OwnerID ("Owner ID (0–255)", Range(0,255)) = 0
    }
    SubShader
    {
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
            sampler2D _OldOwner;
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
                float3 maskCol = tex2D(_MainTex, i.uv).rgb;
                float m = max(max(maskCol.r, maskCol.g), maskCol.b);

                float oldOwnerValue = tex2D(_OldOwner, i.uv).r;

                if (m < 0.1)
                {
                    if (oldOwnerValue > 0.001)
                    {
                        return fixed4(oldOwnerValue, 0, 0, 1);
                    }

                    return fixed4(0, 0, 0, 0);
                }

                float idNorm = _OwnerID / 255.0; // normalize owner ID to [0, 1]
                return fixed4(idNorm, 0, 0, 1);
            }
            ENDCG
        }
    }
}