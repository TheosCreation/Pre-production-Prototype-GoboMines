using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FlashImage : MonoBehaviour
{
    [SerializeField] private float timeUp = 0.1f;
    private float timer;
    private Vector3 initialScale = new Vector3(1.0f, 1.0f, 1.0f);
    [SerializeField] private Vector3 targetScale = new Vector3(0.6f, 0.6f, 1.0f); // Target scale
    [SerializeField] private float targetImageAlpha = 1.0f;

    private Image image;
    private float initialAlpha;

    private void Start()
    {
        image = GetComponent<Image>();
        initialAlpha = image.color.a;
        initialScale = transform.localScale;
    }

    private void Update()
    {
        if (timer > 0.0f)
        {
            timer -= Time.deltaTime;

            // Calculate the new scale based on the remaining time
            float scaleRatio = Mathf.Clamp01(timer / timeUp);
            transform.localScale = Vector3.Lerp(targetScale, initialScale, scaleRatio);

            // Calculate the new alpha based on the remaining time
            float alpha = Mathf.Lerp(targetImageAlpha, initialAlpha, scaleRatio);
            Color color = image.color;
            color.a = alpha;
            image.color = color;

            if (timer <= 0.0f)
            {
                transform.localScale = initialScale; // Reset scale when deactivating
                Color initialColor = image.color;
                initialColor.a = initialAlpha; // Reset alpha to initial value
                image.color = initialColor;
                gameObject.SetActive(false);
            }
        }
    }

    public void Play()
    {
        gameObject.SetActive(true); // Ensure the object is active
        timer = timeUp; // Reset the timer
        transform.localScale = initialScale;
    }
}