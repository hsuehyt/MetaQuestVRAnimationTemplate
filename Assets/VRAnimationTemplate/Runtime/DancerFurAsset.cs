using UnityEngine;

namespace VRAnimationTemplate
{
    [CreateAssetMenu(menuName = "VR Animation Template/Dancer Fur")]
    public sealed class DancerFurAsset : ScriptableObject
    {
        [System.Serializable] public struct Strand
        {
            public int a, b, c, segments;
            public Vector3 barycentric;
            public float length, diameter, curl, shade;
        }
        [HideInInspector] public Strand[] strands;
        [Range(100,16000)] public int targetStrandCount = 2000;
        public Color rootColor = new Color(0,0,1);
        public Color tipColor = new Color(1,0,1);
        public Color undercoatColor = new Color(0,0,1);
        public Color bareSkinColor = new Color(0,0,1);
        [Range(0,1)] public float taper = 0f;
        [Range(.25f,4)] public float taperPower = 1;
        [Min(.001f)] public float minimumLength = .15f;
        [Tooltip("Up to 100 cm. Stored in metres.")]
        [Min(.001f)] public float maximumLength = .35f;
        [Min(.0001f)] public float minimumDiameter = .03f;
        [Min(.0001f)] public float maximumDiameter = .05f;
        [Range(0,1)] public float lengthVariation = .4f;
        [Range(0,1)] public float curlAmount = .4f;
        [Min(.005f)] public float segmentLength = .075f;
        [Range(1,16)] public int minimumSegments = 2;
        [Range(1,16)] public int maximumSegments = 8;
        [Tooltip("Change density/length here, then use Tools > VR Animation Template > Generate Fur in Scene 3.")]
        public string regeneration = "Regenerate through the Tools menu after changing groom settings.";
        [Range(3,8)] public int sides = 4;
        public int seed = 210926;
        public string lengthRule = "Short near palms and soles, measured along the rest surface";

        public void ResetSettingsToDefaults()
        {
            targetStrandCount=2000; rootColor=new Color(0,0,1); tipColor=new Color(1,0,1);
            undercoatColor=new Color(0,0,1); bareSkinColor=new Color(0,0,1);
            taper=0f; taperPower=1; minimumLength=.15f; maximumLength=.35f;
            minimumDiameter=.03f; maximumDiameter=.05f; lengthVariation=.4f; curlAmount=.4f;
            segmentLength=.075f; minimumSegments=2; maximumSegments=8; sides=4; seed=210926;
        }

        public void ClampSettings()
        {
            targetStrandCount=Mathf.Clamp(targetStrandCount,100,16000);
            minimumLength=Mathf.Clamp(minimumLength,.001f,1f); maximumLength=Mathf.Clamp(maximumLength,minimumLength,1f);
            minimumDiameter=Mathf.Clamp(minimumDiameter,.0001f,.05f); maximumDiameter=Mathf.Clamp(maximumDiameter,minimumDiameter,.05f);
            segmentLength=Mathf.Clamp(segmentLength,.005f,1f);
            minimumSegments=Mathf.Clamp(minimumSegments,1,16); maximumSegments=Mathf.Clamp(maximumSegments,minimumSegments,16);
            sides=Mathf.Clamp(sides,3,8); taper=Mathf.Clamp01(taper); taperPower=Mathf.Clamp(taperPower,.25f,4);
            lengthVariation=Mathf.Clamp01(lengthVariation); curlAmount=Mathf.Clamp01(curlAmount);
        }
        void OnValidate() { ClampSettings(); }
    }
}

