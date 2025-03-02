using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

public class NavMeshAreaMaskAttribute : PropertyAttribute
{
}

[CustomPropertyDrawer(typeof(NavMeshAreaMaskAttribute))]
public class NavMeshAreaMaskPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        int mask = property.intValue;
        string[] areaNames = NavMesh.GetAreaNames();

        if (areaNames.Length == 0)
        {
            EditorGUI.HelpBox(position, "No NavMesh areas defined! Please define areas in the Navigation window.", MessageType.Error);
            EditorGUI.EndProperty();
            return;
        }

        Rect controlRect = EditorGUI.PrefixLabel(position, label);
        Rect buttonRect = new Rect(controlRect.x, controlRect.y, controlRect.width, controlRect.height);

        if (GUI.Button(buttonRect, GetSelectedAreasText(mask, areaNames)))
        {
            ShowAreaSelectionMenu(property, areaNames);
        }

        EditorGUI.EndProperty();
    }

    private string GetSelectedAreasText(int mask, string[] areaNames)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        bool isFirst = true;

        for (int i = 0; i < areaNames.Length; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                if (!isFirst)
                    sb.Append(", ");

                sb.Append(areaNames[i]);
                isFirst = false;
            }
        }

        if (sb.Length == 0)
            sb.Append("None");

        return sb.ToString();
    }

    private void ShowAreaSelectionMenu(SerializedProperty property, string[] areaNames)
    {
        GenericMenu menu = new GenericMenu();

        for (int i = 0; i < areaNames.Length; i++)
        {
            int index = i;
            bool isSelected = (property.intValue & (1 << i)) != 0;

            menu.AddItem(new GUIContent(areaNames[i]), isSelected,
                () =>
                {
                    property.intValue ^= (1 << index);
                    property.serializedObject.ApplyModifiedProperties();
                });
        }

        menu.ShowAsContext();
    }
}