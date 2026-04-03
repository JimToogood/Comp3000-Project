using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;


public class MenuManager : MonoBehaviour {
    // Set instance so menu manager can be called in other classes
    public static MenuManager Instance { get; private set; }
    void Awake() { Instance = this; }

    [SerializeField] private GameObject menuUI;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject rulesMenu;
    [SerializeField] private GameObject callMenu;
    [SerializeField] private GameObject winMenu;

    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private TMP_Text rulesText;
    [SerializeField] private TMP_Text rulesTitleText;
    [SerializeField] private TMP_Text callText;
    [SerializeField] private TMP_Text playerText;
    [SerializeField] private TMP_Text winText;

    [SerializeField] private TMP_Text[] ponButtonTexts;
    [SerializeField] private TMP_Text[] chiButtonTexts;
    
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Camera menuCamera;

    private bool gameStarted = false;
    private bool callMenuOpen = false;
    private List<string> rulesPages = new();
    private int currentPage = 0;


    private void Start() {
        mainMenu.SetActive(true);
        pauseMenu.SetActive(false);
        rulesMenu.SetActive(false);
        callMenu.SetActive(false);
        winMenu.SetActive(false);

        menuUI.SetActive(true);
        playerText.enabled = false;
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
                if (menuUI.activeSelf && !callMenu.activeSelf) {
                    PauseMenuToggle(false);
                } else {
                    PauseMenuToggle(true);
                }
            }
        }

        // TODO: DELETE THIS DEBUG/TESTING INPUT 
        if (Input.GetKeyDown(KeyCode.E)) {
            if (Time.timeScale == 10.0f) {
                Time.timeScale = 1.0f;
            } else {
                Time.timeScale = 10.0f;
            }
        }
    }

    public void OpenCallMenu(string callString) {
        menuUI.SetActive(true);
        playerText.enabled = false;
        pauseMenu.SetActive(false);

        callMenu.SetActive(true);
        callMenuOpen = true;
        callText.text = callString;
    }

    private void CloseCallMenu() {
        if (gameStarted) {
            Debug.Log("Closing call menu...");
            menuUI.SetActive(false);
            playerText.enabled = true;
            pauseMenu.SetActive(true);
        }

        callMenu.SetActive(false);
        callMenuOpen = false;
    }

    public void SetPlayerText(int playerIndex) {
        playerText.text = $"Player {playerIndex}";
    }

    public void ShowWinScreen(int playerIndex) {
        if (playerIndex == -1) {
            winText.text = "It's a draw!";
        } else {
            winText.text = $"Player {playerIndex + 1} wins!";
        }

        gameStarted = false;

        menuUI.SetActive(true);
        pauseMenu.SetActive(false);
        winMenu.SetActive(true);
        playerText.enabled = false;
    }


    // -=-=- BUTTONS -=-=-
    public void PonButton(int playerIndex) {
        if (GameManager.Instance.TryPonKan(playerIndex)) {
            CloseCallMenu();
        } else {
            StartCoroutine(FlashRed(ponButtonTexts[playerIndex]));
        }
    }

    public void ChiButton(int playerIndex) {
        if (GameManager.Instance.TryChi(playerIndex)) {
            CloseCallMenu();
        } else {
            StartCoroutine(FlashRed(chiButtonTexts[playerIndex]));
        }
    }

    public void DrawNextTileButton() {
        GameManager.Instance.EndTurn();
        CloseCallMenu();
    }

    public void PauseMenuToggle(bool toggle) {
        GameManager.Instance.TogglePause(toggle);
        if (!callMenuOpen) {
            menuUI.SetActive(toggle);
            playerText.enabled = !toggle;
        } else {
            pauseMenu.SetActive(toggle);
            callMenu.SetActive(!toggle);
        }
    }

    public void ReturnToMainMenuButton() {
        // Unpause timeScale to allow fade animation to play
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


    // -=-=- HELPERS -=-=-
    private IEnumerator StartGameTransition() {
        // Fade out to black
        yield return StartCoroutine(Fade(0.0f, 1.0f));

        // Transition from menu to game whilst screen is black
        mainMenu.SetActive(false);
        pauseMenu.SetActive(true);

        menuUI.SetActive(false);
        playerText.enabled = true;
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

    private IEnumerator FlashRed(TMP_Text buttonText) {
        buttonText.color = Color.red;

        yield return new WaitForSeconds(0.5f);

        buttonText.color = Color.white;
    }

    private void LoadRulesTxt() {
        TextAsset textAsset = Resources.Load<TextAsset>("rule_book");

        if (textAsset == null) {
            Debug.LogError($"Failed to load 'rules.txt'");
            return;
        }

        // Every "-----" starts a new page
        string[] splitText = textAsset.text.Split(new[]{"-----"}, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string page in splitText) {
            rulesPages.Add(page.Trim());
        }
    }
}
