using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class AudioManager : MonoBehaviour {
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private List<AudioClip> backgroundMusic;

    private List<AudioClip> currentShuffle;
    private int currentIndex = 0;


    private void Awake() {
        // Ensure only one instance exists
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        // Allow instance to persist through scene changes
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        GenerateNewShuffle();
        StartCoroutine(LoopMusic());
    }

    public void PlayClick() {
        sfxSource.Play();
    }

    private IEnumerator LoopMusic() {
        while (true) {
            musicSource.clip = currentShuffle[currentIndex];
            musicSource.Play();

            currentIndex++;
            if (currentIndex >= currentShuffle.Count) {
                GenerateNewShuffle();
            }

            // Wait until track has finished playing (ignoring audio pause when game is unfocused)
            yield return new WaitWhile(() => musicSource.isPlaying || !Application.isFocused);
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
        if (musicSource.clip != null && currentShuffle[0] == musicSource.clip) {
            int j = Random.Range(1, currentShuffle.Count);

            (currentShuffle[0], currentShuffle[j]) = (currentShuffle[j], currentShuffle[0]);
        }

        currentIndex = 0;
    }
}
