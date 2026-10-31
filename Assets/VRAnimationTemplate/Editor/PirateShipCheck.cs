using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRAnimationTemplate.Editor
{
    [InitializeOnLoad]
    public static class PirateShipCheck
    {
        const string Key="PirateShipCheck";
        static float low=float.PositiveInfinity,high=float.NegativeInfinity;
        static bool spraySeen,runtimeError,rainSeen,crestSeen,flashSeen,thunderSeen,foamSeen,peakCaptured;
        static float nextFrameCheck,minX=1,minY=1,maxX,maxY;
        static PirateShipCheck()
        {
            EditorApplication.update+=Tick;
            Application.logMessageReceived+=(message,stack,type)=>{if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception))runtimeError=true;};
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene(OceanStormChapterBuilder.ScenePath);
            AuditReferences();
            low=float.PositiveInfinity;high=float.NegativeInfinity;
            spraySeen=runtimeError=rainSeen=crestSeen=flashSeen=thunderSeen=foamSeen=peakCaptured=false;
            nextFrameCheck=0;minX=minY=1;maxX=maxY=0;
            SessionState.SetBool(Key,true);SessionState.SetFloat(Key+"Start",(float)EditorApplication.timeSinceStartup);
            EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false))return;
            if(EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+"Start",0)>75){Finish(false,"Timeout");return;}
            if(!EditorApplication.isPlaying||Time.timeSinceLevelLoad<.1f)return;
            var ship=UnityEngine.Object.FindFirstObjectByType<PirateShipMotion>();
            if(!ship){Finish(false,"Ship component missing");return;}
            low=Mathf.Min(low,ship.transform.position.y);high=Mathf.Max(high,ship.transform.position.y);
            spraySeen|=ship.bowSpray.particleCount>0;
            var impact=ship.GetComponent<ShipWaveImpact>();
            foamSeen|=impact&&impact.ActiveFoamCount>0;
            if(!peakCaptured&&Time.timeSinceLevelLoad>4&&impact&&impact.HasVisiblePeak(Camera.main))
            {
                peakCaptured=true;PirateShipContactFix.Capture();
                File.Copy("Artifacts/OceanStorm/ContactBow.png","Artifacts/OceanStorm/ImpactPeakBow.png",true);
                File.Copy("Artifacts/OceanStorm/ContactMain.png","Artifacts/OceanStorm/ImpactPeakMain.png",true);
            }
            var weather=UnityEngine.Object.FindFirstObjectByType<OceanStormController>();
            rainSeen|=weather.rain.particleCount>0;crestSeen|=weather.spray.particleCount>0;
            flashSeen|=weather.FlashIntensity>0;thunderSeen|=weather.thunder.isPlaying;
            if(Time.timeSinceLevelLoad>=nextFrameCheck)
            {
                nextFrameCheck=Time.timeSinceLevelLoad+.5f;
                var camera=Camera.main;
                foreach(var filter in ship.GetComponentsInChildren<MeshFilter>())
                foreach(var vertex in filter.sharedMesh.vertices)
                {
                    var point=camera.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                    float h=point.z*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f);
                    // Square viewport is stricter horizontally than 16:10 and 16:9.
                    // Reserve 0.8 world units for shader wind displacement.
                    minX=Mathf.Min(minX,.5f+(point.x-.8f)/(2*h));
                    maxX=Mathf.Max(maxX,.5f+(point.x+.8f)/(2*h));
                    minY=Mathf.Min(minY,.5f+(point.y-.8f)/(2*h));
                    maxY=Mathf.Max(maxY,.5f+(point.y+.8f)/(2*h));
                }
            }
            if(Time.timeSinceLevelLoad<12)return;
            PirateShipContactFix.Capture();
            PirateShipBuilder.Preview();
            bool framed=minX>.02f&&minY>.02f&&maxX<.98f&&maxY<.98f;
            Finish(high-low>.05f&&spraySeen&&foamSeen&&rainSeen&&crestSeen&&flashSeen&&thunderSeen&&framed&&!runtimeError,
                "Vertical motion="+(high-low).ToString("F2")+"m; bow splash="+spraySeen+"; rain="+rainSeen+"; crest spray="+crestSeen+"; lightning="+flashSeen+"; thunder="+thunderSeen+"; runtime errors="+runtimeError+
                "; hull foam="+foamSeen+"; contact bursts="+ship.ContactBursts+"; square viewport bounds="+new Vector4(minX,minY,maxX,maxY).ToString("F3")+"; fully framed="+framed+" (also fits 16:10 and 16:9). Wind margin included. Headset not tested.");
        }
        public static void AuditReferences()
        {
            int missing=0;
            var details=new System.Text.StringBuilder();
            foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                foreach(var component in t.GetComponents<Component>())
                {
                    if(!component)continue;
                    var serialized=new SerializedObject(component);var property=serialized.GetIterator();
                    while(property.Next(true))
                        if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue==null&&property.objectReferenceInstanceIDValue!=0){missing++;details.AppendLine(t.name+" / "+component.GetType().Name+" / "+property.propertyPath);}
                }
            }
            var ship=UnityEngine.Object.FindFirstObjectByType<PirateShipMotion>();
            var weather=UnityEngine.Object.FindFirstObjectByType<OceanStormController>();
            if(!ship||!ship.bowSpray||ship.shipRenderers==null||!weather||!weather.rain||!weather.spray||!weather.lightning||!weather.ambience||!weather.thunder||!weather.oceanMaterial||!weather.skyMaterial||!Camera.main)missing++;
            if(ship)foreach(var renderer in ship.shipRenderers)if(!renderer||!renderer.sharedMaterial)missing++;
            Directory.CreateDirectory("Artifacts/OceanStorm");
            File.WriteAllText("Artifacts/OceanStorm/Scene4ReferenceAudit.txt","Missing scripts / broken references / missing required effect references: "+missing+"\n"+details);
            if(missing>0)throw new System.Exception("Scene4 reference audit failed: "+missing);
        }
        static void Finish(bool ok,string detail)
        {
            SessionState.SetBool(Key,false);Directory.CreateDirectory("Artifacts/OceanStorm");
            File.WriteAllText("Artifacts/OceanStorm/PirateShipPlayValidation.txt",(ok?"PASS: ":"FAIL: ")+detail);
            if(Application.isBatchMode)EditorApplication.Exit(ok?0:1);else EditorApplication.ExitPlaymode();
        }
    }
}
