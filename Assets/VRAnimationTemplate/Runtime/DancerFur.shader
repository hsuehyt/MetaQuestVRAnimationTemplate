Shader "VR Animation Template/Dancer Fur"
{
    Properties
    {
        _BaseColor("Fur tint", Color) = (0.28,0.13,0.055,1)
        _SkinColor("Bare skin", Color) = (0.65,0.43,0.30,1)
        _Undercoat("Undercoat mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _SkinColor;
            float _Undercoat;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; float3 rest:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 rest:TEXCOORD1; half4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes v)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS=TransformObjectToWorldNormal(v.normalOS); o.rest=v.rest; o.color=v.color; return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 cell=floor(i.rest*float3(750,130,750));
                float noise=frac(sin(dot(cell,float3(12.9898,78.233,36.719)))*43758.5453);
                half3 fur=_BaseColor.rgb*lerp(.87,1.05,noise);
                half3 color= _Undercoat>.5 ? lerp(_SkinColor.rgb,fur,i.color.a) : i.color.rgb;
                Light main=GetMainLight();
                half light=saturate(dot(normalize(i.normalWS),main.direction))*.8+.25;
                return half4(color*(main.color*light+half3(.12,.12,.12)),1);
            }
            ENDHLSL
        }
    }
}
