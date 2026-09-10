using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace VRAnimationTemplate.Editor
{
    public static class ForestEncounterBuilder
    {
        const string Folder = "Assets/VRAnimationTemplate/ForestEncounter";
        static Material steel, dark, bronze, bark, needles, moss, rock, glow;
        static Mesh cone;
        static System.Random random;
        static float R(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());
        static Transform Node(string name, Transform parent, Vector3 p)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = p; return t;
        }
        static GameObject Part(string name, Transform parent, Vector3 p, Vector3 scale, Material mat, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = p; go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial = mat; return go;
        }
        static Material Mat(string name, Color color, float metal = 0)
        {
            string path = Folder + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.color = color; m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", .24f); m.enableInstancing = true; EditorUtility.SetDirty(m); return m;
        }
        static void SaveMesh(Mesh mesh, string name)
        {
            mesh.name = name; string path = Folder + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); }
            else AssetDatabase.CreateAsset(mesh, path);
        }
        static Mesh ConeMesh(int sides, float length, float radius, bool beam)
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            for (int i = 0; i <= sides; i++)
            {
                float angle = i * Mathf.PI * 2 / sides;
                vertices.Add(beam ? new Vector3(Mathf.Cos(angle)*.25f, Mathf.Sin(angle)*.25f,0) : new Vector3(0,1,0)); uv.Add(new Vector2((float)i/sides,0));
                vertices.Add(beam ? new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,length) : new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))); uv.Add(new Vector2((float)i/sides,1));
                if(i < sides) { int n=i*2; tris.AddRange(new[]{n,n+3,n+1,n,n+2,n+3}); }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(tris,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        static GameObject MeshPart(string name, Transform parent, Vector3 position, Vector3 scale, Mesh mesh, Material mat)
        {
            Transform t=Node(name,parent,position); t.localScale=scale;
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh; t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat; return t.gameObject;
        }

        [MenuItem("Tools/VR Animation Template/Build Forest Encounter in Scene 2")]
        public static void Build()
        {
            random = new System.Random(2015);
            var scene=EditorSceneManager.OpenScene("Assets/VRAnimationTemplate/Scenes/Scene2.unity",OpenSceneMode.Single);
            foreach(var root in scene.GetRootGameObjects())
                if(root.name=="Floor" || root.name=="Key Light" || root.name.StartsWith("Animation Placeholder") || root.name=="Forest Encounter") Object.DestroyImmediate(root);
            steel=Mat("Weathered Steel",new Color(.19f,.24f,.25f),.65f); dark=Mat("Joint Graphite",new Color(.055f,.07f,.075f),.7f);
            var grain=new Texture2D(256,256,TextureFormat.RGB24,false);
            for(int y=0;y<256;y++) for(int x=0;x<256;x++)
            {
                float n=Mathf.PerlinNoise(x*.045f,y*.045f);float scratches=Mathf.PerlinNoise(x*.9f,y*.055f);
                grain.SetPixel(x,y,Color.Lerp(new Color(.26f,.17f,.1f),new Color(.65f,.72f,.74f),Mathf.SmoothStep(.1f,1,n))*(.7f+scratches*.5f));
            }
            grain.Apply();File.WriteAllBytes(Folder+"/Steel Grain.png",grain.EncodeToPNG());Object.DestroyImmediate(grain);AssetDatabase.ImportAsset(Folder+"/Steel Grain.png");
            steel.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Steel Grain.png"));steel.SetTextureScale("_BaseMap",new Vector2(3,3));
            bronze=Mat("Oxidized Edges",new Color(.28f,.20f,.115f),.5f); bark=Mat("Fir Bark",new Color(.12f,.095f,.075f));
            needles=Mat("Alpine Needles",new Color(.045f,.115f,.092f)); moss=Mat("Moss",new Color(.12f,.19f,.105f)); rock=Mat("Mountain Granite",new Color(.23f,.27f,.29f));
            glow=Mat("Amber Eyes",new Color(1,.66f,.2f)); glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor",new Color(1,.46f,.08f)*6);
            glow.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;
            Shader unlit=Shader.Find("Universal Render Pipeline/Unlit"); glow.shader=unlit; glow.SetColor("_BaseColor",new Color(3,1.7f,.35f));
            SaveMesh(ConeMesh(11,1,1,false),"Fir Crown"); cone=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Fir Crown.asset");
            Transform world=Node("Forest Encounter",null,Vector3.zero);
            Environment(world);
            var controller=world.gameObject.AddComponent<ForestRobotEncounter>();
            Robot(world,controller);
            Audio(world,controller);
            foreach(var director in Object.FindObjectsByType<VRChapterDirector>(FindObjectsSortMode.None))
            {
                var so=new SerializedObject(director); so.FindProperty("durationSeconds").floatValue=30; so.FindProperty("autoAdvance").boolValue=true; so.ApplyModifiedPropertiesWithoutUndo();
            }
            GameObject ui=GameObject.Find("VR UI");
            var reticle=GameObject.Find("Gaze Reticle"); if(reticle) {var image=reticle.GetComponent<Image>();image.enabled=true;image.type=Image.Type.Simple;image.fillAmount=1;}
            if(ui)
            {
                // Keep the same forward-facing navigation as the other chapters.
                ui.transform.position=new Vector3(0,1.6f,2f); ui.transform.rotation=Quaternion.identity;
                foreach(Text text in ui.GetComponentsInChildren<Text>()) if(text.text=="FOREST") text.text="SCENE 2";
            }
            Camera camera=Camera.main; camera.farClipPlane=220; camera.backgroundColor=new Color(.035f,.065f,.095f); camera.clearFlags=CameraClearFlags.SolidColor;
            controller.Sample(0);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            VerifyAndRender(controller,camera);
            Debug.Log("FOREST_ENCOUNTER_BUILD_OK");
        }

        static void Environment(Transform world)
        {
            RenderSettings.skybox=null; RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=.008f;
            RenderSettings.fogColor=new Color(.035f,.065f,.095f); RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.24f,.33f,.45f); RenderSettings.ambientEquatorColor=new Color(.09f,.15f,.19f); RenderSettings.ambientGroundColor=new Color(.045f,.06f,.07f);
            Transform light=Node("Cold Moonlight",world,Vector3.zero); light.rotation=Quaternion.Euler(38,-35,0);
            Light moon=light.gameObject.AddComponent<Light>(); moon.type=LightType.Directional; moon.color=new Color(.55f,.72f,1); moon.intensity=1.1f; moon.shadows=LightShadows.Soft; RenderSettings.sun=moon;
            Material moonMat=Mat("Moon Glow",new Color(.8f,.9f,1)); moonMat.shader=Shader.Find("Universal Render Pipeline/Unlit");moonMat.SetColor("_BaseColor",new Color(.8f,.9f,1));
            Part("Moon",world,new Vector3(-45,65,120),Vector3.one*5,moonMat,PrimitiveType.Sphere);
            var ground=Part("Forest Ground",world,new Vector3(0,-.4f,0),new Vector3(360,.8f,360),Mat("Forest Earth",new Color(.085f,.10f,.078f))); ground.AddComponent<BoxCollider>();
            var terrainVertices=new List<Vector3>();var terrainTriangles=new List<int>();var terrainUV=new List<Vector2>();
            for(int z=0;z<=100;z++) for(int x=0;x<=100;x++)
            {
                float px=(x-50)*3,pz=(z-40)*3;
                float clearing=Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,24,Mathf.Abs(px)));
                float height=clearing*Mathf.PerlinNoise(x*.12f,z*.12f)*2.6f;
                terrainVertices.Add(new Vector3(px,height-.03f,pz));terrainUV.Add(new Vector2(x*.2f,z*.2f));
                if(x<100 && z<100) {int n=z*101+x;terrainTriangles.AddRange(new[]{n,n+101,n+1,n+1,n+101,n+102});}
            }
            var terrainMesh=new Mesh();terrainMesh.SetVertices(terrainVertices);terrainMesh.SetUVs(0,terrainUV);terrainMesh.SetTriangles(terrainTriangles,0);terrainMesh.RecalculateNormals();SaveMesh(terrainMesh,"Mountain Forest Floor");
            var terrain=MeshPart("Rocky Ground Relief",world,Vector3.zero,Vector3.one,AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Mountain Forest Floor.asset"),ground.GetComponent<Renderer>().sharedMaterial);terrain.AddComponent<MeshCollider>();
            Transform forest=Node("Dense Mountain Firs",world,Vector3.zero);
            var plants=new List<Transform>();
            for(int i=0;i<410;i++)
            {
                float x=R(-105,105),z=R(-70,145);
                if(new Vector2(x,z).magnitude<8 || (Mathf.Abs(x)<10 && z>0 && z<40)) continue;
                float groundY=Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,24,Mathf.Abs(x)))*Mathf.PerlinNoise((x/3+50)*.12f,(z/3+40)*.12f)*2.6f;
                float h=R(20,23); Transform tree=Node("Fir "+i,forest,new Vector3(x,groundY,z));
                Part("Trunk",tree,new Vector3(0,h*.48f,0),new Vector3(.6f,h*.48f,.6f),bark,PrimitiveType.Cylinder);
                for(int j=0;j<5;j++)
                {
                    float baseY=h*(.24f+j*.125f); float radius=(1-j*.16f)*R(2.5f,3.9f);
                    MeshPart("Needle Boughs",tree,new Vector3(0,baseY,0),new Vector3(radius,h*.26f,radius),cone,needles);
                    for(int b=0;b<6;b++)
                    {
                        float angle=(b*60+i*17)*Mathf.Deg2Rad;
                        var branch=MeshPart("Branch Spray",tree,new Vector3(Mathf.Cos(angle)*radius*.6f,baseY+.2f,Mathf.Sin(angle)*radius*.6f),new Vector3(radius*.48f,h*.12f,radius*.48f),cone,needles);
                        branch.transform.localRotation=Quaternion.Euler(Mathf.Sin(angle)*22,0,Mathf.Cos(angle)*22);
                    }
                }
            }
            Transform undergrowth=Node("Ferns Rocks and Fallen Timber",world,Vector3.zero);
            for(int i=0;i<190;i++)
            {
                float x=R(-55,55),z=R(-25,75); if(new Vector2(x,z).magnitude<2.5f) continue;
                var stone=Part("Glacial Rock",undergrowth,new Vector3(x,-.1f,z),new Vector3(R(.3f,2.4f),R(.4f,1.4f),R(.4f,2)),i%3==0?moss:rock,PrimitiveType.Sphere); stone.transform.rotation=Quaternion.Euler(R(-20,20),R(0,360),R(-20,20));
                if(i<90)
                {
                    Transform fern=Node("Fern",undergrowth,new Vector3(x+1,0,z)); plants.Add(fern);
                    for(int j=0;j<7;j++) { var frond=Part("Frond",fern,new Vector3(0,.4f,0),new Vector3(.12f,1.3f,.28f),moss); frond.transform.localRotation=Quaternion.Euler(42,j*51,0); }
                }
            }
            for(int i=0;i<12;i++)
            {
                var log=Part("Fallen Fir",undergrowth,new Vector3(R(-40,-12),.6f,R(-5,70)),new Vector3(.7f,R(3,6),.7f),bark,PrimitiveType.Cylinder); log.transform.rotation=Quaternion.Euler(88,R(0,180),0);
            }
            for(int i=0;i<18;i++)
                MeshPart("Distant Mountain",world,new Vector3(R(-170,170),-1,R(115,175)),new Vector3(R(25,50),R(35,70),R(20,40)),cone,rock);
            // Combine static environment by material to avoid thousands of individual draw submissions.
            Combine(forest); Combine(undergrowth);
            Material mist=Mat("Ground Mist",new Color(.24f,.34f,.43f,.13f));mist.shader=Shader.Find("VRAnimationTemplate/GroundMist");mist.SetColor("_Color",new Color(.24f,.34f,.43f,.13f));
            for(int i=0;i<12;i++)
            {
                var patch=Part("Drifting Ground Mist",world,new Vector3(R(-40,40),R(.6f,2),R(10,85)),new Vector3(R(15,30),R(2,4),R(10,22)),mist,PrimitiveType.Sphere);
                patch.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;patch.GetComponent<Renderer>().receiveShadows=false;
            }
        }
        static void Combine(Transform root)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                Material mat=mf.GetComponent<Renderer>().sharedMaterial;
                if(!groups.ContainsKey(mat)) groups[mat]=new List<CombineInstance>();
                groups[mat].Add(new CombineInstance{mesh=mf.sharedMesh,transform=root.worldToLocalMatrix*mf.transform.localToWorldMatrix});
            }
            foreach(var group in groups)
            {
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32}; mesh.CombineMeshes(group.Value.ToArray());
                string name=root.name+" "+group.Key.name; SaveMesh(mesh,name);
            }
            for(int i=root.childCount-1;i>=0;i--) Object.DestroyImmediate(root.GetChild(i).gameObject);
            foreach(var group in groups) MeshPart(group.Key.name,root,Vector3.zero,Vector3.one,AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/"+root.name+" "+group.Key.name+".asset"),group.Key);
        }

        static void Robot(Transform world,ForestRobotEncounter c)
        {
            c.robot=Node("Giant Robot - 29 metres",world,new Vector3(0,0,29)); c.robot.localRotation=Quaternion.Euler(0,65,0);
            c.hips=Node("Hips",c.robot,new Vector3(0,12.2f,0));
            Part("Pelvis",c.hips,Vector3.zero,new Vector3(5,2.8f,3.6f),dark,PrimitiveType.Sphere);
            c.torso=Node("Spine",c.hips,new Vector3(0,2,0));
            Part("Abdominal Coupling",c.torso,new Vector3(0,.8f,0),new Vector3(3,2.4f,3),bronze,PrimitiveType.Cylinder);
            Part("Chest Shell",c.torso,new Vector3(0,5.4f,0),new Vector3(9,8,5.5f),steel,PrimitiveType.Sphere);
            Part("Chest Plate",c.torso,new Vector3(0,5.8f,2.4f),new Vector3(6.4f,5.2f,.6f),steel);
            for(int i=0;i<7;i++) Part("Cooling Rib",c.torso,new Vector3((i-3)*.55f,3,2.65f),new Vector3(.22f,1.6f,.18f),dark);
            for(int s=-1;s<=1;s+=2)
            {
                for(int j=0;j<4;j++) Part("Plate Rivet",c.torso,new Vector3(s*2.8f,4+j,2.78f),Vector3.one*.21f,bronze,PrimitiveType.Sphere);
                Transform hip=Node(s<0?"Left Hip":"Right Hip",c.hips,new Vector3(s*2.1f,0,0));
                Part("Hip Bearing",hip,Vector3.zero,Vector3.one*2.3f,bronze,PrimitiveType.Sphere);
                Part("Thigh Armor",hip,new Vector3(0,-2.8f,0),new Vector3(2.1f,4.8f,2.4f),steel);
                Transform knee=Node("Knee",hip,new Vector3(0,-5.3f,0)); Part("Knee Bearing",knee,Vector3.zero,Vector3.one*2.1f,dark,PrimitiveType.Sphere);
                Part("Shin Armor",knee,new Vector3(0,-2.6f,0),new Vector3(2.5f,4.4f,2.7f),steel);
                Transform ankle=Node("Ankle",knee,new Vector3(0,-5.1f,0)); Part("Foot",ankle,new Vector3(0,-.8f,.7f),new Vector3(3,1.8f,4.5f),steel);
                Transform shoulder=Node(s<0?"Left Shoulder":"Right Shoulder",c.torso,new Vector3(s*5,7.4f,0));
                Part("Shoulder Shell",shoulder,Vector3.zero,Vector3.one*3.8f,steel,PrimitiveType.Sphere);
                Part("Upper Arm",shoulder,new Vector3(0,-3,0),new Vector3(2,4.7f,2.3f),steel);
                Transform elbow=Node("Elbow",shoulder,new Vector3(0,-5.4f,0)); Part("Elbow Bearing",elbow,Vector3.zero,Vector3.one*2.3f,bronze,PrimitiveType.Sphere);
                Part("Forearm",elbow,new Vector3(0,-2.5f,0),new Vector3(2.6f,4.4f,2.7f),steel);
                Transform wrist=Node("Wrist",elbow,new Vector3(0,-4.8f,0)); Part("Palm",wrist,new Vector3(0,-.9f,0),new Vector3(2.8f,2.1f,1.5f),dark);
                for(int f=0;f<4;f++)
                {
                    Transform finger=Node("Finger "+f,wrist,new Vector3((f-1.5f)*.65f,-1.8f,0));
                    for(int k=0;k<3;k++) { Part("Phalanx",finger,new Vector3(0,-.4f,0),new Vector3(.5f,.8f,.65f),steel); finger=Node("Finger Joint",finger,new Vector3(0,-.75f,0)); finger.localRotation=Quaternion.Euler(-12,0,0); }
                }
                Transform thumb=Node("Thumb",wrist,new Vector3(-s*1.55f,-.4f,.1f)); thumb.localRotation=Quaternion.Euler(-20,0,-s*35);
                Part("Thumb Segment",thumb,new Vector3(0,-.7f,0),new Vector3(.7f,1.4f,.8f),steel);
                if(s<0) { c.leftHip=hip;c.leftKnee=knee;c.leftAnkle=ankle;c.leftShoulder=shoulder; } else {c.rightHip=hip;c.rightKnee=knee;c.rightAnkle=ankle;c.rightShoulder=shoulder;}
            }
            c.head=Node("Neck - Gaze Joint",c.torso,new Vector3(0,10.5f,.3f));
            Part("Head Shell",c.head,new Vector3(0,1.5f,0),new Vector3(4.3f,4.3f,3.6f),steel,PrimitiveType.Sphere);
            Part("Recessed Visor",c.head,new Vector3(0,1.35f,1.55f),new Vector3(3.5f,1.6f,.45f),dark);
            var beamMat=Mat("Searchlight Haze",new Color(1,.72f,.32f,.1f)); beamMat.shader=Shader.Find("VRAnimationTemplate/SearchBeam"); beamMat.SetColor("_Color",new Color(1,.72f,.32f,.12f)); beamMat.SetFloat("_Strength",1);
            SaveMesh(ConeMesh(32,38,5,true),"Searchlight Cone"); Mesh beamMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Searchlight Cone.asset");
            c.eyes=new Transform[2];c.beams=new Transform[2];c.searchlights=new Light[2];
            for(int i=0;i<2;i++)
            {
                Vector3 p=new Vector3(i==0?-.85f:.85f,1.4f,1.9f);
                c.eyes[i]=Part("Amber Eye",c.head,p,new Vector3(.62f,.62f,.18f),glow,PrimitiveType.Sphere).transform;
                c.beams[i]=MeshPart("Eye Search Beam",c.head,p,Vector3.one,beamMesh,beamMat).transform;
                c.beams[i].GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                var light=Node("Eye Spotlight",c.head,p).gameObject.AddComponent<Light>(); light.type=LightType.Spot;light.range=42;light.spotAngle=17;light.innerSpotAngle=10;light.color=new Color(1,.68f,.28f);light.intensity=6; light.shadows=LightShadows.None; c.searchlights[i]=light;
            }
            var foliage=new List<Transform>();
            for(int i=0;i<8;i++) { var plant=Node("Footfall Fern",world,new Vector3(R(-8,8),0,R(3,8))); for(int j=0;j<5;j++) { var leaf=Part("Frond",plant,new Vector3(0,.4f,0),new Vector3(.12f,1.1f,.25f),moss); leaf.transform.localRotation=Quaternion.Euler(45,j*72,0); } foliage.Add(plant); }
            c.nearbyFoliage=foliage.ToArray();
        }

        static void Audio(Transform world,ForestRobotEncounter c)
        {
            WriteAudio("Forest Wind",8,false); WriteAudio("Heavy Footfall",3,true);
            var ambience=Node("Mountain Wind",world,Vector3.zero).gameObject.AddComponent<AudioSource>(); ambience.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/Forest Wind.wav");ambience.loop=true;ambience.playOnAwake=true;ambience.volume=.18f;
            c.footfall=Node("Heavy Footfall",c.robot,Vector3.zero).gameObject.AddComponent<AudioSource>();c.footfall.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/Heavy Footfall.wav");c.footfall.playOnAwake=false;c.footfall.spatialBlend=1;c.footfall.minDistance=20;c.footfall.maxDistance=90;c.footfall.volume=.7f;
        }
        static void WriteAudio(string name,int seconds,bool impact)
        {
            int rate=22050,count=rate*seconds;string path=Folder+"/"+name+".wav";
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                float low=0;
                for(int i=0;i<count;i++) {float t=(float)i/rate;low=Mathf.Lerp(low,R(-1,1),.025f);float env=impact?Mathf.Exp(-t*2.6f)*Mathf.Min(t*80,1):Mathf.Sin(Mathf.PI*t/seconds)*.6f; float signal=impact?(.5f*Mathf.Sin(t*2*Mathf.PI*43)+low)*env:low*env;writer.Write((short)(Mathf.Clamp(signal,-1,1)*26000));}
            }
            AssetDatabase.ImportAsset(path);
        }
        static void VerifyAndRender(ForestRobotEncounter c,Camera camera)
        {
            Directory.CreateDirectory("Artifacts/ForestEncounter");
            foreach(var mb in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(mb.gameObject)>0) throw new Exception("Missing script: "+mb.name);
            var originalRotation=camera.transform.rotation;
            Vector3 planted=c.rightAnkle.position;
            float maxSlide=0;
            for(int frame=0;frame<=120;frame++)
            {
                c.Sample(frame*.25f);
                maxSlide=Mathf.Max(maxSlide,Vector3.Distance(planted,c.rightAnkle.position));
                if(float.IsNaN(c.head.rotation.x)) throw new Exception("Invalid gaze rotation");
            }
            if(maxSlide>.02f) throw new Exception("Support foot moved "+maxSlide+" metres");
            c.Sample(26.3f);if(c.beams[0].gameObject.activeSelf)throw new Exception("Blink did not close beam");
            c.Sample(28);if(!c.beams[0].gameObject.activeSelf)throw new Exception("Beam did not reopen");
            c.Sample(40);if(Vector3.Dot(c.head.forward,(camera.transform.position-c.head.position).normalized)<.99f)throw new Exception("Gaze did not find viewer");
            camera.transform.rotation=Quaternion.LookRotation(new Vector3(0,18,29)-camera.transform.position);
            foreach(float time in new[]{0f,17f,28f})
            {
                c.Sample(time); var rt=new RenderTexture(1280,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var texture=new Texture2D(1280,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,900),0,0);texture.Apply();File.WriteAllBytes("Artifacts/ForestEncounter/preview-"+time+".png",texture.EncodeToPNG());
                camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(rt);
            }
            camera.transform.rotation=originalRotation;c.Sample(0);
            File.WriteAllText("Artifacts/ForestEncounter/validation.txt","Scene 2 saved. No missing scripts. 121 timeline samples passed. Maximum support-foot drift: "+maxSlide+"m. Blink close/reopen and final viewer gaze passed. Rendered 0, 17, 28 seconds. VR camera transform restored. Device performance remains untested.");
        }
    }
}
