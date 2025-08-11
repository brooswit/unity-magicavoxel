using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoxelDefinition))]
public class VoxelDefinitionEditor : Editor
{
    private SerializedProperty _smoothProp;
    private SerializedProperty _smoothEpsilonProp;
    private SerializedProperty _smoothStrengthProp;

    private static bool _showAdvanced = false;

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
                _smoothEpsilonProp.floatValue = EditorGUILayout.Slider("Smooth Epsilon", _smoothEpsilonProp.floatValue, 0f, 1f);
            }
            if (_smoothStrengthProp != null)
            {
                _smoothStrengthProp.floatValue = EditorGUILayout.Slider("Smooth Strength", _smoothStrengthProp.floatValue, 0f, 1f);
            }
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}


