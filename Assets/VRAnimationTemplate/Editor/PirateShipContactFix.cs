using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRAnimationTemplate.Editor
{
    public static class PirateShipContactFix
    {
        const string Folder="Assets/VRAnimationTemplate/OceanStorm/PirateShip";
        public static Mesh BowCap()
        {
            var vertices=new List<Vector3>{new Vector3(0,.8f,12)};
            for(int k=0;k<10;k++){float a=k/9f*Mathf.PI;vertices.Add(new Vector3(Mathf.Cos(a)*.25f,3.45f-Mathf.Sin(a)*5.25f,12));}
            var triangles=new List<int>();
            for(int k=0;k<10;k++)triangles.AddRange(new[]{0,k+1,(k+1)%10+1});
            var mesh=new Mesh{name="Closed bow end"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
            var uv=new Vector2[vertices.Count];for(int k=0;k<uv.Length;k++)uv[k]=new Vector2(vertices[k].x,vertices[k].y);mesh.uv=uv;
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode before applying the persistent repair.");
            var ship=Object.FindFirstObjectByType<PirateShipMotion>();
            if(!ship)throw new System.InvalidOperationException("Open Scene4 with its existing ship.");
            if(!ship.transform.Find("Closed bow end"))
            {
                var mesh=BowCap();AssetDatabase.CreateAsset(mesh,Folder+"/BowCap.asset");
                var cap=new GameObject("Closed bow end");cap.transform.SetParent(ship.transform,false);
                cap.AddComponent<MeshFilter>().sharedMesh=mesh;
                cap.AddComponent<MeshRenderer>().sharedMaterial=ship.transform.Find("Wood").GetComponent<Renderer>().sharedMaterial;
            }
            if(!ship.hullFoam)
            {
                var obj=new GameObject("Hull contact foam");obj.transform.SetParent(ship.transform,false);
                var foam=obj.AddComponent<ParticleSystem>();foam.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=foam.main;main.playOnAwake=true;main.loop=true;main.maxParticles=512;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;main.gravityModifier=0;
                var emission=foam.emission;emission.enabled=false;var shape=foam.shape;shape.enabled=false;
                var size=foam.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.5f,1,1.6f));
                var fade=foam.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(0,1)});fade.color=gradient;
                var renderer=foam.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.HorizontalBillboard;
                var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/HullFoam.mat");
                if(!material){material=new Material(Shader.Find("VRAnimationTemplate/HullFoam"));AssetDatabase.CreateAsset(material,Folder+"/HullFoam.mat");}
                renderer.sharedMaterial=material;ship.hullFoam=foam;
            }
            var sprayMain=ship.bowSpray.main;sprayMain.maxParticles=512;sprayMain.simulationSpace=ParticleSystemSimulationSpace.World;
            var sprayRenderer=ship.bowSpray.GetComponent<ParticleSystemRenderer>();
            sprayRenderer.renderMode=ParticleSystemRenderMode.Billboard;
            var spraySize=ship.bowSpray.sizeOverLifetime;spraySize.enabled=true;spraySize.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,.2f));
            // Retain the old object/reference for compatibility, but remove continuous bubble emission.
            ship.hullFoam.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            ship.hullFoam.gameObject.SetActive(false);
            var impact=ship.GetComponent<ShipWaveImpact>();if(!impact)impact=ship.gameObject.AddComponent<ShipWaveImpact>();
            impact.droplets=ship.bowSpray;
            impact.foamMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/HullFoam.mat");impact.foamMaterial.SetFloat("_Sheet",0);
            var sheet=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/ImpactSheet.mat");
            if(!sheet){sheet=new Material(Shader.Find("VRAnimationTemplate/HullFoam"));AssetDatabase.CreateAsset(sheet,Folder+"/ImpactSheet.mat");}
            sheet.SetFloat("_Sheet",1);impact.sheetMaterial=sheet;
            EditorUtility.SetDirty(sheet);EditorUtility.SetDirty(impact.foamMaterial);EditorUtility.SetDirty(impact);
            var renderers=new List<Renderer>();foreach(var r in ship.GetComponentsInChildren<MeshRenderer>())renderers.Add(r);ship.shipRenderers=renderers.ToArray();
            EditorUtility.SetDirty(ship);EditorSceneManager.MarkSceneDirty(ship.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(ship.gameObject.scene);
            Debug.Log("SHIP_CONTACT_FIX_APPLIED: bow cap and external waterline effects; existing ship and scene retained.");
        }
        public static void Capture()
        {
            var ship=Object.FindFirstObjectByType<PirateShipMotion>();var camera=Camera.main;
            var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
            try
            {
                OceanSurfaceUpgrade.RenderPreview();System.IO.File.Copy("Artifacts/OceanStorm/ImprovedOcean.png","Artifacts/OceanStorm/ContactMain.png",true);
                camera.fieldOfView=65;
                camera.transform.position=ship.transform.TransformPoint(new Vector3(9,5,25));
                camera.transform.LookAt(ship.transform.TransformPoint(new Vector3(0,.6f,10)));
                OceanSurfaceUpgrade.RenderPreview();System.IO.File.Copy("Artifacts/OceanStorm/ImprovedOcean.png","Artifacts/OceanStorm/ContactBow.png",true);
                camera.transform.position=ship.transform.TransformPoint(new Vector3(-24,7,8));
                camera.transform.LookAt(ship.transform.TransformPoint(new Vector3(0,1,2)));
                OceanSurfaceUpgrade.RenderPreview();System.IO.File.Copy("Artifacts/OceanStorm/ImprovedOcean.png","Artifacts/OceanStorm/ContactSide.png",true);
            }
            finally{camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;}
        }
        public static void Check(){EditorSceneManager.OpenScene(OceanStormChapterBuilder.ScenePath);Apply();PirateShipCheck.Run();}
    }
}
