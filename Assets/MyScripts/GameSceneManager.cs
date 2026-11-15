using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    [Header("Scene Names")]
    public string homeScene = "HomeScene";
    public string gamePlayScene = "GamePlayScene";

    [Header("Panels")]
    public GameObject progressPanel;

    private void Start()
    {
        if (progressPanel != null)
            progressPanel.SetActive(false);
    }

    // Home Scene Start Button
    public void StartGame()
    {
        SceneManager.LoadScene(gamePlayScene);
    }

    // Pause Button (Go Back Home)
    public void PauseGame()
    {
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

    // Quit Game
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