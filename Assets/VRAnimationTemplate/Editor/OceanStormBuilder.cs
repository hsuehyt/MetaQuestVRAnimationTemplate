using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace VRAnimationTemplate.Editor
{
    public static class OceanStormBuilder
    {
        const string Folder="Assets/VRAnimationTemplate/OceanStorm";
        public const string ScenePath=Folder+"/OceanStorm.unity";

        [MenuItem("Tools/VR Animation Template/Open Ocean Storm")]
        public static void Open()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if(File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else Build();
        }
        public static void Build()
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var world=new GameObject("Ocean Storm - Weather Controls");
            var storm=world.AddComponent<OceanStormController>();
            storm.oceanMaterial=Material("Ocean","VRAnimationTemplate/StormOcean");
            storm.skyMaterial=Material("Sky","VRAnimationTemplate/StormSky");
            var water=new GameObject("Ocean - GPU animated waves"); water.transform.SetParent(world.transform);
            var mesh=OceanMesh(224);
            string meshPath=Folder+"/OceanMesh.asset";
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(old) { EditorUtility.CopySerialized(mesh,old); UnityEngine.Object.DestroyImmediate(mesh); mesh=old; }
            else AssetDatabase.CreateAsset(mesh,meshPath);
            water.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=water.AddComponent<MeshRenderer>(); renderer.sharedMaterial=storm.oceanMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;

            var rig=new GameObject("Stable XR Origin"); rig.transform.position=new Vector3(0,2.2f,0);
            var cameraObject=new GameObject("Main Camera"); cameraObject.tag="MainCamera";
            cameraObject.transform.SetParent(rig.transform,false); cameraObject.transform.localPosition=new Vector3(0,1.6f,0);
            var camera=cameraObject.AddComponent<Camera>(); camera.nearClipPlane=.08f; camera.farClipPlane=1800; camera.fieldOfView=85;
            camera.clearFlags=CameraClearFlags.Skybox; camera.allowHDR=true; camera.allowMSAA=true;
            cameraObject.AddComponent<AudioListener>(); cameraObject.AddComponent<QuestCameraPoseDriver>();
            RenderSettings.skybox=storm.skyMaterial; RenderSettings.fog=false;
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.25f,.3f,.38f);

            var particles=Material("Rain and spray","VRAnimationTemplate/Rain");
            storm.rain=Rain(world.transform,particles,false);
            storm.spray=Rain(world.transform,particles,true);
            var bolt=new GameObject("Lightning bolt"); bolt.transform.SetParent(world.transform);
            storm.lightning=bolt.AddComponent<LineRenderer>();
            var boltMaterial=Material("Lightning","Universal Render Pipeline/Unlit"); boltMaterial.SetColor("_BaseColor",new Color(3,3.5f,4));
            storm.lightning.sharedMaterial=boltMaterial; storm.lightning.widthMultiplier=.24f;
            storm.lightning.widthCurve=AnimationCurve.Linear(0,1,1,.18f); storm.lightning.numCornerVertices=2;
            storm.lightning.useWorldSpace=true; storm.lightning.shadowCastingMode=ShadowCastingMode.Off; storm.lightning.enabled=false;
            storm.ambience=world.AddComponent<AudioSource>(); storm.ambience.playOnAwake=false; storm.ambience.spatialBlend=0;
            var sound=new GameObject("Distant thunder"); sound.transform.SetParent(world.transform);
            storm.thunder=sound.AddComponent<AudioSource>(); storm.thunder.playOnAwake=false;
            storm.thunder.spatialBlend=.8f; storm.thunder.rolloffMode=AudioRolloffMode.Linear;
            storm.thunder.minDistance=100; storm.thunder.maxDistance=650;
            OceanSurfaceUpgrade.Configure(storm);
            EditorSceneManager.SaveScene(scene,ScenePath); AssetDatabase.SaveAssets();
            ValidateAndRender(camera,storm);
            Debug.Log("OCEAN_STORM_BUILD_OK: "+ScenePath);
        }
        static Material Material(string name,string shaderName)
        {
            Shader shader=Shader.Find(shaderName); if(!shader) throw new Exception("Missing shader: "+shaderName);
            string path=Folder+"/"+name+".mat"; var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) {material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            material.shader=shader; EditorUtility.SetDirty(material); return material;
        }
        static Mesh OceanMesh(int segments)
        {
            int side=segments+1; var vertices=new Vector3[side*side]; var triangles=new int[segments*segments*6];
            // Dense near the viewer, progressively coarser at the horizon.
            for(int z=0;z<side;z++) for(int x=0;x<side;x++)
            {
                float u=(x/(float)segments)*2-1,v=(z/(float)segments)*2-1;
                vertices[z*side+x]=new Vector3(Extent(u),0,Extent(v));
            }
            int n=0;
            for(int z=0;z<segments;z++) for(int x=0;x<segments;x++)
            {int a=z*side+x;triangles[n++]=a;triangles[n++]=a+side;triangles[n++]=a+1;triangles[n++]=a+1;triangles[n++]=a+side;triangles[n++]=a+side+1;}
            var mesh=new Mesh {name="Ocean adaptive grid",vertices=vertices,triangles=triangles}; mesh.RecalculateNormals();
            mesh.bounds=new Bounds(Vector3.zero,new Vector3(2400,8,2400)); return mesh;
        }
        static float Extent(float v) => Mathf.Sign(v)*(Mathf.Abs(v)*35+Mathf.Pow(Mathf.Abs(v),4)*1165);
        static ParticleSystem Rain(Transform parent,Material material,bool spray)
        {
            var obj=new GameObject(spray?"Low sea spray":"Wind driven rain"); obj.transform.SetParent(parent);
            obj.transform.position=spray?new Vector3(0,.6f,0):new Vector3(0,12,0);
            obj.transform.rotation=Quaternion.Euler(90,0,0);
            var ps=obj.AddComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main; main.duration=8;main.loop=true;main.prewarm=true; main.startLifetime=spray?2:1.5f;
            main.startSpeed=0;main.startSize=spray?.05f:.018f;main.maxParticles=spray?250:2400;
            main.startColor=spray?new Color(.5f,.65f,.7f,.16f):new Color(.65f,.78f,.86f,.3f);
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(36,36,1);
            var emission=ps.emission;emission.rateOverTime=spray?90:1100;
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
            velocity.x=3;velocity.y=spray?.35f:-12;velocity.z=1.3f;
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(0,1)});color.color=gradient;
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
            renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.velocityScale=spray?.025f:.055f;renderer.lengthScale=2;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return ps;
        }
        static void ValidateAndRender(Camera camera,OceanStormController storm)
        {
            Directory.CreateDirectory("Artifacts/OceanStorm");
            var rt=new RenderTexture(1440,900,24); var old=RenderTexture.active;
            camera.targetTexture=rt; camera.transform.localRotation=Quaternion.Euler(7,0,0);
            try
            {
                camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
                File.WriteAllBytes("Artifacts/OceanStorm/Preview.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            }
            finally {camera.targetTexture=null;camera.transform.localRotation=Quaternion.identity;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
            foreach(var shader in new[]{storm.oceanMaterial.shader,storm.skyMaterial.shader})
                foreach(var message in ShaderUtil.GetShaderMessages(shader))
                    if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error) throw new Exception(shader.name+": "+message.message);
            if(camera.GetComponent<QuestCameraPoseDriver>()==null || storm.rain==null || storm.thunder==null) throw new Exception("Incomplete storm scene");
            File.WriteAllText("Artifacts/OceanStorm/Validation.txt","Scene generated; camera, weather and sound references verified; preview rendered; no reported ocean/sky shader compilation errors.\nHeadset frame rate and stereo appearance require Quest Link testing.\n");
        }
    }
}
