using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[System.Serializable]
public class UiPage : MonoBehaviour
{
    [SerializeField] protected UiButton[] buttons;

    // Cache for MethodInfo lookups
    private readonly Dictionary<string, MethodInfo> methodCache = new Dictionary<string, MethodInfo>();
    protected MenuManager menuManager; // Parent IMenuManager reference

    protected virtual void Awake()
    {
        // Find the IMenuManager in the parent hierarchy
        menuManager = GetComponentInParent<MenuManager>();
    }

    protected virtual void OnEnable()
    {
        if (buttons == null || buttons.Length == 0) return;

        foreach (UiButton uiButton in buttons)
        {
            uiButton.Init();

            // Strip out the class name if it exists (e.g., MainMenu.)
            //int dotIndex = methodName.IndexOf('.');
            //if (dotIndex >= 0)
            //{
            //    methodName = methodName.Substring(dotIndex + 1); // Get the substring after the dot
            //}
            //
            //if (uiButton.button != null && !string.IsNullOrEmpty(methodName))
            //{
            //    // Attempt to cache or retrieve the MethodInfo
            //    if (!methodCache.TryGetValue(methodName, out MethodInfo method))
            //    {
            //        // Log to verify function name being searched
            //        //Debug.Log($"Searching for method '{methodName}'");
            //
            //        // Check UiPage for the method
            //        method = GetType().GetMethod(
            //            methodName,
            //            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy
            //        );
            //
            //        // Check IMenuManager for the method if not found in UiPage
            //        if (method == null && menuManager != null)
            //        {
            //            method = menuManager.GetType().GetMethod(
            //                methodName,
            //                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy
            //            );
            //        }
            //
            //        if (method == null && GameManager.Instance != null)
            //        {
            //            method = GameManager.Instance.GetType().GetMethod(
            //                methodName,
            //                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy
            //            );
            //        }
            //
            //        if (method == null && SessionManager.Instance != null)
            //        {
            //            method = SessionManager.Instance.GetType().GetMethod(
            //                methodName,
            //                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy
            //            );
            //        }
            //
            //        // Check if method was found
            //        if (method != null)
            //        {
            //            methodCache[methodName] = method; // Cache the result
            //        }
            //        else
            //        {
            //            Debug.LogWarning($"Method '{methodName}' not found in {GetType().Name} or {menuManager?.GetType().Name}.");
            //            continue; // Skip this button if no valid method is found
            //        }
            //    }
            //
            //    // Add the listener if the method was found
            //    try
            //    {
            //        uiButton.button.onClick.AddListener(() =>
            //        {
            //            InvokeMethod(uiButton, method);
            //        });
            //    }
            //    catch (Exception ex)
            //    {
            //        Debug.LogError($"Error setting up method '{methodName}' on {nameof(UiPage)}: {ex.Message}");
            //    }
            //}
        }
    }


    //private void InvokeMethod(UiButton uiButton, MethodInfo method)
    //{
    //    try
    //    {
    //        // Convert string parameters to the method's expected types
    //        ParameterInfo[] paramInfos = method.GetParameters();
    //        object[] methodParams = new object[paramInfos.Length];
    //
    //        for (int i = 0; i < paramInfos.Length; i++)
    //        {
    //            if (uiButton.parameters != null && uiButton.parameters.Count > i)
    //            {
    //                methodParams[i] = uiButton.parameters[i];
    //            }
    //            else
    //            {
    //                methodParams[i] = paramInfos[i].HasDefaultValue
    //                    ? paramInfos[i].DefaultValue
    //                    : GetDefault(paramInfos[i].ParameterType); // Fallback to default
    //            }
    //        }
    //
    //        // Determine the correct target object
    //        object targetObject = method.DeclaringType == typeof(GameManager) ? GameManager.Instance :
    //                              (method.DeclaringType == typeof(UiPage) ? this : menuManager);
    //
    //        if (targetObject == null)
    //        {
    //            Debug.LogError($"Target object for method '{method.Name}' is null.");
    //            return;
    //        }
    //
    //        // Invoke the method with the appropriate target and parameters
    //        method.Invoke(targetObject, methodParams);
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.LogError($"Error invoking method '{method.Name}' with parameters: {ex.Message}");
    //    }
    //
    //}

    private object GetDefault(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    protected virtual void OnDisable()
    {
        if (buttons == null || buttons.Length == 0) return;

        foreach (UiButton uiButton in buttons)
        {
            // uiButton.CleanUp();
        }

        // Clear method cache to prevent memory leaks
        methodCache.Clear();
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
}