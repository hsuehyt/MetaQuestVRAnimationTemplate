using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRAnimationTemplate.Editor
{
    // Batch-only Play Mode smoke check, including the first lightning/thunder cycle.
    [InitializeOnLoad]
    public static class OceanStormSmokeCheck
    {
        const string Key="OceanStormSmokeCheck.Active";
        static OceanStormSmokeCheck()
        {
            if(SessionState.GetBool(Key,false)) Attach();
        }
        [MenuItem("Tools/VR Animation Template/Validate Ocean Weather")]
        public static void Run()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(OceanStormChapterBuilder.ScenePath);
            SessionState.SetBool(Key,true);SessionState.SetBool(Key+"Flash",false);
            SessionState.SetBool(Key+"Thunder",false);SessionState.SetBool(Key+"Error",false);
            SessionState.SetFloat(Key+"Start",(float)EditorApplication.timeSinceStartup);
            Attach(); EditorApplication.EnterPlaymode();
        }
        static void Attach()
        {
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
        }
        static void Log(string message,string stack,LogType type)
        {
            if(type==LogType.Exception || (type==LogType.Error && stack.Contains("VRAnimationTemplate"))) SessionState.SetBool(Key+"Error",true);
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)) return;
            if(EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+"Start",0)>100) {Finish(false,"Play Mode timeout");return;}
            if(!EditorApplication.isPlaying || Time.timeSinceLevelLoad<.2f) return;
            var storm=UnityEngine.Object.FindFirstObjectByType<OceanStormController>();
            if(!storm) {Finish(false,"Storm controller missing");return;}
            if(storm.lightning.enabled) SessionState.SetBool(Key+"Flash",true);
            if(storm.thunder.isPlaying) SessionState.SetBool(Key+"Thunder",true);
            if(Time.timeSinceLevelLoad<9) return;
            bool ok=SessionState.GetBool(Key+"Flash",false)&&SessionState.GetBool(Key+"Thunder",false)&&!SessionState.GetBool(Key+"Error",false)&&storm.rain.particleCount>0&&storm.spray.particleCount>0&&storm.ambience.isPlaying;
            OceanSurfaceUpgrade.RenderPreview();
            Finish(ok,"Lightning="+SessionState.GetBool(Key+"Flash",false)+", thunder="+SessionState.GetBool(Key+"Thunder",false)+", rain particles="+storm.rain.particleCount+", crest spray="+storm.spray.particleCount+", ambience="+storm.ambience.isPlaying);
        }
        static void Finish(bool ok,string detail)
        {
            SessionState.SetBool(Key,false);
            File.WriteAllText("Artifacts/OceanStorm/PlayModeValidation.txt",(ok?"PASS":"FAIL")+": "+detail+"\nThis desktop smoke check does not validate headset performance or stereo rendering.");
            Debug.Log("OCEAN_STORM_PLAY_"+(ok?"PASS: ":"FAIL: ")+detail);
            if(Application.isBatchMode) EditorApplication.Exit(ok?0:1);
            else EditorApplication.ExitPlaymode();
        }
    }
}
