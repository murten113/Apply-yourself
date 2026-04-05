using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a GameObject in the main menu scene. Wire buttons to <see cref="PlayGame"/> and <see cref="QuitGame"/>.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("First gameplay scene name — must match File > Build Settings (e.g. Level Layout).")]
    [SerializeField] private string gameSceneName = "Level Layout";
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject creditMenu;

    private void Start()
    {
        BackMenu();
    }

    public void PlayGame()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("MainMenuController: assign Game Scene Name.", this);
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void CreditMenu()
    {
        mainMenu.SetActive(false);
        creditMenu.SetActive(true);
    }

    public void BackMenu()
    {
        mainMenu.SetActive(true);
        creditMenu.SetActive(false);
    }
}
