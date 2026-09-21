using UnityEditor;
using UnityEngine;

namespace VRAnimationTemplate.Editor
{
    [CustomEditor(typeof(DancerFur))]
    public sealed class DancerFurEditor : UnityEditor.Editor
    {
        bool showVolumes;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using(new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("body"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("groom"));
            serializedObject.ApplyModifiedProperties();
            var fur=(DancerFur)target;
            if(fur.groom)
            {
                using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    var settings=new SerializedObject(fur.groom); settings.Update();
                    FurInspectorFields.Draw(settings); settings.ApplyModifiedProperties();
                    EditorGUILayout.HelpBox("Edit the shared groom settings, then Apply Fur Settings. This updates this coat's preview without reopening the scene. Save the scene/project to keep the changes.",MessageType.Info);
                    if(GUILayout.Button("Apply Fur Settings",GUILayout.Height(30)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        try {DancerFurBuilder.ApplySelected(fur);}
                        catch(System.Exception e) {Debug.LogException(e,fur);}
                    }
                }
                if(EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorGUILayout.HelpBox("Stop Play mode to rebuild appearance or geometry. Motion controls below can be adjusted during playback.",MessageType.Info);
                var mesh=fur.GetComponent<MeshFilter>().sharedMesh;
                if(mesh && mesh.subMeshCount>0)
                    EditorGUILayout.LabelField("Generated coat",$"{fur.groom.strands?.Length ?? 0:N0} strands / {mesh.GetIndexCount(0)/3:N0} triangles");
            }
            EditorGUILayout.Space(); EditorGUILayout.LabelField("Motion",EditorStyles.boldLabel);
            serializedObject.Update();
            foreach(string p in new[]{"inertia","stiffness","gravity","simulate","bodyCollision"}) EditorGUILayout.PropertyField(serializedObject.FindProperty(p));
            showVolumes=EditorGUILayout.Foldout(showVolumes,"Collision volumes",true);
            if(showVolumes) EditorGUILayout.PropertyField(serializedObject.FindProperty("volumes"),true);
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();
            using(new EditorGUI.DisabledScope(!fur.groom || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if(GUILayout.Button("Reset Settings to Defaults"))
                {
                    Undo.RecordObjects(new UnityEngine.Object[]{fur.groom,fur},"Reset fur settings");
                    fur.groom.ResetSettingsToDefaults();
                    fur.inertia=.9f; fur.stiffness=.5f; fur.gravity=.4f;
                    fur.simulate=true; fur.bodyCollision=true;
                    EditorUtility.SetDirty(fur.groom); EditorUtility.SetDirty(fur);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(fur);
                }
            }
        }
    }

    [CustomEditor(typeof(DancerFurAsset))]
    public sealed class DancerFurAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                serializedObject.Update(); FurInspectorFields.Draw(serializedObject); serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.HelpBox("Select the character's Fur object and click Apply Fur Settings to regenerate from these settings.",MessageType.Info);
        }
    }

    static class FurInspectorFields
    {
        static void Field(SerializedObject s,string name,string label) => EditorGUILayout.PropertyField(s.FindProperty(name),new GUIContent(label));
        static void Units(SerializedObject s,string name,string label,float factor)
        {
            var p=s.FindProperty(name); EditorGUI.BeginChangeCheck();
            float value=EditorGUILayout.FloatField(label,p.floatValue*factor);
            if(EditorGUI.EndChangeCheck()) p.floatValue=value/factor;
        }
        public static void Draw(SerializedObject s)
        {
            EditorGUILayout.Space(); EditorGUILayout.LabelField("Color",EditorStyles.boldLabel);
            Field(s,"rootColor","Root color"); Field(s,"tipColor","Tip color"); Field(s,"undercoatColor","Undercoat color");
            Field(s,"bareSkinColor","Bare skin color");
            EditorGUILayout.Space(); EditorGUILayout.LabelField("Coverage and length",EditorStyles.boldLabel);
            Field(s,"targetStrandCount","Number of strands");
            Units(s,"minimumLength","Minimum length (cm)",100); Units(s,"maximumLength","Maximum length (cm)",100);
            EditorGUILayout.LabelField("Maximum supported length: 100 cm",EditorStyles.miniLabel);
            Field(s,"lengthVariation","Length randomness"); Field(s,"curlAmount","Direction randomness");
            EditorGUILayout.Space(); EditorGUILayout.LabelField("Thickness and taper",EditorStyles.boldLabel);
            Units(s,"minimumDiameter","Minimum diameter (mm)",1000); Units(s,"maximumDiameter","Maximum diameter (mm)",1000);
            Field(s,"taper","Taper"); Field(s,"taperPower","Taper profile");
            EditorGUILayout.LabelField("Taper: 0 = constant thickness; 1 = pointed tip",EditorStyles.miniLabel);
            EditorGUILayout.Space(); EditorGUILayout.LabelField("Automatic segments",EditorStyles.boldLabel);
            Units(s,"segmentLength","Target segment length (cm)",100);
            Field(s,"minimumSegments","Minimum segments"); Field(s,"maximumSegments","Maximum segments");
            Field(s,"sides","Tube sides"); Field(s,"seed","Random seed");
            EditorGUILayout.LabelField("Segments = ceil(strand length / target), within the limits.",EditorStyles.wordWrappedMiniLabel);
        }
    }
}
