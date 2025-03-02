using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EnemyAI), true)]
public class GetCurrentStateButton : UnityEditor.Editor
{
    private TransformDataStorage transformDataStorage;


    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // Draw Save Button
        if (GUILayout.Button("GetAiState"))
        {
            DebugState();
        }

    }
    private void DebugState()
    {
        MonoBehaviour script = (MonoBehaviour)target;
        string currentState = script.GetType().GetField("currentState").GetValue(script).ToString();
        Debug.Log(currentState);
    }
   
}