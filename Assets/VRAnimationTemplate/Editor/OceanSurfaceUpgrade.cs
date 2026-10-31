using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace VRAnimationTemplate.Editor
{
    public static class OceanSurfaceUpgrade
    {
        const string Folder="Assets/VRAnimationTemplate/OceanStorm";
        [MenuItem("Tools/VR Animation Template/Upgrade Ocean Surface")]
        public static void Apply()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateDetailTexture();
            foreach(string path in new[]{OceanStormBuilder.ScenePath,OceanStormChapterBuilder.ScenePath})
            {
                if(!File.Exists(path)) continue;
                var scene=EditorSceneManager.OpenScene(path);
                Configure(UnityEngine.Object.FindFirstObjectByType<OceanStormController>());
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            RenderPreview();
            Debug.Log("OCEAN_SURFACE_UPGRADE_OK");
        }
        public static void Configure(OceanStormController storm)
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/WaveDetail.asset");
            if(!texture) {CreateDetailTexture();texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/WaveDetail.asset");}
            storm.oceanMaterial.SetTexture("_DetailTex",texture);
            storm.oceanMaterial.SetFloat("_FoamAmount",.68f);
            storm.oceanMaterial.SetFloat("_RippleStrength",.28f);
            EditorUtility.SetDirty(storm.oceanMaterial);
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/CrestSpray.mat");
            if(!material) {material=new Material(Shader.Find("VRAnimationTemplate/CrestSpray"));AssetDatabase.CreateAsset(material,Folder+"/CrestSpray.mat");}
            var ps=storm.spray;
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main; main.maxParticles=400; main.startSpeed=0; main.gravityModifier=.18f;
            main.prewarm=false; main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission; emission.enabled=false;
            var shape=ps.shape;shape.enabled=false;
            var velocity=ps.velocityOverLifetime;velocity.enabled=false;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.6f,1,1.8f));
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
            renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;
            storm.crestSprayChecksPerSecond=35;
            EditorUtility.SetDirty(ps);EditorUtility.SetDirty(renderer);EditorUtility.SetDirty(storm);
        }
        static float Hash(int x,int y,int period)
        {
            x=(x%period+period)%period;y=(y%period+period)%period;
            uint n=unchecked((uint)(x*374761393+y*668265263+19237));
            n=(n^(n>>13))*1274126177u; n^=n>>16;
            return (n&0xffffff)/16777215f;
        }
        static float Noise(float u,float v,int period)
        {
            float x=u*period,y=v*period;int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y);
            float a=x-ix,b=y-iy;a=a*a*a*(a*(a*6-15)+10);b=b*b*b*(b*(b*6-15)+10);
            return Mathf.Lerp(Mathf.Lerp(Hash(ix,iy,period),Hash(ix+1,iy,period),a),Mathf.Lerp(Hash(ix,iy+1,period),Hash(ix+1,iy+1,period),a),b);
        }
        static float Field(float u,float v)
        {
            float du=(Noise(u,v,4)-.5f)*.19f,dv=(Noise(u+.43f,v+.19f,4)-.5f)*.19f;
            u+=du;v+=dv;
            return Noise(u,v,8)*.46f+Noise(u+.23f,v,16)*.28f+Noise(u,v+.38f,32)*.16f+Noise(u,v,64)*.10f;
        }
        static void CreateDetailTexture()
        {
            const int n=512;var height=new float[n*n];var colors=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)height[y*n+x]=Field(x/(float)n,y/(float)n);
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float dx=height[y*n+(x+1)%n]-height[y*n+(x+n-1)%n];
                float dy=height[((y+1)%n)*n+x]-height[((y+n-1)%n)*n+x];
                float foam=Mathf.Clamp01((height[y*n+x]-.25f)*2);
                float cells=1-Mathf.Abs(Noise(x/(float)n,y/(float)n,64)*2-1);
                colors[y*n+x]=new Color(Mathf.Clamp01(.5f+dx*15),Mathf.Clamp01(.5f+dy*15),foam,cells);
            }
            var texture=new Texture2D(n,n,TextureFormat.RGBA32,true,true){name="Seamless ocean detail",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=8};
            texture.SetPixels(colors);texture.Apply(true,false);
            string path=Folder+"/WaveDetail.asset";var old=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(old){EditorUtility.CopySerialized(texture,old);UnityEngine.Object.DestroyImmediate(texture);EditorUtility.SetDirty(old);}
            else AssetDatabase.CreateAsset(texture,path);
        }
        public static void RenderPreview()
        {
            var camera=Camera.main;var rt=new RenderTexture(1440,900,24);var old=RenderTexture.active;
            Directory.CreateDirectory("Artifacts/OceanStorm");
            try
            {
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
                File.WriteAllBytes("Artifacts/OceanStorm/ImprovedOcean.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            }
            finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
            foreach(var name in new[]{"VRAnimationTemplate/StormOcean","VRAnimationTemplate/CrestSpray"})
                foreach(var message in ShaderUtil.GetShaderMessages(Shader.Find(name)))
                    if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)throw new Exception(name+": "+message.message);
        }
    }
}
