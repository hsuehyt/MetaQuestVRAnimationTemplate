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
    public static class OceanStormChapterBuilder
    {
        public const string ScenePath = "Assets/VRAnimationTemplate/Scenes/Scene4.unity";
        static readonly string[] Names = { "StartMenu", "Scene1", "Scene2", "Scene3", "Scene4", "EndCredits" };
        static readonly string[] UIRoots = { "VR Scene Flow", "EventSystem", "VR UI", "Gaze Reticle Canvas", "Chapter Director" };

        [MenuItem("Tools/VR Animation Template/Add Ocean Storm as Scene 4")]
        public static void Install()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) CreateSceneFour();
            // Update only the serialized sequence, preserving the existing chapters' authored content.
            foreach (string name in Names)
            {
                string path = "Assets/VRAnimationTemplate/Scenes/" + name + ".unity";
                string text = File.ReadAllText(path);
                text = System.Text.RegularExpressions.Regex.Replace(text,
                    @"(  - Scene3\r?\n)(  - EndCredits)", "$1  - Scene4\n$2");
                if (name == "StartMenu") text = text.Replace("A five-scene presentation template", "A six-scene presentation template");
                if (text != File.ReadAllText(path)) File.WriteAllText(path, text);
            }
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            int index = scenes.FindIndex(s => s.path.EndsWith("/Scene3.unity"));
            if (index < 0) throw new InvalidOperationException("Scene3 is missing from Build Settings.");
            scenes.Insert(index + 1, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.Refresh(); AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePath);
            PositionUI();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            ValidateAndRender();
            Debug.Log("OCEAN_SCENE4_INSTALL_OK");
        }

        public static void CreateSceneFour()
        {
            if (!File.Exists(OceanStormBuilder.ScenePath)) OceanStormBuilder.Build();
            var ocean = EditorSceneManager.OpenScene(OceanStormBuilder.ScenePath, OpenSceneMode.Single);
            var camera = Camera.main;
            var source = EditorSceneManager.OpenScene("Assets/VRAnimationTemplate/Scenes/Scene1.unity", OpenSceneMode.Additive);
            float offset = camera.transform.position.y - source.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Camera>()).Single().transform.position.y;
            // Move all existing UI roots together so their persistent event targets remain intact.
            foreach (string name in UIRoots)
            {
                var root = source.GetRootGameObjects().Single(r => r.name == name);
                SceneManager.MoveGameObjectToScene(root, ocean);
                if (name == "VR UI") root.transform.position += Vector3.up * offset;
            }
            foreach (var canvas in ocean.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)))
                canvas.worldCamera = camera;
            var gaze = UnityEngine.Object.FindFirstObjectByType<VRGazeInputModule>();
            var gazeSettings = new SerializedObject(gaze);
            gazeSettings.FindProperty("gazeCamera").objectReferenceValue = camera;
            gazeSettings.ApplyModifiedPropertiesWithoutUndo();
            foreach (var text in ocean.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Text>(true)))
                if (text.text == "SCENE 1") text.text = "SCENE 4";
            var flow = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<VRSceneFlow>());
            var names = flow.FindProperty("sceneNames"); names.arraySize = Names.Length;
            for (int i = 0; i < Names.Length; i++) names.GetArrayElementAtIndex(i).stringValue = Names[i];
            flow.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.CloseScene(source, true);
            SceneManager.SetActiveScene(ocean);
            PositionUI();
            // Save under a new path: the standalone OceanStorm scene remains available.
            EditorSceneManager.SaveScene(ocean, ScenePath);
        }

        static void PositionUI()
        {
            Canvas.ForceUpdateCanvases();
            var rect = GameObject.Find("VR UI").GetComponent<RectTransform>();
            rect.anchoredPosition3D = new Vector3(0, Camera.main.transform.position.y - .05f, 2);
            EditorUtility.SetDirty(rect);
        }

        static void ValidateAndRender()
        {
            var camera = Camera.main;
            if (UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length != 1)
                throw new Exception("Scene4 must have exactly one camera.");
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.worldCamera != camera) throw new Exception("UI camera reference is missing.");
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (string name in new[] { "MENU Button", "SKIP / NEXT Button" })
            {
                var button = buttons.Single(b => b.name == name);
                if (!(button.onClick.GetPersistentTarget(0) is VRSceneFlow)) throw new Exception("Broken navigation event: " + name);
            }
            var director = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<VRChapterDirector>());
            if (!director.FindProperty("timeLabel").objectReferenceValue || !director.FindProperty("progressFill").objectReferenceValue)
                throw new Exception("Timer/progress UI reference is missing.");
            Directory.CreateDirectory("Artifacts/OceanStorm");
            var rt = new RenderTexture(1440, 900, 24); var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); image.Apply();
                File.WriteAllBytes("Artifacts/OceanStorm/Scene4Preview.png", image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
        }
    }
}
