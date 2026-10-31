#ifndef STORM_ATMOSPHERE_INCLUDED
#define STORM_ATMOSPHERE_INCLUDED
// Shared by sky and water: the same distant rain bank, fog colour and lightning response.
float StormRainBank(float3 direction, float time)
{
 float broad=sin(direction.x*3.7+direction.z*2.3-time*.018);
 float secondary=sin(direction.z*5.1-direction.x*1.6+time*.011+1.7);
 return saturate(.5+broad*.3+secondary*.2);
}
float3 StormFogColor(float3 direction,float time,float flash)
{
 float bank=StormRainBank(direction,time);
 float3 grey=float3(.205,.225,.245)-bank*float3(.014,.015,.016);
 float glow=.82+.18*saturate(dot(direction,normalize(float3(.4,.35,.8))));
 return grey+flash*float3(.29,.33,.39)*glow;
}
float StormTransmittance(float distanceToEye,float3 direction,float time)
{
 float bank=StormRainBank(direction,time);
 // Clear foreground, progressive extinction through the rain, almost opaque by 150m.
 float opticalDepth=max(0,distanceToEye-12)/58*lerp(.85,1.35,bank);
 return exp(-pow(opticalDepth,1.65));
}
#endif
