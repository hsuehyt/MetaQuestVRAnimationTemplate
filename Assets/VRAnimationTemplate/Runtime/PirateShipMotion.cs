using UnityEngine;

namespace VRAnimationTemplate
{
    public sealed class PirateShipMotion : MonoBehaviour
    {
        public ParticleSystem bowSpray, hullFoam;
        public Renderer[] shipRenderers;
        [Range(0,1)] public float rockingStrength=.75f;
        Vector3 anchor;
        Quaternion heading;
        OceanStormController storm;
        MaterialPropertyBlock properties;




        public int ContactBursts { get { var impact=GetComponent<ShipWaveImpact>();return impact?impact.ImpactCount:0; } }
        void Awake()
        {
            anchor=transform.position;heading=transform.rotation;
            storm=FindFirstObjectByType<OceanStormController>();properties=new MaterialPropertyBlock();
        }
        public void Pose(float time, bool immediate)
        {
            Vector3 forward=heading*Vector3.forward,right=heading*Vector3.right;
            float Sample(Vector3 p){OceanWaveField.Sample(new Vector2(p.x,p.z),time,out float y,out _);return y;}
            float halfLength=9*transform.lossyScale.z,halfBeam=3*transform.lossyScale.x;
            float bow=Sample(anchor+forward*halfLength),stern=Sample(anchor-forward*halfLength);
            float port=Sample(anchor-right*halfBeam),starboard=Sample(anchor+right*halfBeam);
            Vector3 target=new Vector3(anchor.x,(bow+stern+port+starboard)*.25f,anchor.z);
            float pitch=-Mathf.Atan2(bow-stern,2*halfLength)*Mathf.Rad2Deg*rockingStrength;
            float roll=Mathf.Atan2(starboard-port,2*halfBeam)*Mathf.Rad2Deg*rockingStrength;
            Quaternion rotation=heading*Quaternion.Euler(pitch,0,roll);
            float blend=immediate?1:1-Mathf.Exp(-Time.deltaTime*2);
            transform.position=Vector3.Lerp(transform.position,target,blend);transform.rotation=Quaternion.Slerp(transform.rotation,rotation,blend);
        }
        void Update()
        {
            Pose(Time.time,false);
            properties.SetFloat("_StormFlash",storm?storm.FlashIntensity:0);
            foreach(var renderer in shipRenderers)if(renderer)renderer.SetPropertyBlock(properties);
        }
    }
}
