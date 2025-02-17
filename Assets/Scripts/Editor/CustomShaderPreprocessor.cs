using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using System.Collections.Generic;
using UnityEngine.Rendering;

class CustomShaderPreprocessor : IPreprocessShaders
{
    public int callbackOrder => 0;

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
    {
        for (int i = shaderCompilerData.Count - 1; i >= 0; i--)
        {
            var data = shaderCompilerData[i];
            // Remove unwanted shader variants here
            if (shader.name == "Lpk/LightModel/ToonLightBase")
            {
                // Example: Remove variants with shadows disabled
                if (!data.shaderKeywordSet.IsEnabled(new ShaderKeyword("_SHADOWS_SOFT")))
                {
                    shaderCompilerData.RemoveAt(i);
                }
            }
        }
    }
}
