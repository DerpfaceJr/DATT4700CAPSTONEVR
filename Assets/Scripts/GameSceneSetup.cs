using UnityEngine;

/// <summary>
/// Auto-configures all game systems when SimpleArea scene loads.
/// Place on any persistent root GameObject in the SimpleArea scene.
/// Creates SoundManager, HighScoreManager, and MeleeWeaponSetup if missing.
/// This minimizes manual Inspector setup required by the developer.
/// </summary>
public class GameSceneSetup : MonoBehaviour
{
    [Header("Auto-Setup Toggles")]
    public bool setupSoundManager = true;
    public bool setupHighScoreManager = true;
    public bool setupMeleeWeapons = true;

    void Awake()
    {
        if (setupSoundManager)
            EnsureSoundManager();
        if (setupHighScoreManager)
            EnsureHighScoreManager();
    }

    void Start()
    {
        if (setupMeleeWeapons)
            EnsureMeleeWeaponSetup();
    }

    private void EnsureSoundManager()
    {
        if (SoundManager.Instance != null) return;
        if (FindObjectOfType<SoundManager>() != null) return;

        // SoundManagerSetup will handle creation and migration
        if (FindObjectOfType<SoundManagerSetup>() == null)
        {
            GameObject obj = new GameObject("SoundManagerBootstrap");
            obj.AddComponent<SoundManagerSetup>();
        }
    }

    private void EnsureHighScoreManager()
    {
        if (HighScoreManager.Instance != null) return;
        if (FindObjectOfType<HighScoreManager>() != null) return;

        GameObject obj = new GameObject("HighScoreManager");
        obj.AddComponent<HighScoreManager>();
    }

    private void EnsureMeleeWeaponSetup()
    {
        if (FindObjectOfType<MeleeWeaponSetup>() != null) return;

        GameObject obj = new GameObject("MeleeWeaponSetup");
        obj.AddComponent<MeleeWeaponSetup>();
    }
}
