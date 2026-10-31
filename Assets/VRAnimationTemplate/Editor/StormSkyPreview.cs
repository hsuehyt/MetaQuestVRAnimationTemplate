using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRAnimationTemplate.Editor
{
    public static class StormSkyPreview
    {
        // Edit-mode rendering does not initialize OpenXR or change saved scene content.
        public static void Render()
        {
            RenderTo("Artifacts/OceanStorm/SkyReview");
        }
        public static void RenderFog()
        {
            RenderTo("Artifacts/OceanStorm/FogReview");
        }
        static void RenderTo(string directory)
        {
            EditorSceneManager.OpenScene(OceanStormChapterBuilder.ScenePath);
            var camera=Camera.main;
            var initial=camera.transform.localRotation;
            Directory.CreateDirectory(directory);
            try
            {
                foreach(int angle in new[]{0,90,180,270})
                {
                    camera.transform.localRotation=Quaternion.Euler(0,angle,0);
                    OceanSurfaceUpgrade.RenderPreview();
                    File.Copy("Artifacts/OceanStorm/ImprovedOcean.png",directory+"/Sky"+angle+".png",true);
                }
                camera.transform.localRotation=Quaternion.Euler(-80,0,0);
                OceanSurfaceUpgrade.RenderPreview();
                File.Copy("Artifacts/OceanStorm/ImprovedOcean.png",directory+"/Zenith.png",true);
                foreach(var message in ShaderUtil.GetShaderMessages(Shader.Find("VRAnimationTemplate/StormSky")))
                    if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error) throw new Exception(message.message);
                File.WriteAllText(directory+"/Validation.txt","PASS: Sky and ocean shaders rendered in four horizontal directions and overhead without reported shader compilation errors. Edit-mode validation only; no headset performance claim.");
                Debug.Log("STORM_SKY_PREVIEW_OK");
            }
            finally {camera.transform.localRotation=initial;}
        }
    }
}
