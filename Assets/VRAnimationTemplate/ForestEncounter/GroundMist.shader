Shader "VRAnimationTemplate/GroundMist"
{
    Properties { _Color("Mist", Color) = (.24,.34,.43,.12) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 vertex:POSITION;float3 normal:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            CBUFFER_END
            V Vert(A a) {V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=TransformObjectToHClip(a.vertex.xyz);o.world=TransformObjectToWorld(a.vertex.xyz);o.normal=TransformObjectToWorldNormal(a.normal);return o;}
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float soft=pow(saturate(dot(normalize(i.normal),GetWorldSpaceNormalizeViewDir(i.world))),2);
                float noise=.65+.35*sin(i.world.x*.3+sin(i.world.z*.4)+_Time.y*.12);
                return half4(_Color.rgb,_Color.a*soft*noise);
            }
            ENDHLSL
        }
    }
}
