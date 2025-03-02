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
        Rect popupRect = new Rect(controlRect.x, controlRect.y, controlRect.width - 20f, controlRect.height);
        Rect buttonRect = new Rect(controlRect.x + controlRect.width - 15f, controlRect.y, 15f, controlRect.height);

        int selectedIndex = GetSelectedIndex(mask, areaNames);
        int newIndex = EditorGUI.Popup(popupRect, selectedIndex, areaNames);

        if (GUI.Button(buttonRect, "..."))
        {
            ShowAreaSelectionDialog(property, areaNames);
        }
        else if (newIndex != selectedIndex)
        {
            property.intValue = 1 << newIndex;
        }

        EditorGUI.EndProperty();
    }

    private int GetSelectedIndex(int mask, string[] areaNames)
    {
        for (int i = 0; i < areaNames.Length; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                return i;
            }
        }
        return 0;
    }

    private void ShowAreaSelectionDialog(SerializedProperty property, string[] areaNames)
    {
        GenericMenu menu = new GenericMenu();

        for (int i = 0; i < areaNames.Length; i++)
        {
            int index = i;
            menu.AddItem(new GUIContent(areaNames[i]), (property.intValue & (1 << i)) != 0,
                () => property.intValue ^= (1 << index));
        }

        menu.ShowAsContext();
    }
}