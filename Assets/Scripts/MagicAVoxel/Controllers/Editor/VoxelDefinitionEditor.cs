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
                EditorGUILayout.PropertyField(_smoothEpsilonProp, new GUIContent("Smooth Epsilon"));
            }
            if (_smoothStrengthProp != null)
            {
                EditorGUILayout.PropertyField(_smoothStrengthProp, new GUIContent("Smooth Strength"));
            }
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}


