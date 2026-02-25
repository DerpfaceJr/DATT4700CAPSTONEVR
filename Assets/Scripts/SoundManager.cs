using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized sound manager for the SimpleArea game scene.
/// Manages all BGM, SFX, and ambient audio from a single location.
/// Place on a "SoundManager" GameObject in the SimpleArea scene.
/// Auto-creates if not found when accessed via Instance.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("BGM")]
    [Tooltip("Background music clip")]
    public AudioClip bgmClip;
    [Range(0f, 1f)]
    public float bgmVolume = 0.5f;

    [Header("SFX Clips")]
    [Tooltip("Sound when enemy is killed")]
    public AudioClip enemyDeathClip;
    [Tooltip("Sound when round is won")]
    public AudioClip roundWinClip;
    [Tooltip("Walking/footstep sound")]
    public AudioClip walkingClip;
    [Tooltip("Player takes damage")]
    public AudioClip playerHitClip;
    [Tooltip("Weapon break sound")]
    public AudioClip weaponBreakClip;
    [Tooltip("Enemy attack whoosh")]
    public AudioClip enemyAttackClip;
    [Tooltip("Projectile throw sound")]
    public AudioClip throwClip;
    [Tooltip("Game over / player death")]
    public AudioClip gameOverClip;
    [Tooltip("Round start jingle")]
    public AudioClip roundStartClip;
    [Tooltip("Mini-boss spawn alert")]
    public AudioClip miniBossSpawnClip;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Header("Audio Sources (auto-created if not set)")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource ambientSource;

    private Dictionary<string, AudioClip> clipRegistry = new Dictionary<string, AudioClip>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupAudioSources();
        RegisterClips();
    }

    void Start()
    {
        PlayBGM();
    }

    private void SetupAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f;
            bgmSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.playOnAwake = false;
        }

        if (ambientSource == null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0f;
            ambientSource.playOnAwake = false;
        }
    }

    private void RegisterClips()
    {
        RegisterClip("enemy_death", enemyDeathClip);
        RegisterClip("round_win", roundWinClip);
        RegisterClip("walking", walkingClip);
        RegisterClip("player_hit", playerHitClip);
        RegisterClip("weapon_break", weaponBreakClip);
        RegisterClip("enemy_attack", enemyAttackClip);
        RegisterClip("throw", throwClip);
        RegisterClip("game_over", gameOverClip);
        RegisterClip("round_start", roundStartClip);
        RegisterClip("miniboss_spawn", miniBossSpawnClip);
    }

    /// <summary>
    /// Register a clip by key for lookup. Skips null clips.
    /// </summary>
    public void RegisterClip(string key, AudioClip clip)
    {
        if (clip != null)
            clipRegistry[key] = clip;
    }

    /// <summary>
    /// Play a registered SFX by key.
    /// </summary>
    public void PlaySFX(string key, float volumeScale = 1f)
    {
        if (clipRegistry.TryGetValue(key, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume * volumeScale);
        }
        else
        {
            Debug.LogWarning($"[SoundManager] SFX key '{key}' not registered.");
        }
    }

    /// <summary>
    /// Play a one-shot SFX clip directly (for clips not in registry).
    /// </summary>
    public void PlaySFXClip(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume * volumeScale);
    }

    /// <summary>
    /// Play a 3D positional SFX at a world position.
    /// </summary>
    public void PlaySFXAtPosition(string key, Vector3 position, float volumeScale = 1f)
    {
        if (clipRegistry.TryGetValue(key, out AudioClip clip))
        {
            AudioSource.PlayClipAtPoint(clip, position, sfxVolume * masterVolume * volumeScale);
        }
    }

    /// <summary>
    /// Play BGM. Restarts if a different clip is provided.
    /// </summary>
    public void PlayBGM(AudioClip overrideClip = null)
    {
        AudioClip clip = overrideClip ?? bgmClip;
        if (clip == null) return;

        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume * masterVolume;
        bgmSource.Play();
    }

    /// <summary>
    /// Stop BGM.
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource.isPlaying)
            bgmSource.Stop();
    }

    /// <summary>
    /// Pause/unpause BGM.
    /// </summary>
    public void PauseBGM(bool pause)
    {
        if (pause)
            bgmSource.Pause();
        else
            bgmSource.UnPause();
    }

    /// <summary>
    /// Set master volume (affects all audio).
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume * masterVolume;
    }

    /// <summary>
    /// Set SFX volume.
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Set BGM volume.
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume * masterVolume;
    }

    /// <summary>
    /// Convenience: play enemy death sound.
    /// </summary>
    public void PlayEnemyDeath() => PlaySFX("enemy_death");

    /// <summary>
    /// Convenience: play round win sound.
    /// </summary>
    public void PlayRoundWin() => PlaySFX("round_win");

    /// <summary>
    /// Convenience: play game over sound.
    /// </summary>
    public void PlayGameOver() => PlaySFX("game_over");

    /// <summary>
    /// Convenience: play mini-boss spawn alert.
    /// </summary>
    public void PlayMiniBossSpawn() => PlaySFX("miniboss_spawn");
}
