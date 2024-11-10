using System.Collections;
using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomEditor(typeof(MonoBehaviour), true)]
public class CommonEditor : Editor
{
    private bool foldout = true;

    private void DoField(string name, System.Type type, object obj)
    {
        if (obj is Object unityObject)
        {
            EditorGUILayout.ObjectField(name, unityObject, type, true);
        }
        else if (obj is IEnumerable enumerable)
        {
            EditorGUILayout.LabelField(name);
            EditorGUI.indentLevel++;

            int index = 0;
            foreach (var element in enumerable)
            {
                if (element is Object elementUnityObject)
                {
                    EditorGUILayout.ObjectField($"{index}", elementUnityObject, element.GetType(), true);
                }
                else
                {
                    EditorGUILayout.LabelField($"{index}", element?.ToString() ?? "null");
                }
                index++;
            }

            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.LabelField(name, obj?.ToString() ?? "null");
        }
    }

    public override void OnInspectorGUI()
    {
        MonoBehaviour targetObject = (MonoBehaviour)target;

        // Draw the default inspector
        DrawDefaultInspector();

        EditorGUILayout.Separator();
        foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, new GUIContent("Debugging Display"));
        if (foldout)
        {
            // Draw each field with the DebugDisplayAttribute
            foreach (FieldInfo field in targetObject.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                DebugDisplayAttribute attr = field.GetCustomAttribute<DebugDisplayAttribute>();
                if (attr != null)
                {
                    EditorGUI.BeginDisabledGroup(!attr.editable);
                    DoField(field.Name, field.GetType(), field.GetValue(targetObject));
                    EditorGUI.EndDisabledGroup();
                }
            }

            // Draw each property with the DebugDisplayAttribute
            foreach (PropertyInfo prop in targetObject.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                DebugDisplayAttribute attr = prop.GetCustomAttribute<DebugDisplayAttribute>();
                if (attr != null)
                {
                    EditorGUI.BeginDisabledGroup(!attr.editable);
                    DoField(prop.Name, prop.GetType(), prop.GetValue(targetObject));
                    EditorGUI.EndDisabledGroup();
                }
            }

        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }
}
