using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class UiButton
{
    public Button button;
    [SerializeField]
    private UnityEvent onClick;
    [SerializeField]
    private SingletonEvent singletonEvent;

    public void Init()
    {
        if (button == null)
        {
            Debug.LogWarning("UiButton: Button is not assigned.");
            return;
        }

        // Clear existing listeners
        button.onClick.RemoveAllListeners();

        // Bind the singletonEvent if it is assigned
        if(singletonEvent != null && !string.IsNullOrEmpty(singletonEvent.SingletonTypeName) && !string.IsNullOrEmpty(singletonEvent.MethodName))
        {
            singletonEvent.BindToMethod();
            button.onClick.AddListener(() => singletonEvent.Invoke());
        }

        // Bind the onClick event
        button.onClick.AddListener(() => onClick?.Invoke());
    }

    public void CleanUp()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }
}

[Serializable]
public class SingletonEvent : UnityEvent
{
    [SerializeField]
    private string m_SingletonTypeName; // Stores the type name of the selected singleton

    [SerializeField]
    private string m_MethodName; // Stores the name of the selected method

    [SerializeField]
    private List<ParameterValue> m_ParameterValues; // Stores parameter values for the method

    private static List<object> _cachedSingletons;

    [Serializable]
    private class ParameterValue
    {
        public string typeName; // Type of the parameter
        public UnityEngine.Object objectValue; // For Object references
        public string stringValue; // For strings
        public int intValue; // For integers
        public float floatValue; // For floats
        public bool boolValue; // For booleans
    }
    public string SingletonTypeName => m_SingletonTypeName;
    public string MethodName => m_MethodName;

    public void BindToMethod()
    {
        if (string.IsNullOrEmpty(m_SingletonTypeName) || string.IsNullOrEmpty(m_MethodName))
        {
            Debug.LogWarning("SingletonEvent: Singleton type or method name is not assigned.");
            return;
        }

        // Find the singleton instance
        var singleton = _cachedSingletons?.FirstOrDefault(s => s.GetType().FullName == m_SingletonTypeName);
        if (singleton == null)
        {
            Debug.LogWarning($"SingletonEvent: Singleton of type '{m_SingletonTypeName}' not found.");
            return;
        }

        // Find the method
        var method = singleton.GetType().GetMethod(m_MethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        if (method == null)
        {
            Debug.LogWarning($"SingletonEvent: Method '{m_MethodName}' not found in singleton '{m_SingletonTypeName}'.");
            return;
        }

        // Prepare parameters
        var parameters = method.GetParameters();
        if (m_ParameterValues == null || m_ParameterValues.Count != parameters.Length)
        {
            Debug.LogWarning($"SingletonEvent: Parameter count mismatch for method '{m_MethodName}'.");
            return;
        }

        // Convert parameter values to the correct types
        var parameterValues = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var parameterValue = m_ParameterValues[i];

            if (parameter.ParameterType == typeof(string))
            {
                parameterValues[i] = parameterValue.stringValue;
            }
            else if (parameter.ParameterType == typeof(int))
            {
                parameterValues[i] = parameterValue.intValue;
            }
            else if (parameter.ParameterType == typeof(float))
            {
                parameterValues[i] = parameterValue.floatValue;
            }
            else if (parameter.ParameterType == typeof(bool))
            {
                parameterValues[i] = parameterValue.boolValue;
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(parameter.ParameterType))
            {
                parameterValues[i] = parameterValue.objectValue;
            }
            else
            {
                Debug.LogWarning($"SingletonEvent: Unsupported parameter type '{parameter.ParameterType}' for method '{m_MethodName}'.");
                return;
            }
        }

        // Bind the method to the event
        RemoveAllListeners();
        AddListener(() => method.Invoke(singleton, parameterValues));
        //Debug.Log($"SingletonEvent: Successfully bound method '{m_MethodName}' from singleton '{m_SingletonTypeName}'.");
    }

    public static void CacheSingletons()
    {
        _cachedSingletons = new List<object>();

        // Get all MonoBehaviours in the scene
        var allBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        //Debug.Log($"Found {allBehaviours.Length} MonoBehaviours in the scene.");

        // Find singletons (any MonoBehaviour with a static 'Instance' property)
        foreach (var behaviour in allBehaviours)
        {
            //Debug.Log($"Checking MonoBehaviour: {behaviour.GetType().Name}");
            var type = behaviour.GetType();
            // Look for the static 'Instance' property on the type or we look for Singleton
            PropertyInfo singletonProperty = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy); 
            if(singletonProperty == null) singletonProperty = type.GetProperty("Singleton", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            if (singletonProperty != null)
            {
                //Debug.Log($"Found Instance property in {behaviour.GetType().Name}");

                // Get the value of the Instance property
                var instanceValue = singletonProperty.GetValue(null);
                if (instanceValue != null)
                {
                    //Debug.Log($"Instance value: {instanceValue}");
                    _cachedSingletons.Add(instanceValue);
                }
                else
                {
                    //Debug.Log($"Instance value is null for {behaviour.GetType().Name}");
                }
            }
            else
            {
                //Debug.Log($"No Instance property found in {behaviour.GetType().Name}");
            }
        }

        //Debug.Log($"Found {_cachedSingletons.Count} singletons in the scene.");
    }

    public static List<object> GetCachedSingletons()
    {
        if (_cachedSingletons == null)
        {
            CacheSingletons();
        }
        return _cachedSingletons;
    }

    public static List<string> GetSingletonMethodNames(object singleton)
    {
        if (singleton == null)
        {
            return new List<string>();
        }

        // Get the type of the singleton
        var type = singleton.GetType();

        // Get all public instance methods declared in the singleton class itself
        var methods = type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m =>
                !m.Name.StartsWith("get_") && // Exclude property getters
                !m.Name.StartsWith("set_") && // Exclude property setters
                !m.IsSpecialName // Exclude special methods (e.g., property getters/setters)
            )
            .Select(m => m.Name)
            .ToList();

        return methods;
    }

    public static List<ParameterInfo> GetMethodParameters(object singleton, string methodName)
    {
        if (singleton == null || string.IsNullOrEmpty(methodName))
        {
            return new List<ParameterInfo>();
        }

        // Get the method
        var method = singleton.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        if (method == null)
        {
            return new List<ParameterInfo>();
        }

        // Return the method's parameters
        return method.GetParameters().ToList();
    }
}