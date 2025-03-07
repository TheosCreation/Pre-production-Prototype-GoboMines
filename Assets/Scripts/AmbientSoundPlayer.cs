using UnityEngine;
using UnityEngine.Audio;

public class AmbientSoundPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] audioClip;

    AudioSource audioSource;


    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }


    // Update is called once per frame
    void Update()
    {
        if ((int)GameManager.Instance.timeOfDay % 20 == 1)
        {
            if (UnityEngine.Random.Range(0, 100) <= 30 )
            {
                // Play Random Sound
                audioSource.resource = audioClip[UnityEngine.Random.Range(0, audioClip.Length)];
                audioSource.Play();
            }
        }
    }
}
