using UnityEngine;
using System.Collections;

public class PlayMusicForDuration : MonoBehaviour
{
    public AudioSource audioSource; // Reference to the AudioSource component
    private float totalPlayTime = 40f; // Total duration to play music
    private float elapsedTime = 0f; // Tracks time elapsed

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>(); // Get AudioSource if not assigned
        }

        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play(); // Start playing the audio
            StartCoroutine(ManageAudioPlayback()); // Start the coroutine
        }
    }

    private IEnumerator ManageAudioPlayback()
    {
        while (elapsedTime < totalPlayTime)
        {
            float remainingTime = totalPlayTime - elapsedTime;
            float clipLength = audioSource.clip.length;

            if (remainingTime < clipLength)
            {
                yield return new WaitForSeconds(remainingTime);
                audioSource.Stop();
                break;
            }
            else
            {
                yield return new WaitForSeconds(clipLength);
                elapsedTime += clipLength;
                audioSource.Play(); // Restart the track if needed
            }
        }
    }
}
