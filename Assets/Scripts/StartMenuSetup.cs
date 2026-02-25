using UnityEngine;

/// <summary>
/// Auto-configures Start Menu scene systems.
/// Creates HighScoreDisplay, HighScoreManager, and TutorialArea if missing.
/// Place on any root GameObject in the Start Menu scene.
/// </summary>
public class StartMenuSetup : MonoBehaviour
{
    [Header("Auto-Setup Toggles")]
    public bool setupHighScoreDisplay = true;
    public bool setupTutorialArea = true;

    [Header("High Score Panel")]
    [Tooltip("Offset from the main Canvas for the hologram panel")]
    public Vector3 highScorePanelOffset = new Vector3(1.5f, 0f, 0f);

    [Header("Tutorial Area")]
    [Tooltip("Center position for the tutorial area")]
    public Vector3 tutorialCenter = new Vector3(3f, 0f, -2f);

    [Tooltip("Enemy prefab for tutorial dummy (optional - creates procedural if null)")]
    public GameObject enemyPrefab;

    void Start()
    {
        EnsureHighScoreManager();

        if (setupHighScoreDisplay)
            EnsureHighScoreDisplay();
        if (setupTutorialArea)
            EnsureTutorialArea();
    }

    private void EnsureHighScoreManager()
    {
        if (HighScoreManager.Instance != null) return;
        if (FindObjectOfType<HighScoreManager>() != null) return;

        GameObject obj = new GameObject("HighScoreManager");
        obj.AddComponent<HighScoreManager>();
    }

    private void EnsureHighScoreDisplay()
    {
        if (FindObjectOfType<HighScoreDisplay>() != null) return;

        GameObject obj = new GameObject("HighScoreDisplaySetup");
        HighScoreDisplay display = obj.AddComponent<HighScoreDisplay>();
        display.panelOffset = highScorePanelOffset;
    }

    private void EnsureTutorialArea()
    {
        if (FindObjectOfType<TutorialArea>() != null) return;

        GameObject obj = new GameObject("TutorialArea");
        TutorialArea tutorial = obj.AddComponent<TutorialArea>();
        tutorial.tutorialCenter = tutorialCenter;
        tutorial.enemyPrefab = enemyPrefab;
    }
}
