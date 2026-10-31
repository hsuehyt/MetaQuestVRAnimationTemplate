Shader "VRAnimationTemplate/PirateShip"
{
 Properties { _BaseColor("Color",Color)=(.3,.2,.1,1) _Wind("Wind deformation",Float)=0 _Flag("Pirate flag",Float)=0 _Wood("Wood grain",Float)=0 _StormFlash("Lightning",Float)=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Cull Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "StormAtmosphere.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor; float _Wind,_Flag,_Wood,_StormFlash;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
   V Vert(A i)
   {
    V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    float3 p=i.positionOS.xyz;
    float gust=.7+.3*sin(_Time.y*.7);
    p.z+=_Wind*i.color.r*(sin(p.x*2.3+p.y*.6-_Time.y*4)*.25+sin(p.x*.9-_Time.y*2)*.4)*gust;
    o.world=TransformObjectToWorld(p);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normal);o.uv=i.uv;return o;
   }
   float Segment(float2 p,float2 a,float2 b){float2 v=b-a;return length(p-a-v*saturate(dot(p-a,v)/dot(v,v)));}
   half4 Frag(V i):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float3 n=normalize(i.normal);
    float3 c=_BaseColor.rgb;
    float grain=.85+.15*sin(i.world.y*51+sin(i.world.z*3)*1.3);
    c*=lerp(1,grain,_Wood);
    if(_Flag>.5)
    {
     float2 p=i.uv;float skull=1-smoothstep(.145,.16,length((p-float2(.5,.62))*float2(1,1.1)));
     float jaw=(1-smoothstep(.065,.08,abs(p.x-.5)))*(1-smoothstep(.065,.08,abs(p.y-.48)));
     float bones=1-smoothstep(.018,.028,min(Segment(p,float2(.28,.24),float2(.72,.4)),Segment(p,float2(.28,.4),float2(.72,.24))));
     float eyes=1-smoothstep(.027,.04,min(length(p-float2(.445,.63)),length(p-float2(.555,.63))));
     c=lerp(float3(.015,.018,.02),float3(.8,.79,.7),saturate(max(max(skull,jaw),bones)-eyes));
    }
    float light=.48+.45*abs(dot(n,normalize(float3(-.4,.7,.5))));
    c*=light; c+=_StormFlash*float3(.38,.43,.5);
    float3 dir=normalize(i.world-GetCameraPositionWS());float distanceToEye=distance(i.world,GetCameraPositionWS());
    return half4(lerp(StormFogColor(dir,_Time.y,_StormFlash),c,StormTransmittance(distanceToEye,dir,_Time.y)),1);
   }
   ENDHLSL
  }
 }
}
