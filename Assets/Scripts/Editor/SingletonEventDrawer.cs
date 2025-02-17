using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SingletonEvent))]
public class SingletonEventDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get the serialized properties for the fields in SingletonEvent
        var singletonTypeNameProperty = property.FindPropertyRelative("m_SingletonTypeName");
        var methodNameProperty = property.FindPropertyRelative("m_MethodName");
        var parameterValuesProperty = property.FindPropertyRelative("m_ParameterValues");

        // Get the cached singletons
        var singletons = SingletonEvent.GetCachedSingletons();

        // Add a "None" option to the singleton dropdown
        var singletonNames = new List<string> { "None" };
        singletonNames.AddRange(singletons.Select(s => s.GetType().Name));

        // Draw the singleton dropdown
        int selectedSingletonIndex = Mathf.Max(0, singletons.FindIndex(s => s.GetType().FullName == singletonTypeNameProperty.stringValue) + 1);
        selectedSingletonIndex = EditorGUI.Popup(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            "Singleton",
            selectedSingletonIndex,
            singletonNames.ToArray()
        );

        if (selectedSingletonIndex == 0)
        {
            // "None" is selected
            singletonTypeNameProperty.stringValue = string.Empty;
            methodNameProperty.stringValue = string.Empty;
            parameterValuesProperty.ClearArray();
        }
        else if (selectedSingletonIndex - 1 < singletons.Count)
        {
            var selectedSingleton = singletons[selectedSingletonIndex - 1];
            singletonTypeNameProperty.stringValue = selectedSingleton.GetType().FullName;

            // Draw the method dropdown
            var methodNames = SingletonEvent.GetSingletonMethodNames(selectedSingleton);
            var methodNamesWithNone = new List<string> { "None" };
            methodNamesWithNone.AddRange(methodNames);

            int selectedMethodIndex = Mathf.Max(0, methodNames.IndexOf(methodNameProperty.stringValue) + 1);
            selectedMethodIndex = EditorGUI.Popup(
                new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight),
                "Method",
                selectedMethodIndex,
                methodNamesWithNone.ToArray()
            );

            if (selectedMethodIndex == 0)
            {
                // "None" is selected for the method
                methodNameProperty.stringValue = string.Empty;
                parameterValuesProperty.ClearArray();
            }
            else if (selectedMethodIndex - 1 < methodNames.Count)
            {
                methodNameProperty.stringValue = methodNames[selectedMethodIndex - 1];

                // Draw parameter fields
                var parameters = SingletonEvent.GetMethodParameters(selectedSingleton, methodNameProperty.stringValue);
                if (parameters.Count > 0)
                {
                    if (parameterValuesProperty.arraySize != parameters.Count)
                    {
                        parameterValuesProperty.ClearArray();
                        for (int i = 0; i < parameters.Count; i++)
                        {
                            parameterValuesProperty.InsertArrayElementAtIndex(i);
                            var parameterValue = parameterValuesProperty.GetArrayElementAtIndex(i);
                            parameterValue.FindPropertyRelative("typeName").stringValue = parameters[i].ParameterType.FullName;
                        }
                    }

                    for (int i = 0; i < parameters.Count; i++)
                    {
                        var parameter = parameters[i];
                        var parameterValue = parameterValuesProperty.GetArrayElementAtIndex(i);

                        EditorGUI.LabelField(
                            new Rect(position.x, position.y + (i + 2) * (EditorGUIUtility.singleLineHeight + 2), position.width, EditorGUIUtility.singleLineHeight),
                            parameter.Name
                        );

                        if (parameter.ParameterType == typeof(string))
                        {
                            parameterValue.FindPropertyRelative("stringValue").stringValue = EditorGUI.TextField(
                                new Rect(position.x + 100, position.y + (i + 2) * (EditorGUIUtility.singleLineHeight + 2), position.width - 100, EditorGUIUtility.singleLineHeight),
                                parameterValue.FindPropertyRelative("stringValue").stringValue
                            );
                        }
                        else if (parameter.ParameterType == typeof(int))
                        {
                            parameterValue.FindPropertyRelative("intValue").intValue = EditorGUI.IntField(
                                new Rect(position.x + 100, position.y + (i + 2) * (EditorGUIUtility.singleLineHeight + 2), position.width - 100, EditorGUIUtility.singleLineHeight),
                                parameterValue.FindPropertyRelative("intValue").intValue
                            );
                        }
                        else if (parameter.ParameterType == typeof(float))
                        {
                            parameterValue.FindPropertyRelative("floatValue").floatValue = EditorGUI.FloatField(
                                new Rect(position.x + 100, position.y + (i + 2) * (EditorGUIUtility.singleLineHeight + 2), position.width - 100, EditorGUIUtility.singleLineHeight),
                                parameterValue.FindPropertyRelative("floatValue").floatValue
                            );
                        }
                        else if (parameter.ParameterType == typeof(bool))
                        {
                            parameterValue.FindPropertyRelative("boolValue").boolValue = EditorGUI.Toggle(
                                new Rect(position.x + 100, position.y + (i + 2) * (EditorGUIUtility.singleLineHeight + 2), position.width - 100, EditorGUIUtility.singleLineHeight),
                                parameterValue.FindPropertyRelative("boolValue").boolValue
                            );
                        }
                        else if (typeof(UnityEngine.Object).IsAssignableFrom(parameter.ParameterType))
                        {
                            parameterValue.FindPropertyRelative("objectValue").objectReferenceValue = EditorGUI.ObjectField(
                                new Rect(position.x + 100, position.y + (i + 2) * (EditorGUIUtility.singleLineHeight + 2), position.width - 100, EditorGUIUtility.singleLineHeight),
                                parameterValue.FindPropertyRelative("objectValue").objectReferenceValue,
                                parameter.ParameterType,
                                true
                            );
                        }
                    }
                }
                else
                {
                    parameterValuesProperty.ClearArray();
                }
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var parameterValuesProperty = property.FindPropertyRelative("m_ParameterValues");
        return EditorGUIUtility.singleLineHeight * (2 + parameterValuesProperty.arraySize) + 4; // Height for dropdowns and parameter fields
    }
}