using UnityEngine;

/// <summary>
/// Auto-creates and configures SoundManager in the SimpleArea scene at runtime.
/// Attach to any persistent GameObject in the scene.
/// Migrates audio references from RoundManager and InGameBGM if SoundManager is missing.
/// </summary>
public class SoundManagerSetup : MonoBehaviour
{
    [Header("Optional Clip Overrides")]
    [Tooltip("Mini-boss spawn SFX (BossSpawnSFX)")]
    public AudioClip bossSpawnClip;
    [Tooltip("Player hurt SFX (PlayerHurtSFX)")]
    public AudioClip playerHurtClip;
    [Tooltip("Healing success SFX (HealingSFX)")]
    public AudioClip healingClip;

    void Awake()
    {
        if (SoundManager.Instance != null) return;

        SoundManager existing = FindObjectOfType<SoundManager>();
        if (existing != null) return;

        GameObject smObj = new GameObject("SoundManager");
        SoundManager sm = smObj.AddComponent<SoundManager>();

        // Migrate clips from RoundManager if available
        RoundManager rm = FindObjectOfType<RoundManager>();
        if (rm != null)
        {
            if (rm.bgmClip != null)
                sm.bgmClip = rm.bgmClip;
            if (rm.enemyDeathClip != null)
                sm.enemyDeathClip = rm.enemyDeathClip;
            if (rm.winClip != null)
                sm.roundWinClip = rm.winClip;
            sm.bgmVolume = rm.bgmVolume;
        }

        // Migrate from InGameBGM if available
        InGameBGM igbgm = FindObjectOfType<InGameBGM>();
        if (igbgm != null)
        {
            if (igbgm.bgmClip != null && sm.bgmClip == null)
                sm.bgmClip = igbgm.bgmClip;
            // Disable InGameBGM to prevent duplicate playback
            igbgm.enabled = false;
        }

        // Apply optional clip overrides for new combat audio.
        if (bossSpawnClip != null) sm.bossSpawnClip = bossSpawnClip;
        if (playerHurtClip != null) sm.playerHurtClip = playerHurtClip;
        if (healingClip != null) sm.healingClip = healingClip;

        // Ensure runtime registry includes any overrides set above.
        sm.RegisterClip("boss_spawn", sm.bossSpawnClip);
        sm.RegisterClip("player_hurt", sm.playerHurtClip);
        sm.RegisterClip("healing", sm.healingClip);

        Debug.Log("[SoundManagerSetup] Created SoundManager and migrated audio references.");
    }
}
