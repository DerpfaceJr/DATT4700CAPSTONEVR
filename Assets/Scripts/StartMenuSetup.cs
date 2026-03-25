using UnityEngine;

/// <summary>
/// Auto-configures Start Menu scene systems.
/// Creates HighScoreDisplay, HighScoreManager, and TutorialArea if missing.
/// Place on any root GameObject in the Start Menu scene.
/// </summary>
public class StartMenuSetup : MonoBehaviour
{
    [Header("Auto-Setup Toggles")]
    public bool setupSoundManager = true;
    public bool setupHighScoreDisplay = true;
    public bool setupTutorialArea = true;

    [Header("Audio (Start Menu + Shared Combat SFX)")]
    [Tooltip("BGM clip for Start Menu scene (e.g. Audio/BGM/StartMenuBGM).")]
    public AudioClip startMenuBgmClip;
    [Range(0f, 1f)]
    public float startMenuBgmVolume = 0.5f;

    [Tooltip("Enemy death SFX (shared with game scene).")]
    public AudioClip enemyDeathClip;
    [Tooltip("Round win SFX (shared with game scene).")]
    public AudioClip roundWinClip;
    [Tooltip("Walking SFX source clip (shared).")]
    public AudioClip walkingClip;
    [Tooltip("Enemy hit SFX (projectile/melee hit).")]
    public AudioClip enemyHitClip;
    [Tooltip("Projectile wall impact SFX.")]
    public AudioClip glassPingClip;
    [Tooltip("Heavy impact SFX.")]
    public AudioClip harshThudClip;
    [Tooltip("Melee pickup SFX.")]
    public AudioClip woodSmallPickupClip;

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
        if (setupSoundManager)
            EnsureSoundManager();

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

    private void EnsureSoundManager()
    {
        SoundManager sm = SoundManager.Instance;
        if (sm == null)
            sm = FindObjectOfType<SoundManager>();

        if (sm == null)
        {
            GameObject obj = new GameObject("SoundManager");
            sm = obj.AddComponent<SoundManager>();
        }

        // Start menu BGM
        if (startMenuBgmClip != null)
            sm.bgmClip = startMenuBgmClip;
        sm.bgmVolume = startMenuBgmVolume;

        // Shared combat clips so Start Menu tutorial area can use same audio setup.
        if (enemyDeathClip != null) sm.enemyDeathClip = enemyDeathClip;
        if (roundWinClip != null) sm.roundWinClip = roundWinClip;
        if (enemyHitClip != null) sm.enemyHitClip = enemyHitClip;
        if (glassPingClip != null) sm.glassPingClip = glassPingClip;
        if (harshThudClip != null) sm.harshThudClip = harshThudClip;
        if (woodSmallPickupClip != null) sm.woodSmallPickupClip = woodSmallPickupClip;

        // Optional dedicated walking source clip if assigned.
        if (walkingClip != null && sm.walkingSource != null)
            sm.walkingSource.clip = walkingClip;

        // Rebuild runtime registries with newly assigned clips.
        sm.RegisterClip("enemy_death", sm.enemyDeathClip);
        sm.RegisterClip("round_win", sm.roundWinClip);
        sm.RegisterClip("enemy_hit", sm.enemyHitClip);
        sm.RegisterClip("glass_ping", sm.glassPingClip);
        sm.RegisterClip("harsh_thud", sm.harshThudClip);
        sm.RegisterClip("wood_small_pickup", sm.woodSmallPickupClip);
        if (sm.walkingSource != null)
            sm.RegisterSource("walking", sm.walkingSource);

        // Ensure Start Menu BGM starts now with assigned clip.
        sm.PlayBGM();
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
