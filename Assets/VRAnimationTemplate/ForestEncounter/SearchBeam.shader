Shader "VRAnimationTemplate/SearchBeam"
{
    Properties { _Color("Color", Color) = (1,.72,.32,.13) _Strength("Strength", Float) = 1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 vertex:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            CBUFFER_START(UnityPerMaterial)
            half4 _Color; float _Strength;
            CBUFFER_END
            V Vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.pos=TransformObjectToHClip(a.vertex.xyz); o.uv=a.uv; return o; }
            half4 Frag(V i):SV_Target { float fade=pow(saturate(1-i.uv.y),2)*smoothstep(0,.06,i.uv.y); return half4(_Color.rgb,_Color.a*fade*_Strength); }
            ENDHLSL
        }
    }
}
