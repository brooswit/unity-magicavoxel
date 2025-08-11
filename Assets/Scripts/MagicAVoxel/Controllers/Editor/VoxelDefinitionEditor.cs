using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VoxelDefinition))]
public class VoxelDefinitionEditor : Editor
{
    private const float RADIUS_SNAP = 0.25f;
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        VoxelDefinition voxelDef = (VoxelDefinition)target;
        
        // Draw all properties except smoothGroupRadius with default inspector
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.name == "m_Script") continue;
            
            if (prop.name == "smoothGroupRadius")
            {
                // Custom slider with snapping for smoothGroupRadius
                EditorGUI.BeginChangeCheck();
                float newValue = EditorGUILayout.Slider(
                    new GUIContent("Smooth Group Radius", "Radius in voxel units for grouping nearby vertices for smoothing (0=exact match, 0.5=half voxel radius, etc)"),
                    voxelDef.smoothGroupRadius, 0f, 5f);
                
                if (EditorGUI.EndChangeCheck())
                {
                    // Snap to nearest 0.25
                    float snappedValue = Mathf.Round(newValue / RADIUS_SNAP) * RADIUS_SNAP;
                    Undo.RecordObject(voxelDef, "Change Smooth Group Radius");
                    voxelDef.smoothGroupRadius = snappedValue;
                    EditorUtility.SetDirty(voxelDef);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(prop, true);
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
