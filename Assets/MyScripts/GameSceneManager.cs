using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    [Header("Scene Names")]
    public string homeScene = "HomeScene";
    public string gamePlayScene = "GamePlayScene";

    [Header("Panels")]
    public GameObject progressPanel;
    public GameObject settingsPanel;
    public GameObject pausePanel;

    private void Start()
    {
        Time.timeScale = 1f;

        if (progressPanel != null)
            progressPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    // Home Scene Start Button
    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gamePlayScene);
    }

    // Pause Button (Opens Pause Panel & Freezes Gameplay)
    public void OpenPausePanel()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    // Pause Panel -> Resume Button
    public void ResumeGame()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    // Pause Panel -> Home Button
    public void GoHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeScene);
    }

    // Open Progress Panel
    public void OpenProgressPanel()
    {
        if (progressPanel != null)
            progressPanel.SetActive(true);
    }

    // Close Progress Panel
    public void CloseProgressPanel()
    {
        if (progressPanel != null)
            progressPanel.SetActive(false);
    }

    // Open Settings Panel
    public void OpenSettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    // Close Settings Panel
    public void CloseSettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // Quit Game (Used by Pause Panel Exit Button & Home Quit Button)
    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Privacy Policy
    public void OpenPrivacyPolicy(string url)
    {
        Application.OpenURL(url);
    }
}
