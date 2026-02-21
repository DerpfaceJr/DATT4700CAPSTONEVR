using UnityEngine;

/// <summary>
/// Plays the in-game background music when the scene loads (e.g. SimpleArea).
/// Add this to a GameObject in the game scene and assign the BGM clip.
/// Uses its own AudioSource so BGM can loop independently of SFX.
/// </summary>
public class InGameBGM : MonoBehaviour
{
    [Header("BGM Settings")]
    [Tooltip("Background music clip to play (e.g. Assets/Audio/InGameBGM.mp3)")]
    public AudioClip bgmClip;

    [Tooltip("Volume for BGM (0-1). Uses AudioSource.volume if not set.")]
    [Range(0f, 1f)]
    public float volume = 0.5f;

    [Tooltip("AudioSource for BGM. If not set, one will be added to this GameObject.")]
    public AudioSource bgmAudioSource;

    void Start()
    {
        if (bgmClip == null)
        {
            Debug.LogWarning("[InGameBGM] No BGM clip assigned. Assign InGameBGM.mp3 in the Inspector.");
            return;
        }

        if (bgmAudioSource == null)
        {
            bgmAudioSource = GetComponent<AudioSource>();
            if (bgmAudioSource == null)
                bgmAudioSource = gameObject.AddComponent<AudioSource>();
        }

        bgmAudioSource.clip = bgmClip;
        bgmAudioSource.loop = true;
        bgmAudioSource.volume = volume;
        bgmAudioSource.spatialBlend = 0f; // 2D so music is not positional
        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.Play();
    }
}
