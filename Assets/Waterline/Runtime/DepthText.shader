Shader "Waterline/Depth Text"
{
    Properties
    {
        _MainTex ("Font atlas", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END
            struct Attributes { float4 positionOS: POSITION; float2 uv: TEXCOORD0; float4 color: COLOR; };
            struct Varyings { float4 positionCS: SV_POSITION; float2 uv: TEXCOORD0; float4 color: COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv; o.color = input.color * _Color;
                return o;
            }
            half4 Frag(Varyings input): SV_Target
            {
                return half4(input.color.rgb, input.color.a * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a);
            }
            ENDHLSL
        }
    }
}
