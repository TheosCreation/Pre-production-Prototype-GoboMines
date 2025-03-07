using UnityEngine;

public class PlayRandAudioSourceOnAwake : MonoBehaviour
{
    [SerializeField] private AudioClip[] audioClip;

    private void Awake()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        // Play Random Sound
        audioSource.resource = audioClip[UnityEngine.Random.Range(0, audioClip.Length)];
        audioSource.Play();
    }
}
