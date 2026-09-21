Shader "Waterline/Harbor Water"
{
    Properties
    {
        _BaseColor ("Water tint", Color) = (0.025,0.13,0.145,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.73
        _RippleStrength ("Ripple slope", Range(0,0.3)) = 0.07
        [HideInInspector] _RippleTime ("Capture time (-1 is live)", Float) = -1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Smoothness;
            half _RippleStrength;
            float _RippleTime;
        CBUFFER_END
        struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
        struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half4 fogAndVertexLight:TEXCOORD2; };
        Varyings Vert(Attributes input)
        {
            Varyings o;
            VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
            o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(input.normalOS);
            o.fogAndVertexLight=half4(ComputeFogFactor(p.positionCS.z),VertexLighting(p.positionWS,o.normalWS));return o;
        }
        half4 Frag(Varyings input):SV_Target
        {
            float time=_RippleTime<0?_Time.y:_RippleTime;
            float2 p=input.positionWS.xz*1.65;
            // World-space wavelength stays consistent between the large dock and smaller maintenance basin.
            float a=dot(p,float2(2.8,1.1))+time*.75;
            float b=dot(p,float2(-1.5,3.6))-time*.52;
            float c=dot(p,float2(7.2,4.1))+time*.91;
            float2 slope=float2(cos(a)+.45*cos(b)+.18*cos(c),.4*cos(a)+cos(b)+.11*cos(c));
            float fade=1-smoothstep(10,32,distance(input.positionWS,GetCameraPositionWS()));
            half3 n=normalize(input.normalWS+half3(-slope.x,0,-slope.y)*_RippleStrength*fade*saturate(input.normalWS.y));
            InputData lighting=(InputData)0;
            lighting.positionWS=input.positionWS;lighting.normalWS=n;lighting.viewDirectionWS=GetWorldSpaceNormalizeViewDir(input.positionWS);
            lighting.shadowCoord=TransformWorldToShadowCoord(input.positionWS);
            lighting.fogCoord=input.fogAndVertexLight.x;lighting.vertexLighting=input.fogAndVertexLight.yzw;
            lighting.bakedGI=SampleSH(n);lighting.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);lighting.shadowMask=half4(1,1,1,1);
            SurfaceData surface=(SurfaceData)0;surface.albedo=_BaseColor.rgb;surface.metallic=0;surface.specular=half3(.04,.04,.04);
            surface.smoothness=_Smoothness;surface.normalTS=half3(0,0,1);surface.occlusion=1;surface.alpha=1;
            half4 color=UniversalFragmentPBR(lighting,surface);color.rgb=MixFog(color.rgb,lighting.fogCoord);return color;
        }
        half4 DepthFrag(Varyings input):SV_Target{return 0;}
        ENDHLSL
        Pass
        {
            Name "WaterForward"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
