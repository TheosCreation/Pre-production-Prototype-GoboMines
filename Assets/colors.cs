using UnityEngine;

public class ColorSpectrum : MonoBehaviour
{
    public Color[] colorVector; // Array to store the generated colors
    public int colorCount = 256; // Number of colors to generate
    [Range(0f, 1f)] public float achromaticRatio = 0.2f; // Ratio of achromatic colors

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
    }
}