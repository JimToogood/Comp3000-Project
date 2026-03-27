using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;


public class MenuManager : MonoBehaviour {
    [SerializeField] private GameObject menuUI;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject rulesMenu;
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private TMP_Text rulesText;
    [SerializeField] private TMP_Text rulesTitleText;
    
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Camera menuCamera;

    private bool gameStarted = false;
    private List<string> rulesPages = new();
    private int currentPage = 0;


    private void Start() {
        mainMenu.SetActive(true);
        pauseMenu.SetActive(false);
        rulesMenu.SetActive(false);

        menuUI.SetActive(true);
        menuCamera.enabled = true;
        gameCamera.enabled = false;

        LoadRulesTxt();

        StartCoroutine(Fade(1.0f, 0.0f));
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (rulesMenu.activeSelf) {
                BackButton();
            } else if (gameStarted) {
                if (menuUI.activeSelf) {
                    PauseMenuClose();
                } else {
                    PauseMenuOpen();
                }
            }
        }
    }

    public void PauseMenuOpen() {
        GameManager.Instance.TogglePause(true);
        menuUI.SetActive(true);
    }

    public void PauseMenuClose() {
        GameManager.Instance.TogglePause(false);
        menuUI.SetActive(false);
    }

    public void ReturnToMainMenuButton() {
        Time.timeScale = 1.0f;
        StartCoroutine(FadeAndReload());
    }

    public void QuitButton() {
        #if UNITY_EDITOR
            // If in the editor, exit play mode
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // If not in the editor, quit the application
            Application.Quit();
        #endif
    }

    public void PlayButton() {
        StartCoroutine(StartGameTransition());
    }

    public void RulesButton() {
        if (gameStarted) {
            pauseMenu.SetActive(false);
        } else {
            mainMenu.SetActive(false);
        }
        
        rulesMenu.SetActive(true);
        rulesText.text = rulesPages[currentPage];
        rulesTitleText.text = $"Rules {currentPage + 1}/{rulesPages.Count}";
    }

    public void BackButton() {
        if (gameStarted) {
            pauseMenu.SetActive(true);
        } else {
            mainMenu.SetActive(true);
        }
        
        rulesMenu.SetActive(false);
        currentPage = 0;
    }

    public void NextPageButton() {
        currentPage += 1;
        if (currentPage > rulesPages.Count - 1) { currentPage = rulesPages.Count - 1; }

        rulesText.text = rulesPages[currentPage];
        rulesTitleText.text = $"Rules {currentPage + 1}/{rulesPages.Count}";
    }

    public void PreviousPageButton() {
        currentPage -= 1;
        if (currentPage < 0) { currentPage = 0; }

        rulesText.text = rulesPages[currentPage];
        rulesTitleText.text = $"Rules {currentPage + 1}/{rulesPages.Count}";
    }

    private IEnumerator StartGameTransition() {
        // Fade out to black
        yield return StartCoroutine(Fade(0.0f, 1.0f));

        // Transition from menu to game whilst screen is black
        mainMenu.SetActive(false);
        pauseMenu.SetActive(true);

        menuUI.SetActive(false);
        menuCamera.enabled = false;
        gameCamera.enabled = true;
        GameManager.Instance.StartGame();

        // Fade in from black
        yield return StartCoroutine(Fade(1.0f, 0.0f));

        gameStarted = true;
        GameManager.Instance.TogglePause(false);
    }

    private IEnumerator FadeAndReload() {
        yield return StartCoroutine(Fade(0.0f, 1.0f));
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator Fade(float startAlpha, float endAlpha) {
        float time = 0.0f;
        float duration = 1.0f;

        fadePanel.alpha = startAlpha;
        fadePanel.blocksRaycasts = true;

        // Animate smooth fade between start alpha and end alpha
        while (time < duration) {
            float t = Mathf.SmoothStep(0.0f, 1.0f, time / duration);

            fadePanel.alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            time += Time.deltaTime;
            yield return null;
        }

        // Snap to end alpha after animation to avoid rounding errors
        fadePanel.alpha = endAlpha;
        fadePanel.blocksRaycasts = false;
    }

    private void LoadRulesTxt() {
        TextAsset textAsset = Resources.Load<TextAsset>("rule_book");

        if (textAsset == null) {
            Debug.LogError($"Failed to load 'rules.txt'");
            return;
        }

        string[] splitText = textAsset.text.Split(new[]{"-----"}, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string page in splitText) {
            rulesPages.Add(page.Trim());
        }
    }
}
