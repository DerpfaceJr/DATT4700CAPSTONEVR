using UnityEngine;

/// <summary>
/// Auto-creates and configures SoundManager in the SimpleArea scene at runtime.
/// Attach to any persistent GameObject in the scene.
/// Migrates audio references from RoundManager and InGameBGM if SoundManager is missing.
/// </summary>
public class SoundManagerSetup : MonoBehaviour
{
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

        Debug.Log("[SoundManagerSetup] Created SoundManager and migrated audio references.");
    }
}
