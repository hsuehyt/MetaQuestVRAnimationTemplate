using System;
using System.IO;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace VRAnimationTemplate.Editor
{
    /// <summary>Import the joint-baked Maya dancer and place it in the existing third chapter.</summary>
    public static class DancingFatManBuilder
    {
        const string Folder = "Assets/VRAnimationTemplate/DancingFatMan";
        const string ModelPath = Folder + "/DancingFatMan.fbx";
        const string ScenePath = "Assets/VRAnimationTemplate/Scenes/Scene3.unity";
        const string ArtifactFolder = "Artifacts/DancingFatMan";

        [Serializable] class Report
        {
            public string clip;
            public float clipSeconds, restHeightMetres, loopJointError, bellyTravelMetres, jiggleBakeErrorMetres, animatorMotionMetres;
            public int meshCount, boneCount, sampledFrames;
            public Vector3 scenePosition, sceneScale, importedRestSize, animatedBoundsSize;
            public bool loop, bellyCurvesPresent;
        }

        static Material Material(string name, Color color, float smoothness)
        {
            string path = Folder + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(m);
            return m;
        }

        [MenuItem("Tools/VR Animation Template/Import Dancing Fat Man into Scene 3")]
        public static void Build()
        {
            Directory.CreateDirectory(ArtifactFolder);
            if (!File.Exists(ArtifactFolder + "/Scene3.before-dancer.unity"))
                File.Copy(ScenePath, ArtifactFolder + "/Scene3.before-dancer.unity");
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.globalScale = 1;
            importer.useFileScale = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importNormals = ModelImporterNormals.Import;
            importer.isReadable = true;
            importer.SaveAndReimport();
            var takes = importer.defaultClipAnimations;
            Debug.Log("Dancer imported takes: " + string.Join(", ", takes.Select(t => t.name + " " + t.firstFrame + ".." + t.lastFrame)));
            var take = takes.First(t => t.name.Contains("BellyDance"));
            take.name = "Belly Dance - Jiggle 2";
            take.loopTime = true;
            // Maya already has an identical endpoint. Do not re-blend the authored motion.
            take.loopPose = false;
            importer.clipAnimations = new[] { take };
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var clip = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Single(a => !a.name.StartsWith("__preview__"));
            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (!bindings.Any(b => b.path.EndsWith("Dance_BellyJiggle_JNT") && b.propertyName.StartsWith("m_LocalPosition")))
                throw new Exception("The FBX is missing baked belly translations.");
            if (Mathf.Abs(clip.length - 448f / 24) > .002f) throw new Exception("Unexpected dance duration: " + clip.length);

            string controllerPath = Folder + "/BellyDance.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.FirstOrDefault().state;
            if (!state) state = machine.AddState("Belly Dance - Jiggle 2");
            state.motion = clip; state.speed = 1; machine.defaultState = state;
            EditorUtility.SetDirty(controller);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
                if (root.name.StartsWith("Animation Placeholder") || root.name == "Dancing Fat Man") Object.DestroyImmediate(root);
            var dancer = (GameObject)PrefabUtility.InstantiatePrefab(model);
            dancer.name = "Dancing Fat Man";
            var renderers = dancer.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var body = renderers.Single(r => r.name == "Dance_Body");
            // The undeformed mesh is authored at 200 cm. Measure its imported bind geometry.
            var restPoints = body.sharedMesh.vertices.Select(v => body.transform.TransformPoint(v)).ToArray();
            Bounds rest = new Bounds(restPoints[0], Vector3.zero);
            foreach (var p in restPoints) rest.Encapsulate(p);
            float scale = 2f / rest.size.y;
            dancer.transform.localScale *= scale;
            var camera = Camera.main;
            if (!camera) throw new Exception("Scene3 has no viewer camera.");
            var forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            dancer.transform.position = new Vector3(camera.transform.position.x, 0, camera.transform.position.z) + forward * 4.2f;
            dancer.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);
            var animator = dancer.GetComponent<Animator>();
            if (!animator) animator = dancer.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var skin = Material("Warm Skin", new Color(.65f, .43f, .30f), .23f);
            var eyes = Material("Eye White", new Color(.92f, .90f, .82f), .4f);
            var dark = Material("Pupils and Mouth", new Color(.025f, .017f, .014f), .28f);
            foreach (var r in renderers)
            {
                r.gameObject.SetActive(true); r.enabled = true;
                var mat = r.name == "Dance_Body" ? skin :
                    (r.name == "Dance_FaceDetail2" || r.name == "Dance_FaceDetail4" ? eyes : dark);
                r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMesh.subMeshCount).ToArray();
                r.quality = SkinQuality.Bone4;
                r.updateWhenOffscreen = true;
            }
            // Leave manual navigation available while the dance loops continuously.
            foreach (var director in Object.FindObjectsByType<VRChapterDirector>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(director);
                so.FindProperty("autoAdvance").boolValue = false;
                so.FindProperty("durationSeconds").floatValue = clip.length;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var report = Validate(dancer, clip);
            // Exercise the actual default Animator state, not only clip.SampleAnimation.
            animator.Rebind(); animator.Update(0);
            var bellyJoint = dancer.GetComponentsInChildren<Transform>().Single(t => t.name == "Dance_BellyJiggle_JNT");
            animator.Play(state.name, 0, 0); animator.Update(0);
            Vector3 bellyStart = bellyJoint.position;
            animator.Play(state.name, 0, .4f); animator.Update(0);
            report.animatorMotionMetres = Vector3.Distance(bellyStart, bellyJoint.position);
            if (report.animatorMotionMetres < .01f) throw new Exception("Animator did not play the dance.");
            report.restHeightMetres = rest.size.y * scale;
            report.importedRestSize = rest.size;
            report.scenePosition = dancer.transform.position;
            report.sceneScale = dancer.transform.localScale;
            File.WriteAllText(ArtifactFolder + "/unity-validation.json", JsonUtility.ToJson(report, true));
            animator.Play(state.name, 0, 0); animator.Update(0);
            clip.SampleAnimation(dancer, 0);
            PrefabUtility.SaveAsPrefabAsset(dancer, Folder + "/DancingFatMan.prefab");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            // Export viewer-camera previews from the actual Unity render pipeline.
            foreach (int frame in new[] { 1, 149, 262, 323 })
            {
                animator.Play(state.name, 0, (frame - 1) / 448f);
                animator.Update(0);
                Render(camera, ArtifactFolder + "/Scene3-frame" + frame + ".png");
            }
            clip.SampleAnimation(dancer, 0);
            Debug.Log("DANCER_COMPLETE " + JsonUtility.ToJson(report));
        }

        static Report Validate(GameObject dancer, AnimationClip clip)
        {
            var all = dancer.GetComponentsInChildren<Transform>(true);
            var belly = all.Single(t => t.name == "Dance_BellyJiggle_JNT");
            var renderers = dancer.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 6) throw new Exception("Expected body plus five face meshes.");
            var bounds = renderers.ToDictionary(r => r, r => new Bounds());
            var mesh = new Mesh(); var min = Vector3.positiveInfinity; var max = Vector3.negativeInfinity;
            var reference = File.ReadAllLines(ArtifactFolder + "/maya-jiggle2-reference.csv")
                .Select(line => line.Split(',').Select(v => float.Parse(v, CultureInfo.InvariantCulture)).ToArray()).ToArray();
            float jiggleError = 0;
            float modelScale = dancer.transform.localScale.x;
            Bounds world = new Bounds();
            for (int f = 0; f <= 448; f++)
            {
                clip.SampleAnimation(dancer, f / 24f);
                min = Vector3.Min(min, belly.localPosition); max = Vector3.Max(max, belly.localPosition);
                // Compare per-axis distances: FBX changes handedness but preserves each axis magnitude.
                Vector3 actual = Vector3.Scale(belly.localPosition, belly.parent.lossyScale);
                for (int axis = 0; axis < 3; axis++)
                    jiggleError = Mathf.Max(jiggleError, Mathf.Abs(Mathf.Abs(actual[axis]) - Mathf.Abs(reference[f][axis]) * .01f * modelScale));
                foreach (var r in renderers)
                {
                    r.BakeMesh(mesh);
                    var b = bounds[r];
                    if (f == 0) b = mesh.bounds; else b.Encapsulate(mesh.bounds);
                    bounds[r] = b;
                    foreach (var v in mesh.vertices)
                    {
                        var p = r.transform.TransformPoint(v);
                        if (float.IsNaN(p.x) || float.IsInfinity(p.x)) throw new Exception("Invalid skinned vertex.");
                        if (f == 0 && r == renderers[0] && world.size == Vector3.zero) world = new Bounds(p, Vector3.zero);
                        else world.Encapsulate(p);
                    }
                }
            }
            Object.DestroyImmediate(mesh);
            foreach (var r in renderers) { var b = bounds[r]; b.Expand(.1f); r.localBounds = b; }
            clip.SampleAnimation(dancer, 0);
            var start = all.Select(t => t.localToWorldMatrix).ToArray();
            clip.SampleAnimation(dancer, clip.length);
            float error = 0;
            for (int i = 0; i < all.Length; i++)
                for (int j = 0; j < 16; j++) error = Mathf.Max(error, Mathf.Abs(start[i][j] - all[i].localToWorldMatrix[j]));
            if (error > .002f) throw new Exception("Dance loop joint seam: " + error);
            // FBX may represent bone lengths in centimetres under a 0.01-scaled hierarchy.
            float travel = belly.parent.TransformVector(max - min).magnitude;
            if (travel < .03f || travel > .3f) throw new Exception("Unexpected belly travel (metres): " + travel);
            if (jiggleError > .001f) throw new Exception("Belly jiggle differs from Maya amount 2: " + jiggleError);
            return new Report { clip = clip.name, clipSeconds = clip.length, loop = clip.isLooping,
                meshCount = renderers.Length, boneCount = all.Count(t => t.name.EndsWith("_JNT")),
                sampledFrames = 449, loopJointError = error, bellyTravelMetres = travel,
                bellyCurvesPresent = true, animatedBoundsSize = world.size, jiggleBakeErrorMetres = jiggleError };
        }

        static void Render(Camera camera, string path)
        {
            // Synchronous batch renders share one editor frame; GPU skinning can retain
            // the first pose. Bake temporary preview meshes from the current joint pose.
            var skins = GameObject.Find("Dancing Fat Man").GetComponentsInChildren<SkinnedMeshRenderer>();
            var snapshots = new System.Collections.Generic.List<GameObject>();
            foreach (var skin in skins)
            {
                var mesh = new Mesh(); skin.BakeMesh(mesh);
                var snapshot = new GameObject("Preview snapshot");
                snapshot.transform.SetParent(skin.transform, false);
                snapshot.AddComponent<MeshFilter>().sharedMesh = mesh;
                snapshot.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                skin.enabled = false; snapshots.Add(snapshot);
            }
            var target = RenderTexture.GetTemporary(1280, 960, 24);
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            camera.targetTexture = oldTarget; RenderTexture.active = oldActive; RenderTexture.ReleaseTemporary(target);
            foreach (var snapshot in snapshots)
            {
                Object.DestroyImmediate(snapshot.GetComponent<MeshFilter>().sharedMesh);
                Object.DestroyImmediate(snapshot);
            }
            foreach (var skin in skins) skin.enabled = true;
        }

        public static void InspectPlayback()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var dancer = GameObject.Find("Dancing Fat Man");
            var animator = dancer.GetComponent<Animator>();
            var clip = animator.runtimeAnimatorController.animationClips[0];
            var all = dancer.GetComponentsInChildren<Transform>();
            var lines = new System.Collections.Generic.List<string>();
            lines.AddRange(AnimationUtility.GetCurveBindings(clip).Where(b => b.path.Contains("UpperArm") || b.path.EndsWith("Dance_Hips_JNT")).Select(b => b.path + " " + b.propertyName + " keys=" + AnimationUtility.GetEditorCurve(clip,b).length));
            animator.Rebind(); animator.Update(0);
            foreach (int f in new[] {1,149,262})
            {
                clip.SampleAnimation(dancer,(f-1)/24f);
                foreach(var t in all.Where(t => new[]{"Dance_L_Hand_JNT","Dance_L_UpperArm_JNT","Dance_Hips_JNT"}.Contains(t.name)))
                    lines.Add("sample " + f + " " + t.name + " modelpos=" + dancer.transform.InverseTransformPoint(t.position).ToString("F6") + " localrot=" + t.localEulerAngles.ToString("F6"));
                animator.Play("Belly Dance - Jiggle 2",0,(f-1)/448f); animator.Update(0);
                foreach(var t in all.Where(t => new[]{"Dance_L_Hand_JNT","Dance_L_UpperArm_JNT","Dance_Hips_JNT"}.Contains(t.name)))
                    lines.Add("animator " + f + " " + t.name + " modelpos=" + dancer.transform.InverseTransformPoint(t.position).ToString("F6") + " localrot=" + t.localEulerAngles.ToString("F6"));
            }
            File.WriteAllLines(ArtifactFolder+"/playback-inspection.txt",lines);
        }
    }
}
