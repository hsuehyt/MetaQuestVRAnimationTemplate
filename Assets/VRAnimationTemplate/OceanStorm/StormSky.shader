Shader "VRAnimationTemplate/StormSky"
{
 Properties
 {
  _StormFlash("Lightning",Range(0,1))=0
  _CloudContrast("Soft cloud contrast",Range(0,.12))=.065
  _HorizonHaze("Horizon haze",Range(.15,.7))=.38
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
  Cull Off ZWrite Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "StormAtmosphere.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float _StormFlash, _CloudContrast, _HorizonHaze;
   CBUFFER_END
   struct A { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct V { float4 positionCS:SV_POSITION; float3 dir:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
   V Vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.dir=i.positionOS.xyz; return o; }
   float Hash(float3 p)
   {
    p=frac(p*.1031); p+=dot(p,p.yzx+33.33); return frac((p.x+p.y)*p.z);
   }
   float Noise(float3 p)
   {
    float3 b=floor(p),f=frac(p); f=f*f*f*(f*(f*6-15)+10);
    float bottom=lerp(lerp(Hash(b),Hash(b+float3(1,0,0)),f.x),lerp(Hash(b+float3(0,1,0)),Hash(b+float3(1,1,0)),f.x),f.y);
    float top=lerp(lerp(Hash(b+float3(0,0,1)),Hash(b+float3(1,0,1)),f.x),lerp(Hash(b+float3(0,1,1)),Hash(b+1),f.x),f.y);
    return lerp(bottom,top,f.z);
   }
   half4 Frag(V i):SV_Target
   {
    // Sample a smooth directional volume instead of projecting a detailed flat cloud sheet.
    // No horizon division, domain-warped brush strokes, sharp cloud edges or fine octaves.
    float3 d=normalize(i.dir);
    float elevation=saturate(d.y);
    float3 drift=float3(_Time.y*.002,0,_Time.y*.0007);
    float3 p=d*2.15+float3(4.7,8.2,-3.4)+drift;
    float broad=Noise(p);
    float soft=Noise(p*1.91+float3(5.1,-2.3,7.2));
    float mass=(broad*.8+soft*.2-.5)*2;
    float horizon=1-smoothstep(0,_HorizonHaze,elevation);
    float3 horizonColor=float3(.205,.225,.245);
    float3 overhead=float3(.145,.16,.178);
    float3 color=lerp(overhead,horizonColor,pow(1-elevation,2.2));
    color-=mass*_CloudContrast*(1-horizon*.92);
    // Distant rain banks are broad, faint, and progressively lost in the horizon haze.
    float rain=Noise(float3(d.x*5.5,2.7,d.z*5.5)+drift*.35);
    float rainBand=(1-smoothstep(.05,.32,elevation))*smoothstep(0,.06,elevation);
    color-=(rain-.5)*.018*rainBand;
    color=lerp(color,horizonColor,horizon*.65);
    // Diffuse illumination through overcast rather than outlining individual cloud strokes.
    float glow=.82+.18*saturate(dot(d,normalize(float3(.4,.35,.8))));
    color+=_StormFlash*float3(.29,.33,.39)*glow;
    // Exact atmospheric match on both sides of the horizon, including lightning.
    // Broad banks also obscure the lower sky without creating a hard horizontal strip.
    float lowSkyFog=1-smoothstep(.015,.28,elevation);
    color=lerp(color,StormFogColor(d,_Time.y,_StormFlash),lowSkyFog);
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}
