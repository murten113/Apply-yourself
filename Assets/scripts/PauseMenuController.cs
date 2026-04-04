using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Toggle pause via <see cref="pauseAction"/> (assign <b>Player / Pause</b> from <c>InputSystem_Actions</c>).
/// Change bindings in the Input Actions asset or in the Inspector reference. If <see cref="pauseAction"/> is unassigned,
/// falls back to Escape and gamepad Start (legacy).
/// Assign a child panel as <see cref="menuRoot"/> and wire UI buttons to <see cref="Resume"/>, <see cref="QuitToMainMenu"/>, and <see cref="QuitGame"/>.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Input System")]
    [Tooltip("Player / Pause — set bindings in InputSystem_Actions (default: Escape, gamepad Start). Leave empty for legacy Escape + Start only.")]
    [SerializeField] private InputActionReference pauseAction;

    [Header("UI")]
    [Tooltip("The panel (or full-screen object) shown when paused. Starts disabled if you leave it off in the scene.")]
    [SerializeField] private GameObject menuRoot;

    [Header("Scenes")]
    [Tooltip("Must match the scene name in File > Build Settings (e.g. MainMenu).")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Behaviour")]
    [SerializeField] private bool pauseTime = true;
    [Tooltip("When resuming, match typical FPS behaviour after closing the menu.")]
    [SerializeField] private bool lockCursorOnResume = true;

    private bool isPaused;

    private void Awake()
    {
        if (menuRoot != null)
            menuRoot.SetActive(false);
    }

    private void OnEnable()
    {
        pauseAction?.action?.Enable();
    }

    private void Update()
    {
        if (WasPausePressed())
            SetPaused(!isPaused);
    }

    private bool WasPausePressed()
    {
        var a = pauseAction?.action;
        if (a != null)
            return a.WasPressedThisFrame();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;
        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            return true;
        return false;
    }

    /// <summary>Hook to Resume button.</summary>
    public void Resume()
    {
        SetPaused(false);
    }

    /// <summary>Hook to Quit / Main menu button.</summary>
    public void QuitToMainMenu()
    {
        CommunityGardenPersistence.SaveNow();
        SetPaused(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>Hook to Quit game button.</summary>
    public void QuitGame()
    {
        CommunityGardenPersistence.SaveNow();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
        if (menuRoot != null)
            menuRoot.SetActive(paused);

        if (pauseTime)
            Time.timeScale = paused ? 0f : 1f;

        Cursor.lockState = paused ? CursorLockMode.None : (lockCursorOnResume ? CursorLockMode.Locked : CursorLockMode.None);
        Cursor.visible = paused;
    }
}
