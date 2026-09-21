using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace VRAnimationTemplate.Editor
{
    /// <summary>Bakes the supplied Mixamo capture onto the rigid robot, accounting for its different joint axes and proportions.</summary>
    public static class ForestRobotMocapBuilder
    {
        public const string SourcePath = "Assets/VRAnimationTemplate/Mocap/Look Around.fbx";
        const string ClipPath = "Assets/VRAnimationTemplate/Mocap/Giant Look Around.anim";
        const string ScenePath = "Assets/VRAnimationTemplate/Scenes/Scene2.unity";
        class Mapping
        {
            public Transform source, target;
            public Quaternion sourceReference, targetReference;
        }
        class FingerMapping
        {
            public Transform source, target;
            public bool thumb;
            public float spread;
        }

        [MenuItem("Tools/VR Animation Template/Apply Look Around Mocap to Scene 2")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before applying mocap.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(ScenePath);
            }
            var encounter = Object.FindFirstObjectByType<ForestRobotEncounter>();
            if (!encounter) throw new InvalidOperationException("Scene 2 has no forest robot.");
            Configure(encounter);
            var director = Object.FindFirstObjectByType<VRChapterDirector>();
            if (director) director.SetDuration(encounter.ChapterDuration);
            ValidateAndRender(encounter, Camera.main);
            encounter.Sample(0);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = encounter.robot.gameObject;
            Debug.Log("ROBOT_MOCAP_APPLIED_AND_VALIDATED");
        }

        public static void Configure(ForestRobotEncounter c)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            if (!model) throw new InvalidOperationException("Missing Look Around.fbx");
            AnimationClip sourceClip = AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<AnimationClip>().FirstOrDefault(a => !a.name.StartsWith("__preview__"));
            if (!sourceClip || sourceClip.length <= 0) throw new InvalidOperationException("FBX has no usable animation.");
            if (sourceClip.humanMotion) throw new InvalidOperationException("Import this source as Generic to retain its original joint curves.");
            GameObject source = Object.Instantiate(model);
            source.name = "Temporary mocap sampling skeleton";
            try
            {
                source.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                sourceClip.SampleAnimation(source, 0);
                var bones = source.GetComponentsInChildren<Transform>().ToDictionary(t => t.name);
                Transform Bone(string name) => bones.TryGetValue("mixamorig:" + name, out var t) ? t : throw new InvalidOperationException("Missing mocap bone: " + name);
                // Face the viewer at scene entry. The baked clip never translates or rotates this root.
                Vector3 viewer = Camera.main ? Camera.main.transform.position : Vector3.zero;
                Vector3 forward = viewer - c.robot.position; forward.y = 0;
                c.robot.rotation = Quaternion.LookRotation(forward.sqrMagnitude > .01f ? forward : Vector3.back);
                foreach (Transform t in new[] {c.hips,c.torso,c.head,c.leftHip,c.leftKnee,c.leftAnkle,c.rightHip,c.rightKnee,c.rightAnkle,c.leftShoulder,c.rightShoulder}) t.localRotation = Quaternion.identity;
                c.hips.localPosition = new Vector3(0, 12.05f, 0);
                c.head.localRotation = Quaternion.Euler(36, 0, 0);
                var mappings = new List<Mapping>();
                var fingerMappings = new List<FingerMapping>();
                void Map(string sourceName, Transform target)
                {
                    if (!target) throw new InvalidOperationException("Missing target for " + sourceName);
                    Transform b = Bone(sourceName);
                    mappings.Add(new Mapping { source=b, target=target, sourceReference=b.rotation, targetReference=Quaternion.Inverse(c.robot.rotation)*target.rotation });
                }
                Map("Hips",c.hips); Map("Spine2",c.torso); Map("Head",c.head);
                for(int side=0;side<2;side++)
                {
                    string prefix=side==0?"Left":"Right";
                    Transform arm=side==0?c.leftShoulder:c.rightShoulder;
                    Transform elbow=arm.Find("Elbow"), hand=elbow.Find("Wrist");
                    arm.localRotation=Quaternion.Euler(0,0,side==0?-8:8);
                    elbow.localRotation=Quaternion.Euler(-10,0,0); hand.localRotation=Quaternion.identity;
                    Map(prefix+"Arm",arm);Map(prefix+"ForeArm",elbow);Map(prefix+"Hand",hand);
                    string[] fingers={"Index","Middle","Ring","Pinky"};
                    for(int f=0;f<4;f++)
                    {
                        Transform finger=hand.Find("Finger "+f);
                        // Index finger must be nearest the thumb on each mirrored hand.
                        finger.localPosition=new Vector3((side==0?1:-1)*(1.5f-f)*.65f,-1.8f,0);
                        for(int segment=1;segment<=3;segment++)
                        {
                            // Robot phalanges point down (-Y); positive X curls them into the palm (-Z).
                            // Transfer local flexion only, not world-space wrist/arm rotation.
                            fingerMappings.Add(new FingerMapping {source=Bone(prefix+"Hand"+fingers[f]+segment),target=finger});
                            finger=finger.Find("Finger Joint");
                        }
                    }
                    fingerMappings.Add(new FingerMapping {source=Bone(prefix+"HandThumb1"),target=hand.Find("Thumb"),thumb=true,spread=side==0?35:-35});
                }
                Vector3 sourceHipStart=Bone("Hips").position;
                Vector3 leftFoot=c.robot.TransformPoint(new Vector3(-2.1f,1.8f,0));
                Vector3 rightFoot=c.robot.TransformPoint(new Vector3(2.1f,1.8f,0));
                var targets=mappings.Select(m=>m.target).Concat(fingerMappings.Select(m=>m.target)).Concat(new[]{c.leftHip,c.leftKnee,c.leftAnkle,c.rightHip,c.rightKnee,c.rightAnkle}).Distinct().ToArray();
                var curves=new Dictionary<string,AnimationCurve>();
                void Key(string path,string property,float time,float value)
                {
                    string key=path+"|"+property;
                    if(!curves.TryGetValue(key,out var curve)) {curve=new AnimationCurve();curves.Add(key,curve);}
                    curve.AddKey(time,value);
                }
                int frames=Mathf.CeilToInt(sourceClip.length*30);
                for(int frame=0;frame<=frames;frame++)
                {
                    float time=Mathf.Min(frame/30f,sourceClip.length);
                    sourceClip.SampleAnimation(source,time);
                    Vector3 displacement=Bone("Hips").position-sourceHipStart;
                    c.hips.localPosition=new Vector3(displacement.x*5,12.05f+displacement.y*12,displacement.z*5);
                    foreach(var m in mappings)
                        m.target.rotation=c.robot.rotation*(m.source.rotation*Quaternion.Inverse(m.sourceReference))*m.targetReference;
                    foreach(var finger in fingerMappings)
                    {
                        float flexion=Mathf.Clamp(Mathf.DeltaAngle(0,finger.source.localEulerAngles.x),0,finger.thumb?40:65);
                        finger.target.localRotation=Quaternion.Euler(flexion,0,finger.spread);
                    }
                    PlantLeg(c.robot,c.leftHip,c.leftKnee,c.leftAnkle,leftFoot);
                    PlantLeg(c.robot,c.rightHip,c.rightKnee,c.rightAnkle,rightFoot);
                    foreach(Transform t in targets)
                    {
                        string path=AnimationUtility.CalculateTransformPath(t,c.robot);
                        Quaternion q=t.localRotation;
                        Key(path,"localRotation.x",time,q.x);Key(path,"localRotation.y",time,q.y);Key(path,"localRotation.z",time,q.z);Key(path,"localRotation.w",time,q.w);
                    }
                    string hipsPath=AnimationUtility.CalculateTransformPath(c.hips,c.robot);
                    Key(hipsPath,"localPosition.x",time,c.hips.localPosition.x);Key(hipsPath,"localPosition.y",time,c.hips.localPosition.y);Key(hipsPath,"localPosition.z",time,c.hips.localPosition.z);
                }
                var baked=new AnimationClip {name="Giant Look Around",frameRate=30,wrapMode=WrapMode.ClampForever};
                foreach(var pair in curves)
                {
                    string[] key=pair.Key.Split('|');
                    for(int i=0;i<pair.Value.length;i++) {AnimationUtility.SetKeyLeftTangentMode(pair.Value,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(pair.Value,i,AnimationUtility.TangentMode.Linear);}
                    AnimationUtility.SetEditorCurve(baked,EditorCurveBinding.FloatCurve(key[0],typeof(Transform),key[1]),pair.Value);
                }
                baked.EnsureQuaternionContinuity();
                var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
                if(existing)
                {
                    existing.ClearCurves();
                    foreach(var binding in AnimationUtility.GetCurveBindings(baked))
                        AnimationUtility.SetEditorCurve(existing,binding,AnimationUtility.GetEditorCurve(baked,binding));
                    existing.frameRate=baked.frameRate;existing.wrapMode=baked.wrapMode;existing.EnsureQuaternionContinuity();
                    Object.DestroyImmediate(baked);EditorUtility.SetDirty(existing);baked=existing;
                }
                else AssetDatabase.CreateAsset(baked,ClipPath);
                c.lookAroundMotion=baked;c.motionSpeed=.3f;c.motionLeadIn=1.5f;
                var animator=c.robot.GetComponent<Animator>();if(!animator) animator=c.robot.gameObject.AddComponent<Animator>();
                animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                c.Sample(0);
                EditorUtility.SetDirty(c);
            }
            finally {Object.DestroyImmediate(source);}
        }

        static void PlantLeg(Transform root,Transform hip,Transform knee,Transform ankle,Vector3 foot)
        {
            float upper=knee.localPosition.magnitude,lower=ankle.localPosition.magnitude;
            Vector3 delta=foot-hip.position;
            float length=Mathf.Clamp(delta.magnitude,.01f,upper+lower-.001f);
            Vector3 direction=delta.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(root.forward,direction).normalized;
            float along=(upper*upper-lower*lower+length*length)/(2*length);
            Vector3 kneePoint=hip.position+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            hip.rotation=root.rotation*Quaternion.FromToRotation(Vector3.down,root.InverseTransformDirection(kneePoint-hip.position));
            knee.rotation=root.rotation*Quaternion.FromToRotation(Vector3.down,root.InverseTransformDirection(foot-knee.position));
            ankle.rotation=root.rotation;
        }

        public static void ValidateAndRender(ForestRobotEncounter c,Camera camera)
        {
            Directory.CreateDirectory("Artifacts/ForestEncounter");c.Sample(0);
            Vector3 root=c.robot.position,left=c.leftAnkle.position,right=c.rightAnkle.position;
            Quaternion first=c.head.localRotation;float motion=0,drift=0;
            var fingerJoints=new List<Transform>();
            foreach(var arm in new[]{c.leftShoulder,c.rightShoulder})
            {
                var hand=arm.Find("Elbow/Wrist");
                if(Mathf.Abs(hand.Find("Finger 0").localPosition.x-hand.Find("Thumb").localPosition.x)>
                   Mathf.Abs(hand.Find("Finger 3").localPosition.x-hand.Find("Thumb").localPosition.x))throw new Exception("Finger order is reversed");
                for(int f=0;f<4;f++) {var finger=hand.Find("Finger "+f);for(int s=0;s<3;s++){fingerJoints.Add(finger);finger=finger.Find("Finger Joint");}}
            }
            for(int i=0;i<=300;i++)
            {
                c.Sample(c.ChapterDuration*i/300f);
                drift=Mathf.Max(drift,Vector3.Distance(left,c.leftAnkle.position),Vector3.Distance(right,c.rightAnkle.position));
                motion=Mathf.Max(motion,Quaternion.Angle(first,c.head.localRotation));
                if(Vector3.Distance(root,c.robot.position)>.001f)throw new Exception("Robot root drifted");
                if(float.IsNaN(c.head.rotation.x))throw new Exception("Non-finite pose");
                foreach(var finger in fingerJoints)
                {
                    Vector3 rotation=finger.localEulerAngles;
                    float curl=Mathf.DeltaAngle(0,rotation.x);
                    if(curl<-.1f || curl>65.1f || Mathf.Abs(Mathf.DeltaAngle(0,rotation.y))>.1f || Mathf.Abs(Mathf.DeltaAngle(0,rotation.z))>.1f)
                        throw new Exception("Finger bent backwards or twisted: "+AnimationUtility.CalculateTransformPath(finger,c.robot)+" pose="+i+" angles="+rotation.ToString("F4"));
                }
            }
            if(drift>.035f)throw new Exception("Foot drift: "+drift);
            if(motion<10)throw new Exception("Mocap head motion missing");
            Vector3 toward=camera.transform.position-c.robot.position;toward.y=0;
            if(Vector3.Dot(c.robot.forward,toward.normalized)<.99f)throw new Exception("Robot is not facing viewer");
            float period=c.lookAroundMotion.length/Mathf.Max(.1f,c.motionSpeed);
            c.Sample(c.motionLeadIn+5);Quaternion cycleHead=c.head.localRotation;
            c.Sample(c.motionLeadIn+period+5);
            if(Quaternion.Angle(cycleHead,c.head.localRotation)>.1f)throw new Exception("Mocap did not repeat");
            c.Sample(c.MotionEnd-.8f);if(c.beams[0].gameObject.activeSelf)throw new Exception("Missing blink");
            c.Sample(c.MotionEnd+.1f);if(!c.beams[0].gameObject.activeSelf)throw new Exception("Eyes stayed closed");
            Quaternion original=camera.transform.rotation;
            var ui=GameObject.Find("VR UI");bool wasActive=ui && ui.activeSelf;
            try
            {
                if(ui)ui.SetActive(false);
                camera.transform.rotation=Quaternion.LookRotation(c.head.position-camera.transform.position);
                foreach(float t in new[]{0f,7f,13f,19f,28f})
                {
                    c.Sample(t);var rt=new RenderTexture(1280,900,24);var tex=new Texture2D(1280,900,TextureFormat.RGB24,false);
                    try {camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,900),0,0);tex.Apply();File.WriteAllBytes("Artifacts/ForestEncounter/mocap-"+t+".png",tex.EncodeToPNG());}
                    finally {camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);}
                }
            }
            finally {camera.transform.rotation=original;if(ui)ui.SetActive(wasActive);c.Sample(0);}
            File.WriteAllText("Artifacts/ForestEncounter/mocap-validation.txt","301 samples passed. Source length="+c.lookAroundMotion.length+"; speed="+c.motionSpeed+"; loop period="+period+"; chapter="+c.ChapterDuration+"; max foot drift="+drift+"m; head rotation range="+motion+" degrees. Facing viewer, stationary root, repeated pose and blinking verified. Previews rendered without the UI; scene UI preserved. Headset performance not tested.");
        }
    }
}
