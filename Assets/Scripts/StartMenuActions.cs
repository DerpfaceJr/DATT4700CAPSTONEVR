using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles Start Menu button actions: Start loads SimpleArea, Quit exits the application.
/// Place on the Canvas or any active object in the Start Menu scene.
/// Buttons are auto-found at runtime if not assigned in the Inspector.
/// </summary>
public class StartMenuActions : MonoBehaviour
{
    [Header("Button references (optional – auto-found if not set)")]
    [Tooltip("Assign in Inspector to wire Start button, or leave empty to auto-find.")]
    public Button startButton;

    [Tooltip("Assign in Inspector to wire Quit button, or leave empty to auto-find.")]
    public Button quitButton;

    [Tooltip("Assign in Inspector to wire Help button, or leave empty to auto-find.")]
    public Button helpButton;

    [Header("Scene to load")]
    [Tooltip("Scene name to load when Start is clicked (must be in Build Settings). Default: SimpleArea")]
    public string gameSceneName = "SlideArea";

    public string helpSceneName = "HelpArea";

    [Tooltip("If true, use build index 1 instead of scene name")]
    public bool useBuildIndex = true;

    private void Start()
    {
        if (startButton == null)
            startButton = FindButton("Start Button") ?? FindButtonByPath("Canvas/Menu/Start Button");
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);
            Debug.Log("[StartMenuActions] Start button onClick wired to OnStartClicked.");
        }
        else
            Debug.LogWarning("[StartMenuActions] Start Button not found.");

        if (quitButton == null)
            quitButton = FindButton("Quit Button") ?? FindButtonByPath("Canvas/Menu/Quit Button");
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);
            Debug.Log("[StartMenuActions] Quit button onClick wired to OnQuitClicked.");
        }
        else
            Debug.LogWarning("[StartMenuActions] Quit Button not found.");

        if (helpButton == null)
            helpButton = FindButton("Help Button") ?? FindButtonByPath("Canvas/Menu/Help Button");
        if (helpButton != null)
        {
            helpButton.onClick.RemoveAllListeners();
            helpButton.onClick.AddListener(OnHelpClicked);
            Debug.Log("[StartMenuActions] Help button onClick wired to OnQuitClicked.");
        }
        else
            Debug.LogWarning("[StartMenuActions] Help Button not found.");
    }


    private Button FindButton(string name)
    {
        var buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var b in buttons)
        {
            if (b.gameObject.name == name)
                return b;
        }
        return null;
    }

    private Button FindButtonByPath(string path)
    {
        // path e.g. "Canvas/Menu/Start Button" – find Canvas then use Transform.Find
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return null;
        var t = canvas.transform.Find(path.Replace("Canvas/", ""));
        return t != null ? t.GetComponent<Button>() : null;
    }

    /// <summary>Called when Start button is clicked. Loads the game scene (SimpleArea).</summary>
    public void OnStartClicked()
    {
        if (useBuildIndex)
        {
            SceneManager.LoadScene(2);
            Debug.Log("[StartMenuActions] Loading scene at build index 2 (SimpleArea).");
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
            Debug.Log($"[StartMenuActions] Loading scene: {gameSceneName}");
        }
    }

    public void OnHelpClicked()
    {
        SceneManager.LoadScene(helpSceneName);
    }   

    /// <summary>Called when Quit button is clicked. Exits the application (or stops Play in Editor).</summary>
    public void OnQuitClicked()
    {
        Debug.Log("[StartMenuActions] Quit requested.");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
