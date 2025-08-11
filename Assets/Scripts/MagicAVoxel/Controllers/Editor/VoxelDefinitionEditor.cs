using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoxelDefinition))]
public class VoxelDefinitionEditor : Editor
{
    private SerializedProperty _smoothProp;
    private SerializedProperty _smoothEpsilonProp;
    private SerializedProperty _smoothStrengthProp;

    private static bool _showAdvanced = false;
    
    // Snap increment for sliders to reduce stress/jitter
    private const float EPSILON_SNAP = 0.01f;
    private const float STRENGTH_SNAP = 0.1f;

    private void OnEnable()
    {
        _smoothProp = serializedObject.FindProperty("smooth");
        _smoothEpsilonProp = serializedObject.FindProperty("smoothEpsilon");
        _smoothStrengthProp = serializedObject.FindProperty("smoothStrength");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw default inspector for visible fields
        DrawDefaultInspector();

        EditorGUILayout.Space();
        _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true);
        if (_showAdvanced)
        {
            EditorGUI.indentLevel++;
            if (_smoothProp != null)
            {
                EditorGUILayout.PropertyField(_smoothProp, new GUIContent("Smooth"));
            }
            if (_smoothEpsilonProp != null)
            {
                float rawValue = EditorGUILayout.Slider("Smooth Epsilon", _smoothEpsilonProp.floatValue, 0f, 1f);
                _smoothEpsilonProp.floatValue = Mathf.Round(rawValue / EPSILON_SNAP) * EPSILON_SNAP;
            }
            if (_smoothStrengthProp != null)
            {
                float rawValue = EditorGUILayout.Slider("Smooth Strength", _smoothStrengthProp.floatValue, 0f, 1f);
                _smoothStrengthProp.floatValue = Mathf.Round(rawValue / STRENGTH_SNAP) * STRENGTH_SNAP;
            }
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}


