using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VRAnimationTemplate.Editor
{
    public static class VRAnimationTemplateBuilder
    {
        private const string SceneFolder = "Assets/VRAnimationTemplate/Scenes";
        private const string MaterialFolder = "Assets/VRAnimationTemplate/GeneratedMaterials";
        private static readonly string[] SceneNames =
        {
            "StartMenu", "Scene1", "Scene2", "Scene3", "EndCredits"
        };

        private static readonly Color[] SceneColors =
        {
            new Color(0.018f, 0.025f, 0.06f),
            new Color(0.02f, 0.10f, 0.16f),
            new Color(0.12f, 0.035f, 0.16f),
            new Color(0.15f, 0.07f, 0.025f),
            new Color(0.012f, 0.012f, 0.025f)
        };

        [MenuItem("Tools/VR Animation Template/Generate All Scenes")]
        public static void GenerateAllScenes()
        {
            EnsureFolder("Assets", "VRAnimationTemplate");
            EnsureFolder("Assets/VRAnimationTemplate", "Scenes");
            EnsureFolder("Assets/VRAnimationTemplate", "GeneratedMaterials");

            for (int i = 0; i < SceneNames.Length; i++)
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Camera camera = CreateCamera(SceneColors[i]);
                CreateEnvironment(i, SceneColors[i]);
                VRSceneFlow flow = new GameObject("VR Scene Flow").AddComponent<VRSceneFlow>();
                VRGazeInputModule gaze = CreateEventSystem();
                Canvas canvas = CreateCanvas(camera, gaze);

                if (i == 0)
                    BuildStartMenu(canvas, flow);
                else if (i == SceneNames.Length - 1)
                    BuildCredits(canvas, flow);
                else
                    BuildChapter(canvas, flow, i);

                string path = $"{SceneFolder}/{SceneNames[i]}.unity";
                EditorSceneManager.SaveScene(scene, path);
            }

            var buildScenes = new EditorBuildSettingsScene[SceneNames.Length];
            for (int i = 0; i < SceneNames.Length; i++)
                buildScenes[i] = new EditorBuildSettingsScene($"{SceneFolder}/{SceneNames[i]}.unity", true);
            EditorBuildSettings.scenes = buildScenes;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene($"{SceneFolder}/StartMenu.unity");
            EditorUtility.DisplayDialog(
                "VR Animation Template",
                "Five scenes were generated and added to Build Settings. Press Play and hold the center gaze on Start.",
                "Ready");
        }

        private static Camera CreateCamera(Color background)
        {
            var rig = new GameObject("Generated XR Rig");
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(rig.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 250f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<QuestCameraPoseDriver>();
            return camera;
        }

        private static void CreateEnvironment(int sceneIndex, Color accent)
        {
            var lightObject = new GameObject("Key Light");
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
            Renderer renderer = floor.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial("Floor Material", Color.Lerp(accent, Color.gray, 0.72f));

            if (sceneIndex > 0 && sceneIndex < 4)
            {
                GameObject animated = GameObject.CreatePrimitive(sceneIndex == 1
                    ? PrimitiveType.Cube
                    : sceneIndex == 2 ? PrimitiveType.Sphere : PrimitiveType.Capsule);
                animated.name = "Animation Placeholder (Replace Me)";
                animated.transform.position = new Vector3(0f, 1.45f, 3.1f);
                animated.transform.localScale = Vector3.one * 0.65f;
                animated.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                    $"Scene {sceneIndex} Accent", Color.Lerp(accent, Color.white, 0.35f));
                animated.AddComponent<DemoOrbitAnimation>();
            }
        }

        private static Material CreateMaterial(string name, Color color)
        {
            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
            Shader shader = pipeline != null ? pipeline.defaultShader : Shader.Find("Standard");
            if (shader == null)
                throw new System.InvalidOperationException("The active render pipeline has no default material shader.");

            string path = $"{MaterialFolder}/{name.Replace(' ', '_')}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = shader;
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static VRGazeInputModule CreateEventSystem()
        {
            var eventObject = new GameObject("EventSystem");
            eventObject.AddComponent<EventSystem>();
            return eventObject.AddComponent<VRGazeInputModule>();
        }

        private static Canvas CreateCanvas(Camera camera, VRGazeInputModule gaze)
        {
            var canvasObject = new GameObject("VR UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1440f, 900f);
            canvasRect.position = new Vector3(0f, 1.6f, 2f);
            canvasRect.rotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * 0.001f;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            var reticleCanvasObject = new GameObject("Gaze Reticle Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas reticleCanvas = reticleCanvasObject.GetComponent<Canvas>();
            reticleCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            reticleCanvas.worldCamera = camera;
            reticleCanvas.planeDistance = 0.5f;
            reticleCanvas.sortingOrder = 100;

            Image reticle = CreateImage(reticleCanvas.transform, "Gaze Reticle", Color.white);
            RectTransform rect = reticle.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(20f, 20f);
            reticle.raycastTarget = false;

            SerializedObject serializedGaze = new SerializedObject(gaze);
            serializedGaze.FindProperty("reticleProgress").objectReferenceValue = reticle;
            serializedGaze.ApplyModifiedPropertiesWithoutUndo();
            return canvas;
        }

        private static void BuildStartMenu(Canvas canvas, VRSceneFlow flow)
        {
            Image panel = CreatePanel(canvas.transform, new Color(0.02f, 0.035f, 0.09f, 0.92f), new Vector2(720f, 590f));
            CreateText(panel.transform, "META QUEST", 28, FontStyle.Bold, new Vector2(0f, 220f), new Vector2(620f, 50f), new Color(0.35f, 0.85f, 1f));
            CreateText(panel.transform, "VR ANIMATION", 64, FontStyle.Bold, new Vector2(0f, 145f), new Vector2(650f, 85f), Color.white);
            CreateText(panel.transform, "A five-scene presentation template", 24, FontStyle.Normal, new Vector2(0f, 78f), new Vector2(620f, 45f), new Color(0.72f, 0.78f, 0.9f));

            Button start = CreateButton(panel.transform, "START", new Vector2(0f, -20f), new Vector2(420f, 82f), new Color(0.10f, 0.55f, 0.85f));
            UnityEventTools.AddPersistentListener(start.onClick, flow.StartExperience);
            Button quit = CreateButton(panel.transform, "QUIT", new Vector2(0f, -130f), new Vector2(420f, 70f), new Color(0.14f, 0.17f, 0.25f));
            UnityEventTools.AddPersistentListener(quit.onClick, flow.QuitApplication);
            CreateText(panel.transform, "Look at a button to select", 19, FontStyle.Italic, new Vector2(0f, -245f), new Vector2(600f, 36f), new Color(0.62f, 0.68f, 0.78f));
        }

        private static void BuildChapter(Canvas canvas, VRSceneFlow flow, int chapterNumber)
        {
            Image header = CreatePanel(canvas.transform, new Color(0.015f, 0.02f, 0.04f, 0.76f), new Vector2(1120f, 115f));
            header.rectTransform.anchorMin = header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -80f);

            CreateText(header.transform, $"SCENE {chapterNumber}", 40, FontStyle.Bold, new Vector2(-370f, 14f), new Vector2(330f, 56f), Color.white);
            Text time = CreateText(header.transform, "12s", 24, FontStyle.Bold, new Vector2(400f, 14f), new Vector2(150f, 50f), new Color(0.3f, 0.85f, 1f));
            Image barBack = CreateImage(header.transform, "Progress Background", new Color(1f, 1f, 1f, 0.14f));
            SetRect(barBack.rectTransform, new Vector2(0f, -30f), new Vector2(940f, 8f));
            Image fill = CreateImage(barBack.transform, "Progress Fill", new Color(0.25f, 0.82f, 1f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            Stretch(fill.rectTransform);

            Image footer = CreatePanel(canvas.transform, new Color(0.015f, 0.02f, 0.04f, 0.76f), new Vector2(760f, 100f));
            footer.rectTransform.anchorMin = footer.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            footer.rectTransform.anchoredPosition = new Vector2(0f, -400f);
            Button menu = CreateButton(footer.transform, "MENU", new Vector2(-190f, 0f), new Vector2(290f, 64f), new Color(0.14f, 0.17f, 0.25f));
            UnityEventTools.AddPersistentListener(menu.onClick, flow.ReturnToMenu);
            Button skip = CreateButton(footer.transform, "SKIP / NEXT", new Vector2(190f, 0f), new Vector2(330f, 64f), new Color(0.10f, 0.55f, 0.85f));
            UnityEventTools.AddPersistentListener(skip.onClick, flow.NextScene);

            VRChapterDirector director = new GameObject("Chapter Director").AddComponent<VRChapterDirector>();
            SerializedObject serialized = new SerializedObject(director);
            serialized.FindProperty("durationSeconds").floatValue = 12f;
            serialized.FindProperty("autoAdvance").boolValue = true;
            serialized.FindProperty("timeLabel").objectReferenceValue = time;
            serialized.FindProperty("progressFill").objectReferenceValue = fill;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildCredits(Canvas canvas, VRSceneFlow flow)
        {
            Image panel = CreatePanel(canvas.transform, new Color(0.015f, 0.02f, 0.05f, 0.88f), new Vector2(820f, 700f));
            CreateText(panel.transform, "END CREDITS", 52, FontStyle.Bold, new Vector2(0f, 275f), new Vector2(700f, 70f), Color.white);

            Text credits = CreateText(panel.transform,
                "YOUR PROJECT TITLE\n\nCreated by\nYour Name\n\nAnimation\nArtist Name\n\nSound and Music\nComposer Name\n\nSpecial Thanks\nMeta Quest Community\n\nMade with Unity",
                27, FontStyle.Normal, new Vector2(0f, -95f), new Vector2(680f, 520f), new Color(0.8f, 0.86f, 0.96f));
            credits.alignment = TextAnchor.UpperCenter;

            CreditsScroller scroller = panel.gameObject.AddComponent<CreditsScroller>();
            SerializedObject serialized = new SerializedObject(scroller);
            serialized.FindProperty("credits").objectReferenceValue = credits.rectTransform;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Button replay = CreateButton(panel.transform, "REPLAY", new Vector2(-185f, -300f), new Vector2(300f, 64f), new Color(0.10f, 0.55f, 0.85f));
            UnityEventTools.AddPersistentListener(replay.onClick, flow.ReplayExperience);
            Button menu = CreateButton(panel.transform, "MENU", new Vector2(185f, -300f), new Vector2(300f, 64f), new Color(0.14f, 0.17f, 0.25f));
            UnityEventTools.AddPersistentListener(menu.onClick, flow.ReturnToMenu);
        }

        private static Image CreatePanel(Transform parent, Color color, Vector2 size)
        {
            Image image = CreateImage(parent, "Panel", color);
            SetRect(image.rectTransform, Vector2.zero, size);
            return image;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string value, int size, FontStyle style, Vector2 position, Vector2 dimensions, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(text.rectTransform, position, dimensions);
            return text;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 position, Vector2 dimensions, Color color)
        {
            Image image = CreateImage(parent, label + " Button", color);
            SetRect(image.rectTransform, position, dimensions);
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.24f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(color, Color.white, 0.38f);
            button.colors = colors;
            CreateText(image.transform, label, 25, FontStyle.Bold, Vector2.zero, dimensions, Color.white);
            return button;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
