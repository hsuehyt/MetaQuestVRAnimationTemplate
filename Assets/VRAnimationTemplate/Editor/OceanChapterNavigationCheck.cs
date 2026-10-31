using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VRAnimationTemplate.Editor
{
    [InitializeOnLoad]
    public static class OceanChapterNavigationCheck
    {
        const string Key = "OceanChapterNavigationCheck";
        static OceanChapterNavigationCheck() { EditorApplication.update += Tick; }
        [MenuItem("Tools/VR Animation Template/Validate Scene 4 Navigation")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/VRAnimationTemplate/Scenes/Scene3.unity");
            SessionState.SetInt(Key, 1);
            SessionState.SetFloat(Key + "start", (float)EditorApplication.timeSinceStartup);
            EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            int step = SessionState.GetInt(Key, 0);
            if (step == 0) return;
            if (step == 6 && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetInt(Key, 0);
                EditorSceneManager.OpenScene(OceanStormChapterBuilder.ScenePath); return;
            }
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + "start", 0) > 100)
            { Finish(false, "Timed out at stage " + step); return; }
            if (!EditorApplication.isPlaying || Time.timeSinceLevelLoad < 1.5f || step == 6) return;
            string scene = SceneManager.GetActiveScene().name;
            try
            {
                if (step == 1 && scene == "Scene3") { SessionState.SetInt(Key, 2); Button("SKIP / NEXT Button").onClick.Invoke(); }
                else if (step == 2 && scene == "Scene4")
                {
                    if (!UnityEngine.Object.FindFirstObjectByType<OceanStormController>()) throw new Exception("Ocean missing");
                    var director = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<VRChapterDirector>());
                    var fill = (Image)director.FindProperty("progressFill").objectReferenceValue;
                    if (fill.fillAmount <= 0) throw new Exception("Progress bar not advancing");
                    SessionState.SetInt(Key, 3); Button("SKIP / NEXT Button").onClick.Invoke();
                }
                else if (step == 3 && scene == "EndCredits") { SessionState.SetInt(Key, 4); VRSceneFlow.Instance.LoadByIndex(4); }
                else if (step == 4 && scene == "Scene4") { SessionState.SetInt(Key, 5); Button("MENU Button").onClick.Invoke(); }
                else if (step == 5 && scene == "StartMenu") Finish(true, "Scene3 NEXT -> Scene4; timer/progress active; Scene4 NEXT -> EndCredits; Scene4 MENU -> StartMenu.");
            }
            catch (Exception e) { Finish(false, e.Message); }
        }
        static Button Button(string name) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == name);
        static void Finish(bool passed, string message)
        {
            Directory.CreateDirectory("Artifacts/OceanStorm");
            File.WriteAllText("Artifacts/OceanStorm/Scene4NavigationValidation.txt", (passed ? "PASS: " : "FAIL: ") + message);
            Debug.Log("SCENE4_NAVIGATION_" + (passed ? "PASS: " : "FAIL: ") + message);
            SessionState.SetInt(Key, 6); EditorApplication.ExitPlaymode();
        }
    }
}
