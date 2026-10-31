Shader "VRAnimationTemplate/HullFoam"
{
 Properties { _Sheet("Water sheet",Float)=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "StormAtmosphere.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float _Sheet;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;float3 world:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
   V Vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.uv=i.uv;o.color=i.color;return o;}
   float Hash(float2 p){p=frac(p*float2(123.34,345.45));p+=dot(p,p+34.345);return frac(p.x*p.y);}
   float Noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);}
   half4 Frag(V i):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float2 q=i.world.xz*1.7+float2(_Time.y*.17,-_Time.y*.23);
    float n=Noise(q)*.55+Noise(q*2.13)*.3+Noise(q*4.37)*.15;
    float2 p=i.uv*2-1;
    float edge=1-smoothstep(.48,1,abs(p.x));
    float ragged=1-smoothstep(.58,1,i.uv.y+(Noise(q*.43)-.5)*.45);
    float density=smoothstep(.3,.65,n);
    float mask=edge*ragged*smoothstep(0,.09,i.uv.y);
    float alpha=mask*lerp(density,.18+density*.5,_Sheet)*i.color.a;
    float3 c=lerp(i.color.rgb,lerp(float3(.22,.36,.39),float3(.8,.87,.88),density),_Sheet);
    float3 dir=normalize(i.world-GetCameraPositionWS());
    return half4(c,alpha*StormTransmittance(distance(i.world,GetCameraPositionWS()),dir,_Time.y));
   }
   ENDHLSL
  }
 }
}
