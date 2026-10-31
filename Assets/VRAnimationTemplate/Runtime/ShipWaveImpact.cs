using UnityEngine;

namespace VRAnimationTemplate
{
    // A bounded pool of broad, wave-conforming impact patches, never a row of emitters.
    public sealed class ShipWaveImpact : MonoBehaviour
    {
        public Material foamMaterial,sheetMaterial;
        public ParticleSystem droplets;
        public int ImpactCount { get; private set; }
        public int ActiveFoamCount { get; private set; }
        static readonly float[] Stations={-8.7f,-6.1f,-3.4f,-.8f,2.3f,5.1f,7.9f,9.8f,11.4f};
        readonly float[] previous=new float[18],cooldown=new float[18];
        readonly Patch[] patches=new Patch[6];
        GameObject effectRoot;
        float sampleTimer,meshTimer,nextImpact;
        bool sampled;
        int cursor;
        class Patch
        {
            public Mesh foam,sheet;public MeshRenderer foamRenderer,sheetRenderer;
            public Vector3[] fv,sv;public Color[] fc,sc;
            public Vector3 origin,outward,tangent;public float born=-100,width,spread,height,seed;
        }
        void Awake()
        {
            effectRoot=new GameObject("Ship wave impacts (runtime)");
            for(int i=0;i<patches.Length;i++)
            {
                var p=new Patch();patches[i]=p;
                Grid("Foam patch "+i,24,foamMaterial,out p.foam,out p.foamRenderer,out p.fv,out p.fc);
                Grid("Impact sheet "+i,16,sheetMaterial,out p.sheet,out p.sheetRenderer,out p.sv,out p.sc);
            }
        }
        void Grid(string label,int nx,Material mat,out Mesh mesh,out MeshRenderer renderer,out Vector3[] vertices,out Color[] colors)
        {
            var obj=new GameObject(label);obj.transform.SetParent(effectRoot.transform,false);
            mesh=new Mesh{name=label};mesh.MarkDynamic();vertices=new Vector3[(nx+1)*7];colors=new Color[vertices.Length];
            var uv=new Vector2[vertices.Length];var indices=new int[nx*6*6];int n=0;
            for(int y=0;y<=6;y++)for(int x=0;x<=nx;x++)
            {
                int k=y*(nx+1)+x;uv[k]=new Vector2(x/(float)nx,y/6f);
                if(x<nx&&y<6){indices[n++]=k;indices[n++]=k+nx+1;indices[n++]=k+1;indices[n++]=k+1;indices[n++]=k+nx+1;indices[n++]=k+nx+2;}
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=indices;
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.enabled=false;
        }
        void LateUpdate()
        {
            sampleTimer+=Time.deltaTime;meshTimer+=Time.deltaTime;
            if(sampleTimer>=.1f){SampleImpacts(sampleTimer);sampleTimer=0;}
            if(meshTimer>=1f/30){UpdatePatches();meshTimer=0;}
        }
        Vector3 Contact(float z,int side,out float localWater,out float crest)
        {
            float deck=2.7f+Mathf.Pow(Mathf.Abs(z)/12,3)*.75f;
            float width=Mathf.Lerp(.25f,3.3f,Mathf.Pow(Mathf.Clamp01(1-Mathf.Pow(Mathf.Abs(z)/12,3)),.55f));
            Vector3 p=transform.TransformPoint(new Vector3(side*width,0,z));localWater=crest=0;
            for(int j=0;j<3;j++)
            {
                OceanWaveField.Sample(new Vector2(p.x,p.z),Time.time,out float water,out crest);
                localWater=transform.InverseTransformPoint(new Vector3(p.x,water,p.z)).y;
                float depth=Mathf.Clamp01((deck-localWater)/(deck+1.8f));
                p=transform.TransformPoint(new Vector3(side*(width*Mathf.Sqrt(1-depth*depth)+.22f),localWater,z));p.y=water+.08f;
            }
            return p;
        }
        void SampleImpacts(float dt)
        {
            float best=.38f;int winner=-1;
            for(int i=0;i<18;i++)
            {
                Contact(Stations[i/2],i%2==0?-1:1,out float water,out float crest);
                float rise=sampled?(water-previous[i])*transform.lossyScale.y/dt:0;
                previous[i]=water;cooldown[i]-=dt;
                float score=rise+crest*.65f+Random.Range(-.18f,.18f);
                if(water> -1.5f&&water<3&&cooldown[i]<=0&&rise>.08f&&score>best){best=score;winner=i;}
            }
            sampled=true;if(winner<0||Time.time<nextImpact)return;
            int side=winner%2==0?-1:1;
            float z=Mathf.Clamp(Stations[winner/2]+Random.Range(-1.2f,1.2f),-9.5f,11.7f);
            Vector3 hit=Contact(z,side,out _,out _);
            cooldown[winner]=Random.Range(1.8f,3.8f);nextImpact=Time.time+Random.Range(.55f,1.15f);
            var p=patches[cursor++%patches.Length];p.born=Time.time;p.origin=hit;
            p.outward=Vector3.ProjectOnPlane(transform.right*side,Vector3.up).normalized;p.tangent=Vector3.Cross(Vector3.up,p.outward);
            p.seed=Random.Range(0,100);p.width=Random.Range(5,9);p.spread=Random.Range(2,4);p.height=Mathf.Clamp(1.8f+best*1.5f,2,5.5f);ImpactCount++;
            if(droplets)for(int j=0;j<42;j++)
                droplets.Emit(new ParticleSystem.EmitParams{position=hit+p.tangent*Random.Range(-.3f,.3f)*p.width+p.outward*Random.Range(.2f,.8f),velocity=p.outward*Random.Range(2,5)+Vector3.up*Random.Range(2,p.height*1.8f)+p.tangent*Random.Range(-1.2f,1.2f),startLifetime=Random.Range(.8f,1.6f),startSize=Random.Range(.07f,.2f),startColor=new Color(.75f,.84f,.87f,.7f)},1);
        }
        void UpdatePatches()
        {
            ActiveFoamCount=0;
            foreach(var p in patches)
            {
                float age=Time.time-p.born;bool active=age>=0&&age<5.5f;
                p.foamRenderer.enabled=active;p.sheetRenderer.enabled=active&&age<1.65f;if(!active)continue;ActiveFoamCount++;
                float fade=Mathf.SmoothStep(0,1,age/.3f)*(1-Mathf.SmoothStep(0,1,(age-1.1f)/4.4f));
                for(int y=0;y<=6;y++)for(int x=0;x<=24;x++)
                {
                    float u=x/24f,v=y/6f;float edge=1+.22f*Mathf.Sin(u*17+p.seed)+.14f*Mathf.Sin(u*31-p.seed);
                    Vector3 q=p.origin+p.tangent*((u-.5f)*p.width*(1+age*.13f))+p.outward*(.15f+age*.35f+v*(p.spread+age*.35f)*edge);
                    q+=p.tangent*Mathf.Sin(v*6+age+p.seed)*v*.45f;
                    OceanWaveField.Sample(new Vector2(q.x,q.z),Time.time,out float h,out _);q.y=h+.06f;
                    int k=y*25+x;p.fv[k]=q;p.fc[k]=new Color(.72f,.8f,.82f,fade);
                }
                p.foam.vertices=p.fv;p.foam.colors=p.fc;p.foam.RecalculateBounds();if(age>=1.65f)continue;
                float a=age/1.65f;
                for(int y=0;y<=6;y++)for(int x=0;x<=16;x++)
                {
                    float u=x/16f,v=y/6f;float crest=.75f+.25f*Mathf.Sin(u*9+p.seed)+.13f*Mathf.Sin(u*23-p.seed);
                    Vector3 q=p.origin+p.tangent*((u-.5f)*p.width*(.65f+a*.4f))+p.outward*(.15f+v*(.6f+a*4));
                    OceanWaveField.Sample(new Vector2(q.x,q.z),Time.time,out float h,out _);q.y=h+.08f+Mathf.Sin(a*Mathf.PI)*p.height*v*crest;
                    int k=y*17+x;p.sv[k]=q;p.sc[k]=new Color(.57f,.7f,.75f,Mathf.Sin(a*Mathf.PI)*(1-v*.3f));
                }
                p.sheet.vertices=p.sv;p.sheet.colors=p.sc;p.sheet.RecalculateBounds();
            }
        }
        public bool HasVisiblePeak(Camera camera)
        {
            foreach(var p in patches)
            {
                float age=Time.time-p.born;
                if(age>.45f&&age<.8f&&Vector3.Dot(p.outward,(camera.transform.position-p.origin).normalized)>.2f)return true;
            }
            return false;
        }
        void OnDisable(){if(effectRoot)effectRoot.SetActive(false);}
        void OnEnable(){if(effectRoot)effectRoot.SetActive(true);}
        void OnDestroy(){foreach(var p in patches)if(p!=null){if(p.foam)Destroy(p.foam);if(p.sheet)Destroy(p.sheet);}if(effectRoot)Destroy(effectRoot);}
    }
}
