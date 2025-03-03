using UnityEngine;
using System.Collections;

public class FlickerLight : MonoBehaviour
{
    private Light flickerLight;
    [SerializeField] private float minTimeBetweenFlickers = 2f; // Minimum time between flickers
    [SerializeField] private float maxTimeBetweenFlickers = 5f; // Maximum time between flickers
    [SerializeField] private float flickerDuration = 0.1f; // How long the flicker lasts
    [SerializeField] private float fadeSpeed = 10f; // Speed of fading effect

    private float originalIntensity;

    private void Start()
    {
        flickerLight = GetComponent<Light>();
        if (flickerLight == null)
        {
            Debug.LogError("No Light component found on this GameObject!");
            enabled = false;
            return;
        }

        originalIntensity = flickerLight.intensity; // Store the original intensity
        StartCoroutine(FlickerRoutine());
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minTimeBetweenFlickers, maxTimeBetweenFlickers));

            // Fade out
            yield return StartCoroutine(FadeLightIntensity(0f, flickerDuration * 0.5f));

            // Pause at 0 intensity
            yield return new WaitForSeconds(flickerDuration * 0.5f);

            // Fade back in
            yield return StartCoroutine(FadeLightIntensity(originalIntensity, flickerDuration * 0.5f));
        }
    }

    private IEnumerator FadeLightIntensity(float targetIntensity, float duration)
    {
        float startIntensity = flickerLight.intensity;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            flickerLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, time / duration);
            yield return null;
        }

        flickerLight.intensity = targetIntensity; // Ensure it reaches the exact target
    }
}
