// Keep synchronized with OceanWaveField.Sample for crest spray emission.
void StormWave(float2 p,float2 direction,float k,float amplitude,float speed,float offset,float time,
               inout float height,inout float2 slope,inout float crest)
{
 float2 d=normalize(direction);
 float phase=dot(p,d)*k-time*speed+offset;
 float s=sin(phase), c=cos(phase);
 height+=amplitude*(s-.22*cos(2*phase));
 slope+=d*amplitude*k*(c+.44*sin(2*phase));
 crest+=amplitude*k*(s-.88*cos(2*phase));
}
void StormWaves(float2 p,float time,out float height,out float2 slope,out float crest)
{
 height=0; slope=0; crest=0;
 StormWave(p,float2(1,.32),.24,.82,1.18,0,time,height,slope,crest);
 StormWave(p,float2(.84,.57),.39,.40,1.54,1.7,time,height,slope,crest);
 StormWave(p,float2(.98,-.19),.73,.17,2.04,3.3,time,height,slope,crest);
 StormWave(p,float2(.65,.76),1.24,.065,2.65,.8,time,height,slope,crest);
 crest=max(0,crest);
}
