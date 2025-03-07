using UnityEngine;
using UnityEngine.Audio;

public class AmbientSoundPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] audioClips;

    AudioSource audioSource;

    private float lastPlayedTime = 0f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        float timeOfDay = GameManager.Instance.timeOfDay;

        // Check if 20-second intervals have passed since the last trigger
        if (timeOfDay >= lastPlayedTime + 20f)
        {
            lastPlayedTime = timeOfDay; // Update the last trigger time

            if (UnityEngine.Random.Range(0, 100) <= 30)
            {
                // Play a random sound
                audioSource.clip = audioClips[UnityEngine.Random.Range(0, audioClips.Length)];
                audioSource.Play();
            }
        }
    }
}
