using UnityEngine;

namespace VRAnimationTemplate
{
    public static class OceanWaveField
    {
        // Same phase, harmonics and coefficients as StormWaves.hlsl.
        public static void Sample(Vector2 p, float time, out float height, out float crest)
        {
            height=0; crest=0;
            Wave(p,new Vector2(1,.32f),.24f,.82f,1.18f,0,time,ref height,ref crest);
            Wave(p,new Vector2(.84f,.57f),.39f,.40f,1.54f,1.7f,time,ref height,ref crest);
            Wave(p,new Vector2(.98f,-.19f),.73f,.17f,2.04f,3.3f,time,ref height,ref crest);
            Wave(p,new Vector2(.65f,.76f),1.24f,.065f,2.65f,.8f,time,ref height,ref crest);
            crest=Mathf.Max(0,crest);
        }
        static void Wave(Vector2 p,Vector2 direction,float k,float amplitude,float speed,float offset,float time,ref float height,ref float crest)
        {
            float phase=Vector2.Dot(p,direction.normalized)*k-time*speed+offset;
            float s=Mathf.Sin(phase), harmonic=Mathf.Cos(2*phase);
            height+=amplitude*(s-.22f*harmonic);
            crest+=amplitude*k*(s-.88f*harmonic);
        }
    }
}
