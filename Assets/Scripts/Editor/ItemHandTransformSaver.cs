using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Item), true)]
public class ItemHandTransformSaver : UnityEditor.Editor
{
    private TransformDataStorage transformDataStorage;

    void OnEnable()
    {
        // Load or create the ScriptableObject asset
        transformDataStorage = AssetDatabase.LoadAssetAtPath<TransformDataStorage>("Assets/TransformDataStorage.asset");
        if (transformDataStorage == null)
        {
            transformDataStorage = CreateInstance<TransformDataStorage>();
            AssetDatabase.CreateAsset(transformDataStorage, "Assets/TransformDataStorage.asset");
            AssetDatabase.SaveAssets();
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // Draw Save Button
        if (GUILayout.Button("Save Local IK Transforms"))
        {
            SaveLocalTransforms();
        }

        // Draw Load Button
        if (GUILayout.Button("Load Local IK Transforms"))
        {
            LoadLocalTransforms();
        }
    }

    private void SaveLocalTransforms()
    {
        MonoBehaviour script = (MonoBehaviour)target;

        // Find the Transforms to save
        Transform[] leftHand = new Transform[]
        {
            (Transform)script.GetType().GetField("IKLeftHandPos").GetValue(script),
        };

        Transform[] rightHand = new Transform[]
        {
            (Transform)script.GetType().GetField("IKRightHandPos").GetValue(script),
        };

        transformDataStorage.SaveLocalHandTransforms(leftHand, rightHand);
        EditorUtility.SetDirty(transformDataStorage);
        Debug.Log("Local transforms saved successfully!");
    }

    private void LoadLocalTransforms()
    {
        MonoBehaviour script = (MonoBehaviour)target;

        // Find the Transforms to load
        Transform[] leftHand = new Transform[]
        {
            (Transform)script.GetType().GetField("IKLeftHandPos").GetValue(script),
        };

        Transform[] rightHand = new Transform[]
        {
            (Transform)script.GetType().GetField("IKRightHandPos").GetValue(script),
        };

        transformDataStorage.LoadLocalHandTransforms(leftHand, rightHand);
        Debug.Log("Local transforms loaded successfully!");
    }
}