using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRAnimationTemplate
{
    public sealed class OceanStormController : MonoBehaviour
    {
        public Material oceanMaterial, skyMaterial;
        public ParticleSystem rain, spray;
        public LineRenderer lightning;
        public AudioSource ambience, thunder;
        [Range(0, 1)] public float weatherVolume = .45f;
        public bool lightningEnabled = true;
        [Range(.1f, 1)] public float flashStrength = .45f;
        [Range(100, 2500)] public int rainRate = 1100;
        public Vector2 lightningInterval = new Vector2(9, 17);
        [Range(0, 60)] public int crestSprayChecksPerSecond = 35;
        float sprayBudget;
        Transform viewer;
        Material oceanInstance, skyInstance, previousSky;
        AudioClip windClip, thunderClip;
        Coroutine stormRoutine;
        public float FlashIntensity { get; private set; }

        void Awake()
        {
            previousSky = RenderSettings.skybox;
            oceanInstance = new Material(oceanMaterial);
            skyInstance = new Material(skyMaterial);
            GetComponentInChildren<MeshRenderer>().sharedMaterial = oceanInstance;
            RenderSettings.skybox = skyInstance;
            windClip = CreateSound(false); thunderClip = CreateSound(true);
            ambience.clip = windClip; ambience.loop = true; ambience.Play();
            thunder.clip = thunderClip;
            if (Camera.main) viewer = Camera.main.transform;
        }
        void OnEnable() { stormRoutine = StartCoroutine(Storm()); }
        void Update()
        {
            ambience.volume = weatherVolume * .5f; thunder.volume = weatherVolume;
            var emission = rain.emission; emission.rateOverTime = rainRate;
            if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
                lightningEnabled = !lightningEnabled;
            if (!lightningEnabled) SetFlash(0);
            EmitCrestSpray();
        }
        void EmitCrestSpray()
        {
            if (!spray || !viewer) return;
            sprayBudget += Time.deltaTime * crestSprayChecksPerSecond;
            int checks = Mathf.Min(12, Mathf.FloorToInt(sprayBudget));
            sprayBudget -= Mathf.Floor(sprayBudget);
            for (int i=0;i<checks;i++)
            {
                Vector2 direction=Random.insideUnitCircle.normalized;
                Vector2 p=new Vector2(viewer.position.x,viewer.position.z)+direction*Random.Range(6f,42f);
                OceanWaveField.Sample(p,Time.time,out float height,out float crest);
                if(crest < .43f) continue;
                for(int j=0;j<7;j++)
                {
                    Vector2 q=p+Random.insideUnitCircle*.35f;
                    OceanWaveField.Sample(q,Time.time,out float y,out float unused);
                    var particle=new ParticleSystem.EmitParams {
                        position=new Vector3(q.x,y+.08f,q.y),
                        velocity=new Vector3(Random.Range(1.4f,3f),Random.Range(.7f,2f),Random.Range(.4f,1.1f)),
                        startLifetime=Random.Range(.45f,1.15f),startSize=Random.Range(.035f,.13f),
                        startColor=new Color(.68f,.78f,.8f,.55f)
                    };
                    spray.Emit(particle,1);
                }
            }
        }
        IEnumerator Storm()
        {
            yield return new WaitForSeconds(3);
            while (true)
            {
                if (lightningEnabled)
                {
                    float angle = Random.Range(-1.05f, 1.05f);
                    Vector3 strike = new Vector3(Mathf.Sin(angle)*210, 0, Mathf.Cos(angle)*210);
                    lightning.positionCount = 18;
                    for (int i=0;i<18;i++)
                    {
                        float f=i/17f;
                        lightning.SetPosition(i,strike+new Vector3(i==17?0:Random.Range(-8,8),115*(1-f),Random.Range(-3,3)));
                    }
                    SetFlash(flashStrength);
                    yield return new WaitForSeconds(.12f);
                    SetFlash(0);
                    yield return new WaitForSeconds(.14f);
                    if (lightningEnabled) SetFlash(flashStrength*.65f);
                    yield return new WaitForSeconds(.18f);
                    SetFlash(0);
                    thunder.transform.position=strike;
                    yield return new WaitForSeconds(.7f);
                    thunder.Play();
                }
                yield return new WaitForSeconds(Random.Range(lightningInterval.x,lightningInterval.y));
            }
        }
        void SetFlash(float f)
        {
            FlashIntensity=f;
            if(oceanInstance) oceanInstance.SetFloat("_StormFlash",f);
            if(skyInstance) skyInstance.SetFloat("_StormFlash",f);
            if(lightning) lightning.enabled=f>0;
        }
        void OnDisable()
        {
            if(stormRoutine!=null) StopCoroutine(stormRoutine);
            SetFlash(0);
            if(ambience) ambience.Stop();
            if(thunder) thunder.Stop();
        }
        void OnDestroy()
        {
            if(RenderSettings.skybox==skyInstance) RenderSettings.skybox=previousSky;
            Destroy(oceanInstance); Destroy(skyInstance); Destroy(windClip); Destroy(thunderClip);
        }
        // Deterministic filtered noise. No external recordings or downloads.
        static AudioClip CreateSound(bool isThunder)
        {
            const int rate=22050; int count=rate*(isThunder?7:12);
            float[] samples=new float[count]; var rng=new System.Random(isThunder?113:37);
            float low=0, deep=0;
            for(int i=0;i<count;i++)
            {
                float t=(float)i/rate; float n=(float)rng.NextDouble()*2-1;
                low=Mathf.Lerp(low,n,isThunder?.045f:.08f); deep=Mathf.Lerp(deep,n,.009f);
                float envelope=isThunder?Mathf.Min(t*9,1)*Mathf.Exp(-t*.65f):(.65f+.2f*Mathf.Sin(t*Mathf.PI/3));
                float edge=Mathf.Min(1,Mathf.Min(i,count-1-i)/(rate*.1f));
                samples[i]=Mathf.Clamp((low*.8f+deep*2+(isThunder?0:n*.035f))*envelope*edge,-1,1);
            }
            var clip=AudioClip.Create(isThunder?"Procedural thunder":"Procedural wind and sea",count,1,rate,false);
            clip.SetData(samples,0); return clip;
        }
    }
}
