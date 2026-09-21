using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VRAnimationTemplate.Editor
{
    public static class DancerFurBuilder
    {
        const string Folder="Assets/VRAnimationTemplate/DancingFatMan/Fur";
        const string Scene="Assets/VRAnimationTemplate/Scenes/Scene3.unity";
        const string Reports="Artifacts/DancingFatMan/Fur";
        static float Smooth(float x) { x=Mathf.Clamp01(x); return x*x*(3-2*x); }
        static bool Bare(Vector3 p,Vector3 n)
        {
            return (p.y<.035f && n.y<-.1f) || (Mathf.Abs(p.x)>.565f && p.y<1.035f && n.z>.05f);
        }
        [MenuItem("Tools/VR Animation Template/Generate Fur in Scene 3")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder); Directory.CreateDirectory(Reports); AssetDatabase.Refresh();
            if(!File.Exists(Reports+"/Scene3.before-fur.unity")) File.Copy(Scene,Reports+"/Scene3.before-fur.unity");
            var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
            var dancer=GameObject.Find("Dancing Fat Man");
            if(!dancer) throw new Exception("Dancing Fat Man is missing from Scene3.");
            var existing=dancer.transform.Find("Fur"); if(existing) Object.DestroyImmediate(existing.gameObject);
            var body=dancer.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Dance_Body");
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRAnimationTemplate/DancingFatMan/DancingFatMan.fbx");
            var original=model.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Dance_Body").sharedMesh;
            Vector3[] points=original.vertices,normals=original.normals; int[] triangles=original.triangles;
            // Source mesh coordinates are metres; maintain its exact topology and skin weights.
            var distance=SurfaceDistances(points,normals,triangles);
            var mesh=Object.Instantiate(original); mesh.name="Body with undercoat mask";
            var colors=new Color[points.Length];
            for(int v=0;v<colors.Length;v++) colors[v]=new Color(1,1,1,Bare(points[v],normals[v])?0:Smooth(distance[v]/.045f));
            mesh.colors=colors; mesh.SetUVs(2,points.ToList());
            string meshPath=Folder+"/UndercoatBody.asset";
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(old) { EditorUtility.CopySerialized(mesh,old); Object.DestroyImmediate(mesh); mesh=old; } else AssetDatabase.CreateAsset(mesh,meshPath);
            body.sharedMesh=mesh;
            var undercoat=MakeMaterial("Undercoat",1); body.sharedMaterial=undercoat;

            string groomPath=Folder+"/FullBodyGroom.asset";
            var groom=AssetDatabase.LoadAssetAtPath<DancerFurAsset>(groomPath);
            if(!groom) {groom=ScriptableObject.CreateInstance<DancerFurAsset>(); AssetDatabase.CreateAsset(groom,groomPath);}
            GenerateStrands(groom,original);
            var go=new GameObject("Fur"); go.transform.SetParent(dancer.transform,false);
            var fur=go.AddComponent<DancerFur>(); fur.body=body; fur.groom=groom;
            go.GetComponent<MeshRenderer>().sharedMaterial=MakeMaterial("Long Strands",0);
            fur.volumes=MakeVolumes(body);
            var animator=dancer.GetComponent<Animator>(); var clip=animator.runtimeAnimatorController.animationClips[0];
            clip.SampleAnimation(dancer,0); fur.ResetSimulation();
            var preview=Object.Instantiate(fur.GeneratedMesh); preview.name="Fur preview";
            string previewPath=Folder+"/FurPreview.asset"; var oldPreview=AssetDatabase.LoadAssetAtPath<Mesh>(previewPath);
            if(oldPreview) {EditorUtility.CopySerialized(preview,oldPreview); Object.DestroyImmediate(preview); preview=oldPreview;} else AssetDatabase.CreateAsset(preview,previewPath);
            fur.previewMesh=preview; go.GetComponent<MeshFilter>().sharedMesh=preview;
            PrefabUtility.SaveAsPrefabAsset(dancer,Folder+"/FurryDancer.prefab");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            VerifyAndRender(dancer,fur,clip);
            Debug.Log("FUR_COMPLETE strands="+groom.strands.Length+" triangles="+fur.TriangleCount);
            fur.ReleaseSimulation(); go.GetComponent<MeshFilter>().sharedMesh=preview;
        }

        public static void GenerateStrands(DancerFurAsset groom,Mesh original)
        {
            groom.ClampSettings();
            var points=original.vertices; var normals=original.normals; var triangles=original.triangles;
            var distance=SurfaceDistances(points,normals,triangles);
            var cumulative=new List<float>(); var eligible=new List<int>(); float total=0;
            for(int t=0;t<triangles.Length;t+=3)
            {
                int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
                var center=(points[a]+points[b]+points[c])/3; var normal=(normals[a]+normals[b]+normals[c]).normalized;
                if(Bare(center,normal)) continue;
                float area=Vector3.Cross(points[b]-points[a],points[c]-points[a]).magnitude*.5f;
                total+=area; cumulative.Add(total); eligible.Add(t);
            }
            // Long strands define the silhouette; a textured undercoat fills the gaps.
            var random=new System.Random(groom.seed); var strands=new List<DancerFurAsset.Strand>();
            int count=Mathf.Clamp(groom.targetStrandCount,100,16000);
            while(strands.Count<count)
            {
                float choose=(float)random.NextDouble()*total;
                int index=cumulative.BinarySearch(choose); if(index<0) index=~index;
                int t=eligible[Mathf.Min(index,eligible.Count-1)],a=triangles[t],b=triangles[t+1],c=triangles[t+2];
                float u=Mathf.Sqrt((float)random.NextDouble()),v=(float)random.NextDouble();
                var bary=new Vector3(1-u,u*(1-v),u*v);
                var p=points[a]*bary.x+points[b]*bary.y+points[c]*bary.z;
                var n=(normals[a]*bary.x+normals[b]*bary.y+normals[c]*bary.z).normalized;
                if(Bare(p,n)) continue;
                float d=distance[a]*bary.x+distance[b]*bary.y+distance[c]*bary.z;
                float length=Mathf.Lerp(groom.minimumLength,groom.maximumLength,Smooth(d/1.15f)) * Mathf.Lerp(1-groom.lengthVariation,1,(float)random.NextDouble());
                length=Mathf.Clamp(length,groom.minimumLength,groom.maximumLength);
                strands.Add(new DancerFurAsset.Strand {a=a,b=b,c=c,barycentric=bary,length=length,
                    diameter=Mathf.Lerp(groom.minimumDiameter,groom.maximumDiameter,Mathf.InverseLerp(groom.minimumLength,groom.maximumLength,length)),
                    segments=Mathf.Clamp(Mathf.CeilToInt(length/groom.segmentLength),groom.minimumSegments,groom.maximumSegments),
                    curl=Mathf.Lerp(-groom.curlAmount,groom.curlAmount,(float)random.NextDouble()),shade=(float)random.NextDouble()});
            }
            groom.strands=strands.ToArray(); EditorUtility.SetDirty(groom);
        }

        // Inspector entry point: only update the selected fur, never open/rebuild Scene3.
        public static void ApplySelected(DancerFur fur)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply fur outside Play mode.");
            if(!fur.body || !fur.groom) throw new InvalidOperationException("Assign a body and groom first.");
            Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Apply fur settings");
            Undo.RegisterCompleteObjectUndo(fur.groom,"Regenerate strands");
            Undo.RecordObject(fur,"Update fur preview"); Undo.RecordObject(fur.GetComponent<MeshFilter>(),"Update fur preview");
            GenerateStrands(fur.groom,fur.body.sharedMesh);
            fur.ResetSimulation();
            var preview=fur.previewMesh;
            if(!preview || !EditorUtility.IsPersistent(preview))
            {
                string folder=Path.GetDirectoryName(AssetDatabase.GetAssetPath(fur.groom)).Replace('\\','/');
                string path=AssetDatabase.GenerateUniqueAssetPath(folder+"/"+fur.groom.name+" Preview.asset");
                preview=Object.Instantiate(fur.GeneratedMesh); preview.name="Fur preview"; AssetDatabase.CreateAsset(preview,path);
            }
            else {Undo.RegisterCompleteObjectUndo(preview,"Update fur geometry"); EditorUtility.CopySerialized(fur.GeneratedMesh,preview);}
            preview.hideFlags=HideFlags.None; fur.previewMesh=preview;
            fur.ReleaseSimulation(); fur.GetComponent<MeshFilter>().sharedMesh=preview;
            var material=fur.body.sharedMaterial;
            if(material && material.HasProperty("_Undercoat"))
            {
                Undo.RecordObject(material,"Update coat colors"); material.SetColor("_BaseColor",fur.groom.undercoatColor);
                material.SetColor("_SkinColor",fur.groom.bareSkinColor); EditorUtility.SetDirty(material);
                fur.body.SetPropertyBlock(null);
            }
            EditorUtility.SetDirty(preview); EditorUtility.SetDirty(fur);
            PrefabUtility.RecordPrefabInstancePropertyModifications(fur);
            PrefabUtility.RecordPrefabInstancePropertyModifications(fur.GetComponent<MeshFilter>());
            if(fur.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(fur.gameObject.scene);
            Undo.CollapseUndoOperations(group);
            SceneView.RepaintAll();
        }
        static Material MakeMaterial(string name,float undercoat)
        {
            string path=Folder+"/"+name+".mat"; var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat) {mat=new Material(Shader.Find("VR Animation Template/Dancer Fur")); AssetDatabase.CreateAsset(mat,path);}
            mat.SetFloat("_Undercoat",undercoat); EditorUtility.SetDirty(mat); return mat;
        }
        public static void VerifySaved()
        {
            EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
            var dancer=GameObject.Find("Dancing Fat Man"); var fur=dancer.GetComponentInChildren<DancerFur>();
            if(!fur || !fur.groom || !fur.body) throw new Exception("Saved fur references are incomplete.");
            var clip=dancer.GetComponent<Animator>().runtimeAnimatorController.animationClips[0];
            try {VerifyAndRender(dancer,fur,clip); Debug.Log("SAVED_FUR_VERIFIED");}
            finally {fur.ReleaseSimulation();}
        }
        static float[] SurfaceDistances(Vector3[] p,Vector3[] normals,int[] triangles)
        {
            var adjacency=new HashSet<int>[p.Length]; for(int i=0;i<p.Length;i++) adjacency[i]=new HashSet<int>();
            for(int t=0;t<triangles.Length;t+=3) for(int e=0;e<3;e++)
            {int a=triangles[t+e],b=triangles[t+(e+1)%3]; adjacency[a].Add(b); adjacency[b].Add(a);}
            // Weld adjacency across UV/normal splits without altering render topology.
            var first=new Dictionary<Vector3Int,int>();
            for(int i=0;i<p.Length;i++)
            {
                var key=new Vector3Int(Mathf.RoundToInt(p[i].x*100000),Mathf.RoundToInt(p[i].y*100000),Mathf.RoundToInt(p[i].z*100000));
                if(first.TryGetValue(key,out int j)) {adjacency[i].Add(j); adjacency[j].Add(i);} else first[key]=i;
            }
            var distance=Enumerable.Repeat(float.PositiveInfinity,p.Length).ToArray();
            var queue=new SortedSet<(float distance,int vertex)>();
            for(int i=0;i<p.Length;i++) if(Bare(p[i],normals[i])) {distance[i]=0;queue.Add((0,i));}
            if(queue.Count==0) throw new Exception("No palm/sole seeds found. Check imported mesh units.");
            while(queue.Count>0)
            {
                var item=queue.Min; queue.Remove(item);
                foreach(int j in adjacency[item.vertex])
                {
                    float next=item.distance+Vector3.Distance(p[item.vertex],p[j]);
                    if(next<distance[j]) {queue.Remove((distance[j],j));distance[j]=next;queue.Add((next,j));}
                }
            }
            if(distance.Any(float.IsInfinity)) throw new Exception("Disconnected body surface in geodesic groom.");
            return distance;
        }
        static DancerFur.Volume[] MakeVolumes(SkinnedMeshRenderer body)
        {
            var list=new List<DancerFur.Volume>();
            void Add(string joint,Vector3 center,Vector3 radii)
            {
                int i=Array.FindIndex(body.bones,b=>b.name=="Dance_"+joint+"_JNT");
                if(i<0) throw new Exception("Missing fur collision bone "+joint);
                list.Add(new DancerFur.Volume {bone=body.bones[i],bindPose=body.sharedMesh.bindposes[i],center=center,radii=radii});
            }
            Add("Head",new Vector3(0,1.81f,.025f),new Vector3(.145f,.19f,.13f));
            Add("Chest",new Vector3(0,1.40f,-.035f),new Vector3(.245f,.22f,.18f));
            Add("Hips",new Vector3(0,.95f,0),new Vector3(.27f,.18f,.18f));
            Add("BellyJiggle",new Vector3(0,1.17f,.15f),new Vector3(.32f,.29f,.28f));
            foreach(int sign in new[]{-1,1})
            {
                // Unity FBX import reflects the X axis: anatomical left has negative X.
                string side=sign<0?"L_":"R_";
                Add(side+"UpperArm",new Vector3(sign*.36f,1.44f,0),new Vector3(.12f,.14f,.115f));
                Add(side+"Forearm",new Vector3(sign*.55f,1.125f,0),new Vector3(.075f,.14f,.075f));
                Add(side+"Thigh",new Vector3(sign*.19f,.775f,0),new Vector3(.13f,.205f,.13f));
                Add(side+"Shin",new Vector3(sign*.205f,.335f,0),new Vector3(.087f,.17f,.085f));
            }
            return list.ToArray();
        }
        static void VerifyAndRender(GameObject dancer,DancerFur fur,AnimationClip clip)
        {
            var clock=new System.Diagnostics.Stopwatch(); double total=0,max=0;
            fur.ResetSimulation();
            for(int f=0;f<=448;f++)
            {
                clip.SampleAnimation(dancer,f/24f); clock.Restart(); fur.Step(1f/24);
                clock.Stop(); total+=clock.Elapsed.TotalMilliseconds; max=Math.Max(max,clock.Elapsed.TotalMilliseconds);
                if(!fur.IsFinite()) throw new Exception("Unstable fur at frame "+f);
                if(new[]{0,148,261,322}.Contains(f)) Render(Camera.main,dancer,Reports+"/Fur-frame"+(f+1)+".png");
            }
            File.WriteAllText(Reports+"/validation.txt","Strands: "+fur.groom.strands.Length+"\nTriangles: "+fur.TriangleCount+"\nFrames: 449\nMean CPU step ms (editor): "+(total/449)+"\nMax CPU step ms: "+max+"\nCollision: fitted ellipsoids and local root surface planes; not exact mesh collision.\n");
        }
        static void Render(Camera camera,GameObject dancer,string path)
        {
            var skins=dancer.GetComponentsInChildren<SkinnedMeshRenderer>(); var snapshots=new List<GameObject>();
            foreach(var skin in skins)
            {
                var mesh=new Mesh(); skin.BakeMesh(mesh); var go=new GameObject("Preview snapshot"); go.transform.SetParent(skin.transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                skin.enabled=false; snapshots.Add(go);
            }
            var target=RenderTexture.GetTemporary(1280,960,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Default,4); var old=camera.targetTexture; var active=RenderTexture.active;
            camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            var image=new Texture2D(1280,960,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1280,960),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG()); Object.DestroyImmediate(image);
            camera.targetTexture=old; RenderTexture.active=active; RenderTexture.ReleaseTemporary(target);
            foreach(var go in snapshots) {Object.DestroyImmediate(go.GetComponent<MeshFilter>().sharedMesh); Object.DestroyImmediate(go);}
            foreach(var skin in skins) skin.enabled=true;
        }
    }
}
