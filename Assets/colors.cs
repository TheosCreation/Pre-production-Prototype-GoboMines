using System.Linq;
using UnityEngine;

public class ColorSpectrum : MonoBehaviour
{
    public Color[] colorVector; // Array to store the generated colors
    public int colorCount = 256; // Number of colors to generate
    [Range(0f, 1f)] public float achromaticRatio = 0.2f; // Ratio of achromatic colors
    public Texture2D[] inputTextures; // Textures to sample colors from

    void Start()
    {
        GenerateColorSpectrum();
    }

    void GenerateColorSpectrum()
    {
        // Create an array of Color values with the specified count
        colorVector = new Color[colorCount];

        // Calculate the number of chromatic and achromatic colors
        int achromaticCount = Mathf.RoundToInt(colorCount * achromaticRatio);
        int chromaticCount = colorCount - achromaticCount;

        // Generate chromatic colors (rainbow spectrum)
        for (int i = 0; i < chromaticCount; i++)
        {
            float hue = (float)i / (chromaticCount - 1); // Normalize hue to range [0, 1]
            colorVector[i] = Color.HSVToRGB(hue, 1f, 1f); // Full saturation and value
        }

        // Generate achromatic colors (white, black, gray)
        for (int i = 0; i < achromaticCount; i++)
        {
            float value = (float)i / (achromaticCount - 1); // Normalize value to range [0, 1]
            colorVector[chromaticCount + i] = new Color(value, value, value); // Grayscale
        }

        // Sample colors from input textures and add them to the colorVector
        if (inputTextures != null && inputTextures.Length > 0)
        {
            foreach (Texture2D texture in inputTextures)
            {
                if (texture != null)
                {
                    Color[] textureColors = texture.GetPixels(); // Get all pixels from the texture
                    AddUniqueColors(textureColors); // Add unique colors to the colorVector
                }
            }
        }
    }

    void AddUniqueColors(Color[] newColors)
    {
        // Iterate through the new colors and add them to the colorVector if they are unique
        foreach (Color color in newColors)
        {
            if (!colorVector.Contains(color)) // Check if the color is already in the array
            {
                // Resize the colorVector array and add the new color
                System.Array.Resize(ref colorVector, colorVector.Length + 1);
                colorVector[colorVector.Length - 1] = color;
            }
        }
    }
}