Shader "RCrobotcat/Particles/Unlit"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Strength("Strength", Float) = 0.3
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "RenderToRT"
            Tags
            {
                "LightMode" = "Custom"
            }

            ZWrite Off
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 position : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 position : SV_POSITION;
                float4 color : TEXCOORD1;
            };

            sampler2D _MainTex;
            float _Strength;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.position = TransformObjectToHClip(IN.position.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                float4 color = tex2D(_MainTex, IN.uv) * _Strength * IN.color.a;
                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}