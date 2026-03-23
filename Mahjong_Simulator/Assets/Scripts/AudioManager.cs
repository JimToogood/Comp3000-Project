using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class AudioManager : MonoBehaviour {
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<AudioClip> backgroundMusic;

    private List<AudioClip> currentShuffle;
    private int currentIndex = 0;

    private static AudioManager instance;


    private void Awake() {
        // Ensure only one instance exists
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }

        // Allow instance to persist through scene changes
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        GenerateNewShuffle();
        StartCoroutine(LoopMusic());
    }

    private IEnumerator LoopMusic() {
        while (true) {
            audioSource.clip = currentShuffle[currentIndex];
            audioSource.Play();

            currentIndex++;
            if (currentIndex >= currentShuffle.Count) {
                GenerateNewShuffle();
            }

            // Ignores Time.timeScale (so music still plays when game is paused)
            yield return new WaitForSecondsRealtime(audioSource.clip.length);
        }
    }

    private void GenerateNewShuffle() {
        currentShuffle = new List<AudioClip>(backgroundMusic);

        // Fisher–Yates shuffle
        for (int i = currentShuffle.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);

            (currentShuffle[i], currentShuffle[j]) = (currentShuffle[j], currentShuffle[i]);
        }

        // Prevent last track of previous shuffle being the same as first track of next shuffle
        if (audioSource.clip != null && currentShuffle[0] == audioSource.clip) {
            int j = Random.Range(1, currentShuffle.Count);

            (currentShuffle[0], currentShuffle[j]) = (currentShuffle[j], currentShuffle[0]);
        }

        currentIndex = 0;
    }
}
