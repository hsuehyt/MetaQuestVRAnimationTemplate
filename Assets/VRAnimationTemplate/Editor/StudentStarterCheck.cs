using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRAnimationTemplate;

[InitializeOnLoad]
public static class StudentStarterCheck
{
    const string Key = "StudentStarterCheck.Active";
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static int chapter, clicks;
    static float lastElapsed, started;
    static bool wrapped, aiming;
    static Quaternion initialRotation;
    static Button target;
    static string report = "";
    static StudentStarterCheck() { EditorApplication.update += Tick; }
    public static void Run()
    {
        Directory.CreateDirectory("Artifacts");
        foreach (string name in new[] { "StartMenu", "Scene1", "Scene2", "Scene3", "EndCredits" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/VRAnimationTemplate/Scenes/" + name + ".unity");
            foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) throw new Exception(name + ": missing script");
                var it = new SerializedObject(component).GetIterator();
                while (it.Next(true))
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == null && it.objectReferenceInstanceIDValue != 0)
                        throw new Exception(name + ": missing reference " + it.propertyPath);
            }
            foreach (var b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
                if (b.onClick.GetPersistentEventCount() != 1 || b.onClick.GetPersistentTarget(0) == null)
                    throw new Exception(name + ": invalid navigation binding");
        }
        EditorSceneManager.OpenScene("Assets/VRAnimationTemplate/Scenes/Scene1.unity");
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
        try
        {
            var director = UnityEngine.Object.FindFirstObjectByType<VRChapterDirector>();
            var animation = UnityEngine.Object.FindFirstObjectByType<DemoOrbitAnimation>();
            if (director == null || animation == null) throw new Exception("Chapter or original animation missing");
            int current = int.Parse(SceneManager.GetActiveScene().name.Substring(5));
            float elapsed = (float)typeof(VRChapterDirector).GetField("elapsed", Flags).GetValue(director);
            if (chapter != current)
            {
                chapter = current; started = Time.time; lastElapsed = 0; wrapped = aiming = false; clicks = 0;
                initialRotation = animation.transform.rotation;
                if ((bool)typeof(VRChapterDirector).GetField("autoAdvance", Flags).GetValue(director)) throw new Exception("Auto advance enabled");
                foreach (var b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
                    if (b.targetGraphic.color != new Color(.14f,.17f,.25f,1)) throw new Exception("Idle color incorrect");
            }
            if (elapsed < lastElapsed) wrapped = true;
            lastElapsed = elapsed;
            if (Time.time - started > 13 && !aiming)
            {
                if (!wrapped || Quaternion.Angle(initialRotation,animation.transform.rotation) < 1) throw new Exception("Loop or animation failed");
                foreach (var driver in UnityEngine.Object.FindObjectsByType<QuestCameraPoseDriver>(FindObjectsSortMode.None)) driver.enabled = false;
                target = UnityEngine.Object.FindFirstObjectByType<Button>();
                target.onClick = new Button.ButtonClickedEvent();
                target.onClick.AddListener(() => clicks++);
                Camera.main.transform.LookAt(target.transform.position);
                aiming = true;
            }
            if (Time.time - started > 15)
            {
                if (clicks == 0 || target.targetGraphic.color != new Color(.10f,.55f,.85f,1)) throw new Exception("Gaze highlight or dwell failed");
                report += "Scene" + chapter + ": PASS - original animation moves, countdown wraps, stays in chapter, gaze turns blue and dwell activates.\n";
                if (chapter < 3) SceneManager.LoadScene("Scene" + (chapter+1));
                else Finish(0, report + "All five scenes: no missing scripts/references; navigation bindings intact.\n");
            }
        }
        catch (Exception e) { Finish(1, report + e); }
    }
    static void Finish(int code, string result)
    {
        SessionState.SetBool(Key,false);
        File.WriteAllText("Artifacts/StudentStarterValidation.txt", result);
        EditorApplication.Exit(code);
    }
}
