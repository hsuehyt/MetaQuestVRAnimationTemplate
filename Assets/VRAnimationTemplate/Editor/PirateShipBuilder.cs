using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace VRAnimationTemplate.Editor
{
    public static class PirateShipBuilder
    {
        const string Folder="Assets/VRAnimationTemplate/OceanStorm/PirateShip";
        static readonly Dictionary<string,List<CombineInstance>> groups=new Dictionary<string,List<CombineInstance>>();
        static readonly List<Mesh> temporary=new List<Mesh>();
        static Transform root;
        static bool replacing;
        public static void UpgradeAndCheck() { Upgrade(); PirateShipCheck.Run(); }
        [MenuItem("Tools/VR Animation Template/Upgrade Ship - Wicked Wench Style")]
        public static void Upgrade()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Artifacts/OceanStorm");
            string backup="Artifacts/OceanStorm/Scene4-before-WickedWench.unity";
            if(!File.Exists(backup)) File.Copy(OceanStormChapterBuilder.ScenePath,backup);
            replacing=true;
            try { Build(); } finally { replacing=false; }
        }
        [MenuItem("Tools/VR Animation Template/Add Pirate Ship to Scene 4")]
        public static void Build()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene=EditorSceneManager.OpenScene(OceanStormChapterBuilder.ScenePath);
            var old=GameObject.Find("Storm Pirate Ship");
            if(old && !replacing) {Debug.Log("Pirate ship already present; preserving existing ship.");return;}
            if(old) UnityEngine.Object.DestroyImmediate(old);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();groups.Clear();temporary.Clear();
            root=new GameObject("Storm Pirate Ship").transform;
            Hull();
            SternGallery();
            // Raised sterncastle, quarterdeck, companionway and gold-trimmed stern windows.
            Box("Wood",new Vector3(0,3.1f,-7.7f),new Vector3(5.3f,2.5f,4.8f));
            Box("Deck",new Vector3(0,4.4f,-7.7f),new Vector3(5.7f,.18f,5));
            for(int i=-2;i<=2;i++)Box("Gold",new Vector3(i*.9f,3.45f,-10.13f),new Vector3(.58f,.7f,.06f));
            for(int side=-1;side<=1;side+=2)
            {
                for(int j=0;j<7;j++)
                {
                    float z=-5.5f+j*1.8f;
                    Box("Dark",new Vector3(side*3.05f,1.35f,z),new Vector3(.16f,.62f,.7f));
                    Beam("Dark",new Vector3(side*2.9f,1.35f,z),new Vector3(side*3.8f,1.35f,z),.16f);
                }
                for(int j=0;j<16;j++)
                {
                    float z=-9+j*1.2f;float x=side*Width(z)*.94f;float deck=DeckHeight(z);
                    Beam("Wood",new Vector3(x,deck,z),new Vector3(x,deck+.85f,z),.045f);
                    if(j<15)
                    {
                        float nz=z+1.2f;
                        Beam("Gold",new Vector3(x,deck+.85f,z),new Vector3(side*Width(nz)*.94f,DeckHeight(nz)+.85f,nz),.055f);
                    }
                }
            }
            // Three masts with reduced canvas and bundled sails for storm conditions.
            Mast(-6,16,6.4f);Mast(0,21,10.4f);Mast(6.2f,18.2f,8.5f);
            Beam("Wood",new Vector3(0,2.6f,9.5f),new Vector3(0,5,16),.13f);
            Rope(new Vector3(0,14.6f,6.2f),new Vector3(0,5,16),.035f,0);
            Rope(new Vector3(0,18,0),new Vector3(0,15.2f,6.2f),.035f,.12f);
            Rope(new Vector3(0,14,-6),new Vector3(0,18,0),.035f,.12f);
            Sail("Flag",new Vector3(.08f,19.1f,0),2.7f,1.5f,true);
            // Foredeck capstan, deck hatch and stern wheel.
            Box("Dark",new Vector3(0,2.82f,2.8f),new Vector3(1.5f,.18f,2));
            Beam("Wood",new Vector3(0,2.7f,7.5f),new Vector3(0,3.5f,7.5f),.25f);
            for(int j=0;j<8;j++)
            {
                float a=j*Mathf.PI/4,b=(j+1)*Mathf.PI/4;
                Vector3 c=new Vector3(0,5.3f,-6.8f);
                Beam("Gold",c+new Vector3(Mathf.Cos(a)*.65f,Mathf.Sin(a)*.65f,0),c+new Vector3(Mathf.Cos(b)*.65f,Mathf.Sin(b)*.65f,0),.045f);
                Beam("Wood",c,c+new Vector3(Mathf.Cos(a)*.8f,Mathf.Sin(a)*.8f,0),.025f);
            }
            var renderers=new List<Renderer>();int triangles=0;
            foreach(var group in groups)
            {
                var mesh=new Mesh{name="Pirate ship "+group.Key,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(group.Value.ToArray(),true,true);
                mesh.RecalculateBounds();var bounds=mesh.bounds;bounds.Expand(2);mesh.bounds=bounds;
                triangles+=mesh.triangles.Length/3;
                string meshPath=Folder+"/"+group.Key+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(existing)
                {
                    // CopySerialized can leave the live mesh GPU buffers stale until reload.
                    // Reassign the channels so editor and player show the same saved geometry.
                    existing.Clear();existing.indexFormat=mesh.indexFormat;
                    existing.vertices=mesh.vertices;existing.normals=mesh.normals;existing.uv=mesh.uv;
                    if(mesh.colors.Length==mesh.vertexCount)existing.colors=mesh.colors;
                    existing.triangles=mesh.triangles;existing.bounds=mesh.bounds;
                    EditorUtility.SetDirty(existing);
                    UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;
                }
                else AssetDatabase.CreateAsset(mesh,meshPath);
                mesh.UploadMeshData(false);
                var obj=new GameObject(group.Key);obj.transform.SetParent(root,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=Material(group.Key);renderer.shadowCastingMode=ShadowCastingMode.Off;renderers.Add(renderer);
            }
            foreach(var mesh in temporary)UnityEngine.Object.DestroyImmediate(mesh);
            root.localScale=Vector3.one*1.5f;
            root.position=new Vector3(-18,0,50);root.rotation=Quaternion.Euler(0,40,0);
            var motion=root.gameObject.AddComponent<PirateShipMotion>();motion.shipRenderers=renderers.ToArray();
            var weather=UnityEngine.Object.FindFirstObjectByType<OceanStormController>();
            var spray=UnityEngine.Object.Instantiate(weather.spray,root);spray.name="Bow impact spray";
            spray.transform.localPosition=Vector3.zero;spray.transform.localRotation=Quaternion.identity;
            var main=spray.main;main.maxParticles=300;main.gravityModifier=.45f;
            motion.bowSpray=spray;
            PirateShipContactFix.Apply();
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Preview();
            if(!Application.isBatchMode) EditorApplication.delayCall+=Preview;
            File.WriteAllText("Artifacts/OceanStorm/PirateShipValidation.txt","Generated ship: "+triangles+" triangles; "+renderers.Count+" mesh renderers; three masts, reduced sails, flag, rigging, guns. Shader compilation and camera render checked. Headset performance unverified.");
            Debug.Log("PIRATE_SHIP_BUILD_OK triangles="+triangles);
        }
        static void SternGallery()
        {
            // Stepped aft galleries, glazed windows, carved crown and lanterns.
            Box("Wood",new Vector3(0,4.6f,-8.8f),new Vector3(5.35f,2.7f,3.5f));
            Box("Deck",new Vector3(0,6,-8.8f),new Vector3(5.8f,.2f,3.8f));
            for(int level=0;level<2;level++)
            {
                float y=3.5f+level*1.45f;
                Box("Gold",new Vector3(0,y-.55f,-10.62f),new Vector3(5.8f,.12f,.2f));
                Box("Gold",new Vector3(0,y+.55f,-10.62f),new Vector3(5.8f,.12f,.2f));
                for(int j=-3;j<=3;j++)
                {
                    float x=j*.74f;
                    Box("Glass",new Vector3(x,y,-10.59f),new Vector3(.55f,.9f,.12f));
                    Beam("Gold",new Vector3(x-.32f,y-.55f,-10.69f),new Vector3(x-.32f,y+.55f,-10.69f),.045f);
                    Beam("Gold",new Vector3(x,y-.45f,-10.7f),new Vector3(x,y+.45f,-10.7f),.025f);
                    Box("Gold",new Vector3(x,y,-10.7f),new Vector3(.55f,.04f,.06f));
                }
                for(int side=-1;side<=1;side+=2)
                {
                    Box("Gold",new Vector3(side*2.85f,y-.6f,-9),new Vector3(.22f,.16f,3.7f));
                    for(int j=0;j<4;j++)
                    {
                        float z=-10.2f+j*.8f;
                        Box("Glass",new Vector3(side*2.71f,y,z),new Vector3(.08f,.86f,.55f));
                        Beam("Gold",new Vector3(side*2.78f,y-.5f,z-.34f),new Vector3(side*2.78f,y+.5f,z-.34f),.04f);
                    }
                }
            }
            for(int j=-6;j<=6;j++)
            {
                float x=j*.42f;
                Beam("Gold",new Vector3(x,6.1f,-10.65f),new Vector3(x,6.7f,-10.65f),.035f);
                if(j<6) Beam("Wood",new Vector3(x,6.7f,-10.65f),new Vector3(x+.42f,6.7f,-10.65f),.065f);
            }
            for(int j=0;j<20;j++)
            {
                float a=j*Mathf.PI/20,b=(j+1)*Mathf.PI/20;
                Beam("Gold",new Vector3(Mathf.Cos(a)*2.4f,6.65f+Mathf.Sin(a)*.8f,-10.6f),new Vector3(Mathf.Cos(b)*2.4f,6.65f+Mathf.Sin(b)*.8f,-10.6f),.085f);
            }
            for(int side=-1;side<=1;side+=2)
            {
                Beam("Gold",new Vector3(side*2.65f,5.9f,-10.5f),new Vector3(side*2.65f,7.7f,-10.5f),.045f);
                Box("Gold",new Vector3(side*2.65f,7.65f,-10.5f),new Vector3(.5f,.12f,.5f));
                Box("Glass",new Vector3(side*2.65f,7.3f,-10.5f),new Vector3(.35f,.55f,.35f));
                Box("Gold",new Vector3(side*2.65f,7,-10.5f),new Vector3(.5f,.12f,.5f));
            }
        }
        static float Width(float z) => Mathf.Lerp(.25f,3.3f,Mathf.Pow(Mathf.Clamp01(1-Mathf.Pow(Mathf.Abs(z)/12,3)),.55f));
        static float DeckHeight(float z) => 2.7f+Mathf.Pow(Mathf.Abs(z)/12,3)*.75f;
        static void Hull()
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            const int stations=32,ring=10;
            for(int j=0;j<=stations;j++)
            {
                float z=Mathf.Lerp(-11,12,j/(float)stations),width=Width(z),deck=DeckHeight(z);
                for(int k=0;k<ring;k++)
                {
                    float a=k/(float)(ring-1)*Mathf.PI;
                    v.Add(new Vector3(Mathf.Cos(a)*width,deck-Mathf.Sin(a)*(deck+1.8f),z));uv.Add(new Vector2(k/(float)(ring-1),j/(float)stations));
                    if(j<stations && k<ring-1){int n=j*ring+k;t.AddRange(new[]{n,n+ring,n+1,n+1,n+ring,n+ring+1});}
                }
                if(j<stations)
                {
                    float nz=Mathf.Lerp(-11,12,(j+1)/(float)stations);
                    for(int side=-1;side<=1;side+=2)
                    {
                        Beam("Gold",new Vector3(side*width,deck-.15f,z),new Vector3(side*Width(nz),DeckHeight(nz)-.15f,nz),.065f);
                        Beam("Wood",new Vector3(side*width*.93f,1.6f,z),new Vector3(side*Width(nz)*.93f,1.6f,nz),.10f);
                    }
                    float dz=nz-z;
                    Box("Deck",new Vector3(0,deck-.08f,z+dz*.5f),new Vector3(width*1.97f,.16f,dz+.03f));
                }
            }
            Add("Wood",Mesh(v,uv,t),Matrix4x4.identity);
            Box("Wood",new Vector3(0,1.5f,-11),new Vector3(Width(-11)*2,3.4f,.2f));
        }
        static void Mast(float z,float height,float span)
        {
            Beam("Wood",new Vector3(0,2.7f,z),new Vector3(0,height,z),.15f);
            for(int level=0;level<3;level++)
            {
                float y=height-1.5f-level*4.4f;float w=span*(level==0?.55f:level==1?.8f:1);
                Beam("Wood",new Vector3(-w*.52f,y,z),new Vector3(w*.52f,y,z),.09f);
                Sail("Canvas",new Vector3(-w*.5f,y-3.5f,z),w,3.5f,false);
                Beam("Canvas",new Vector3(-w*.45f,y-3.6f,z),new Vector3(w*.45f,y-3.6f,z),.17f);
                Rope(new Vector3(-w*.5f,y-3.5f,z),new Vector3(-2.4f,2.8f,z-1),.025f,.2f);
                Rope(new Vector3(w*.5f,y-3.5f,z),new Vector3(2.4f,2.8f,z-1),.025f,.2f);
            }
            for(int side=-1;side<=1;side+=2)for(int j=0;j<3;j++)
                Rope(new Vector3(0,height-3,z),new Vector3(side*2.8f,3,z-1.5f+j*1.4f),.03f,.05f);
        }
        static void Sail(string group,Vector3 origin,float width,float height,bool flag)
        {
            const int nx=20,ny=12;var v=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
            for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++)
            {
                float u=x/(float)nx,w=y/(float)ny;float pin=flag?u:Mathf.Sin(u*Mathf.PI)*Mathf.Sin(w*Mathf.PI);
                float taper=flag?1:Mathf.Lerp(.82f,1,w);
                v.Add(origin+new Vector3(width*(.5f+(u-.5f)*taper),height*w,flag?0:pin*1.15f));uv.Add(new Vector2(u,w));colors.Add(new Color(pin,0,0,1));
                if(x<nx && y<ny){int n=y*(nx+1)+x;triangles.AddRange(new[]{n,n+1,n+nx+1,n+1,n+nx+2,n+nx+1});}
            }
            var mesh=Mesh(v,uv,triangles);mesh.SetColors(colors);Add(group,mesh,Matrix4x4.identity);
        }
        static void Rope(Vector3 a,Vector3 b,float radius,float sag)
        {
            for(int j=0;j<12;j++)
            {
                float f=j/12f,g=(j+1)/12f;
                Vector3 p=Vector3.Lerp(a,b,f)+Vector3.down*Mathf.Sin(f*Mathf.PI)*sag;
                Vector3 q=Vector3.Lerp(a,b,g)+Vector3.down*Mathf.Sin(g*Mathf.PI)*sag;
                Beam("Rope",p,q,radius,Mathf.Sin((f+g)*.5f*Mathf.PI)*.1f);
            }
        }
        static void Box(string group,Vector3 p,Vector3 size)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);var mesh=UnityEngine.Object.Instantiate(obj.GetComponent<MeshFilter>().sharedMesh);
            UnityEngine.Object.DestroyImmediate(obj);Add(group,mesh,Matrix4x4.TRS(p,Quaternion.identity,size));
        }
        static void Beam(string group,Vector3 a,Vector3 b,float radius,float wind=0)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();var colors=new List<Color>();
            Vector3 axis=(b-a).normalized;Vector3 side=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.right:Vector3.up).normalized;Vector3 other=Vector3.Cross(axis,side);
            const int sides=6;
            for(int j=0;j<=1;j++)for(int k=0;k<=sides;k++)
            {
                float angle=k*Mathf.PI*2/sides;v.Add((j==0?a:b)+(side*Mathf.Cos(angle)+other*Mathf.Sin(angle))*radius);uv.Add(new Vector2(k/(float)sides,j));colors.Add(new Color(wind,0,0,1));
                if(j==0 && k<sides){int n=k;triangles.AddRange(new[]{n,n+1,n+sides+1,n+1,n+sides+2,n+sides+1});}
            }
            var mesh=Mesh(v,uv,triangles);mesh.SetColors(colors);Add(group,mesh,Matrix4x4.identity);
        }
        static Mesh Mesh(List<Vector3> vertices,List<Vector2> uv,List<int> triangles)
        {
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
        }
        static void Add(string key,Mesh mesh,Matrix4x4 matrix)
        {
            if(!groups.ContainsKey(key))groups[key]=new List<CombineInstance>();
            groups[key].Add(new CombineInstance{mesh=mesh,transform=matrix});temporary.Add(mesh);
        }
        static Material Material(string key)
        {
            string path=Folder+"/"+key+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("VRAnimationTemplate/PirateShip"));AssetDatabase.CreateAsset(mat,path);}
            Color color=key=="Canvas"?new Color(.78f,.74f,.61f):key=="Gold"?new Color(.58f,.39f,.15f):key=="Glass"?new Color(.16f,.24f,.25f):key=="Deck"?new Color(.24f,.15f,.075f):key=="Wood"?new Color(.29f,.16f,.075f):new Color(.035f,.028f,.02f);
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Wood",key=="Wood"||key=="Deck"?1:0);mat.SetFloat("_Flag",key=="Flag"?1:0);mat.SetFloat("_Wind",key=="Canvas"?.5f:key=="Flag"?1.5f:key=="Rope"?.3f:0);EditorUtility.SetDirty(mat);return mat;
        }
        public static void Preview()
        {
            if(!Camera.main)EditorSceneManager.OpenScene(OceanStormChapterBuilder.ScenePath);
            OceanSurfaceUpgrade.RenderPreview();
            File.Copy("Artifacts/OceanStorm/ImprovedOcean.png","Artifacts/OceanStorm/PirateShip.png",true);
            foreach(var message in ShaderUtil.GetShaderMessages(Shader.Find("VRAnimationTemplate/PirateShip")))
                if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)throw new Exception(message.message);
        }
    }
}
