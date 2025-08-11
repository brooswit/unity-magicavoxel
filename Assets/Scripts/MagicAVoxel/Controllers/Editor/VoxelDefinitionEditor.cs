using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VoxelDefinition))]
public class VoxelDefinitionEditor : Editor
{
    private const float RADIUS_SNAP = 0.25f;
    private const float STRENGTH_SNAP = 0.1f;
    
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
            
            if (prop.name == "smoothStrength")
            {
                // Custom slider with snapping for smoothStrength
                EditorGUI.BeginChangeCheck();
                // Always display the snapped value
                float displayValue = Mathf.Round(voxelDef.smoothStrength / STRENGTH_SNAP) * STRENGTH_SNAP;
                float newValue = EditorGUILayout.Slider(
                    new GUIContent("Smooth Strength", "Strength of normal smoothing (0=hard edges, 1=fully smooth)"),
                    displayValue, 0f, 1f);
                
                if (EditorGUI.EndChangeCheck())
                {
                    // Snap immediately
                    float snappedValue = Mathf.Round(newValue / STRENGTH_SNAP) * STRENGTH_SNAP;
                    Undo.RecordObject(voxelDef, "Change Smooth Strength");
                    voxelDef.smoothStrength = snappedValue;
                    EditorUtility.SetDirty(voxelDef);
                    // Force Scene view repaint to reflect normal changes immediately in edit mode
                    #if UNITY_EDITOR
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    #endif
                }
            }
            else if (prop.name == "smoothGroupRadius")
            {
                // Custom slider with snapping for smoothGroupRadius
                EditorGUI.BeginChangeCheck();
                // Always display the snapped value
                float displayValue = Mathf.Round(voxelDef.smoothGroupRadius / RADIUS_SNAP) * RADIUS_SNAP;
                float newValue = EditorGUILayout.Slider(
                    new GUIContent("Normal Blur Radius (voxels)", "Radius in voxel units for blurring/averaging normals across nearby voxels (0=exact match, 0.5=half voxel radius, etc)"),
                    displayValue, 0f, 5f);
                
                if (EditorGUI.EndChangeCheck())
                {
                    // Snap immediately
                    float snappedValue = Mathf.Round(newValue / RADIUS_SNAP) * RADIUS_SNAP;
                    Undo.RecordObject(voxelDef, "Change Smooth Group Radius");
                    voxelDef.smoothGroupRadius = snappedValue;
                    EditorUtility.SetDirty(voxelDef);
                    #if UNITY_EDITOR
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    #endif
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
