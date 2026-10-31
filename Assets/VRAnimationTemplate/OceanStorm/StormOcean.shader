Shader "VRAnimationTemplate/StormOcean"
{
 Properties
 {
  _StormFlash("Lightning", Range(0,1)) = 0
  _DetailTex("Seamless wave detail", 2D) = "gray" {}
  _FoamAmount("Whitecap coverage", Range(0,1)) = .58
  _RippleStrength("Small wave strength", Range(0,.5)) = .18
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "StormWaves.hlsl"
   #include "StormAtmosphere.hlsl"
   TEXTURE2D(_DetailTex); SAMPLER(sampler_DetailTex);
   CBUFFER_START(UnityPerMaterial)
   float _StormFlash, _FoamAmount, _RippleStrength;
   CBUFFER_END
   struct A { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
   V Vert(A i)
   {
    V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    float3 p=TransformObjectToWorld(i.positionOS.xyz);
    float height, crest; float2 slope;
    StormWaves(p.xz,_Time.y,height,slope,crest);
    p.y+=height; o.world=p; o.positionCS=TransformWorldToHClip(p); return o;
   }
   half4 Frag(V i):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float2 p=i.world.xz; float t=_Time.y;
    float dist=distance(GetCameraPositionWS(),i.world);
    float height, crest; float2 slope;
    StormWaves(p,t,height,slope,crest);
    // Non-aligned advected detail with mip filtering, instead of crossed sine ripples.
    float2 uv=p*.043-float2(t*.024,t*.009);
    float4 a=SAMPLE_TEXTURE2D(_DetailTex,sampler_DetailTex,uv);
    float2 uv2=float2(p.x*.81-p.y*.59,p.x*.59+p.y*.81)*.117-float2(t*.031,t*.014);
    float4 b=SAMPLE_TEXTURE2D(_DetailTex,sampler_DetailTex,uv2+a.rg*.09);
    float detail=1-smoothstep(20,85,dist);
    float2 small=(a.rg*2-1)+(b.rg*2-1)*.45;
    float3 n=normalize(float3(-slope.x+small.x*_RippleStrength*detail,1,-slope.y+small.y*_RippleStrength*detail));
    float3 v=normalize(GetWorldSpaceViewDir(i.world));
    float fres=.025+.975*pow(1-saturate(dot(n,v)),5);
    float3 reflection=reflect(-v,n);
    float skyBlend=smoothstep(0,.7,reflection.y);
    float reflectedCloud=SAMPLE_TEXTURE2D(_DetailTex,sampler_DetailTex,reflection.xz*.35+float2(t*.001,0)).b;
    float3 sky=lerp(float3(.20,.26,.31),float3(.055,.078,.105),skyBlend)*(.75+reflectedCloud*.45);
    float3 deep=float3(.008,.035,.043);
    float backLight=pow(saturate(dot(v,normalize(float3(.45,-.2,-.7)))),3);
    float3 water=deep+float3(.012,.065,.063)*saturate(height*.65+.25)*(.4+backLight);
    float3 color=lerp(water,sky,fres);
    // Curvature-based caps with broken trailing foam, rather than flat height masks.
    float threshold=lerp(.76,.26,_FoamAmount);
    float cap=smoothstep(threshold,threshold+.23,crest);
    float wake=smoothstep(threshold-.17,threshold+.07,crest)*.32;
    float breakup=a.b*.65+b.b*.35;
    float patch=smoothstep(.29,.66,breakup+cap*.21);
    float cells=SAMPLE_TEXTURE2D(_DetailTex,sampler_DetailTex,uv2*2.8+a.rg*.2).b;
    float foam=saturate((cap+wake)*patch*(.48+.52*cells));
    foam*=1-smoothstep(35,115,dist);
    float foamLight=.68+.22*saturate(n.y)+_StormFlash*.3;
    color=lerp(color,float3(.66,.75,.76)*foamLight,foam*.92);
    float3 l=normalize(float3(-.4,.65,.6));
    float spec=pow(saturate(dot(n,normalize(l+v))),lerp(65,20,1-detail));
    color+=spec*float3(.10,.14,.18)*(1-foam)*(.45+_StormFlash*3)*(1-smoothstep(25,100,dist));
    color+=_StormFlash*(.06+fres*.28)*float3(.65,.8,1);
    float3 direction=-v;
    float visibility=StormTransmittance(dist,direction,t);
    color=lerp(StormFogColor(direction,t,_StormFlash),color,visibility);
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}
