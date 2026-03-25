using UnityEngine;
using System.Collections;
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

    [Header("SFX Clips (played via shared sfxSource)")]
    [Tooltip("Sound when enemy is killed")]
    public AudioClip enemyDeathClip;
    [Tooltip("Sound when round is won")]
    public AudioClip roundWinClip;
    [Tooltip("Weapon break sound")]
    public AudioClip weaponBreakClip;
    [Tooltip("Game over / player death")]
    public AudioClip gameOverClip;
    [Tooltip("Round start jingle")]
    public AudioClip roundStartClip;
    [Tooltip("Seconds to fade BGM back in after round start jingle finishes")]
    [Min(0.05f)]
    public float roundStartBgmFadeInDuration = 1f;
    [Tooltip("Seconds to duck BGM down before jingle plays")]
    [Min(0.02f)]
    public float roundStartBgmDuckDuration = 0.12f;
    [Tooltip("Enemy hit by projectile or melee")]
    public AudioClip enemyHitClip;
    [Tooltip("Projectile hits wall/surface")]
    public AudioClip glassPingClip;
    [Tooltip("Heavy impact (reserved for future use)")]
    public AudioClip harshThudClip;
    [Tooltip("Melee weapon picked up")]
    public AudioClip woodSmallPickupClip;
    [Tooltip("Mini-boss spawn SFX")]
    public AudioClip bossSpawnClip;
    [Tooltip("Player hurt SFX")]
    public AudioClip playerHurtClip;
    [Tooltip("Healing success SFX")]
    public AudioClip healingClip;
    [Tooltip("When player throws a building-block / healing projectile")]
    public AudioClip projectileThrowClip;

    [Header("SFX Sources (dedicated AudioSource per sound, editable in Inspector)")]
    [Tooltip("Walking/footstep AudioSource")]
    public AudioSource walkingSource;
    [Tooltip("Player takes damage AudioSource")]
    public AudioSource playerHitSource;
    [Tooltip("Enemy attack whoosh AudioSource")]
    public AudioSource enemyAttackSource;
    [Tooltip("Projectile throw AudioSource")]
    public AudioSource throwSource;
    [Tooltip("Mini-boss spawn alert AudioSource")]
    public AudioSource miniBossSpawnSource;

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
    private Dictionary<string, AudioSource> sourceRegistry = new Dictionary<string, AudioSource>();
    private Coroutine roundStartCoroutine;

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
        // If round start jingle exists, RoundManager will call TriggerRoundStartAudio (duck → jingle → fade BGM in).
        if (roundStartClip == null)
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
        RegisterClip("weapon_break", weaponBreakClip);
        RegisterClip("game_over", gameOverClip);
        RegisterClip("round_start", roundStartClip);
        RegisterClip("enemy_hit", enemyHitClip);
        RegisterClip("glass_ping", glassPingClip);
        RegisterClip("harsh_thud", harshThudClip);
        RegisterClip("wood_small_pickup", woodSmallPickupClip);
        RegisterClip("boss_spawn", bossSpawnClip);
        RegisterClip("player_hurt", playerHurtClip);
        RegisterClip("healing", healingClip);
        RegisterClip("projectile_throw", projectileThrowClip);

        RegisterSource("walking", walkingSource);
        RegisterSource("player_hit", playerHitSource);
        RegisterSource("enemy_attack", enemyAttackSource);
        RegisterSource("throw", throwSource);
        RegisterSource("miniboss_spawn", miniBossSpawnSource);
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
    /// Register a dedicated AudioSource by key. Skips null sources.
    /// </summary>
    public void RegisterSource(string key, AudioSource source)
    {
        if (source != null)
            sourceRegistry[key] = source;
    }

    /// <summary>
    /// Play a registered SFX by key. Checks dedicated AudioSource registry first, then clip registry.
    /// </summary>
    public void PlaySFX(string key, float volumeScale = 1f)
    {
        if (sourceRegistry.TryGetValue(key, out AudioSource source))
        {
            source.volume = sfxVolume * masterVolume * volumeScale;
            source.Play();
            return;
        }

        if (clipRegistry.TryGetValue(key, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume * volumeScale);
            return;
        }

        Debug.LogWarning($"[SoundManager] SFX key '{key}' not registered.");
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
    /// For source-based SFX, moves the source to position and plays.
    /// For clip-based SFX, uses PlayClipAtPoint.
    /// </summary>
    public void PlaySFXAtPosition(string key, Vector3 position, float volumeScale = 1f)
    {
        if (sourceRegistry.TryGetValue(key, out AudioSource source))
        {
            source.transform.position = position;
            source.volume = sfxVolume * masterVolume * volumeScale;
            source.Play();
            return;
        }

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
    /// Play game over SFX and pause SimpleArea BGM until resumed.
    /// </summary>
    public void PlayGameOver()
    {
        PlaySFX("game_over");
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Pause();
    }

    /// <summary>
    /// Convenience: play mini-boss spawn alert.
    /// </summary>
    public void PlayMiniBossSpawn()
    {
        if (sourceRegistry.ContainsKey("miniboss_spawn"))
            PlaySFX("miniboss_spawn");
        else
            PlaySFX("boss_spawn");
    }

    /// <summary>Play enemy hit sound (projectile/melee). Use from enemy or call PlaySFXAtPosition("enemy_hit", pos) for 3D.</summary>
    public void PlayEnemyHit() => PlaySFX("enemy_hit");
    /// <summary>Play glass ping (projectile hit wall). Use PlaySFXAtPosition for 3D at impact.</summary>
    public void PlayGlassPing() => PlaySFX("glass_ping");
    /// <summary>Play harsh thud (reserved for heavy impacts).</summary>
    public void PlayHarshThud() => PlaySFX("harsh_thud");
    /// <summary>Play melee weapon pickup sound.</summary>
    public void PlayWoodSmallPickup() => PlaySFX("wood_small_pickup");
    /// <summary>Play player hurt sound.</summary>
    public void PlayPlayerHurt() => PlaySFX("player_hurt");
    /// <summary>Play healing success sound.</summary>
    public void PlayHealing() => PlaySFX("healing");

    /// <summary>Play melee weapon break (durability depleted).</summary>
    public void PlayWeaponBreak() => PlaySFX("weapon_break");

    /// <summary>Play when player throws a projectile (building block / healing apple).</summary>
    public void PlayProjectileThrow() => PlaySFX("projectile_throw");

    /// <summary>
    /// Call at the beginning of each round. Plays round start jingle first, then fades BGM in.
    /// If no round start clip is assigned, starts BGM at normal volume.
    /// </summary>
    public void TriggerRoundStartAudio()
    {
        if (roundStartClip == null)
        {
            PlayBGM();
            return;
        }

        if (roundStartCoroutine != null)
            StopCoroutine(roundStartCoroutine);
        roundStartCoroutine = StartCoroutine(RoundStartAudioRoutine());
    }

    private IEnumerator RoundStartAudioRoutine()
    {
        float targetVol = bgmVolume * masterVolume;
        if (bgmClip == null)
        {
            Debug.LogWarning("[SoundManager] Round start jingle set but bgmClip is null; playing jingle only.");
            sfxSource.PlayOneShot(roundStartClip, sfxVolume * masterVolume);
            yield return new WaitForSeconds(roundStartClip.length);
            roundStartCoroutine = null;
            yield break;
        }

        if (bgmSource == null)
            SetupAudioSources();

        if (bgmSource.clip != bgmClip)
            bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        if (!bgmSource.isPlaying)
            bgmSource.Play();

        float duck = Mathf.Max(0.02f, roundStartBgmDuckDuration);
        float fadeIn = Mathf.Max(0.05f, roundStartBgmFadeInDuration);
        float startVol = bgmSource.volume;
        float t = 0f;
        while (t < duck)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, t / duck);
            yield return null;
        }
        bgmSource.volume = 0f;

        sfxSource.PlayOneShot(roundStartClip, sfxVolume * masterVolume);
        yield return new WaitForSeconds(roundStartClip.length);

        if (!bgmSource.isPlaying)
            bgmSource.Play();

        t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, targetVol, t / fadeIn);
            yield return null;
        }
        bgmSource.volume = targetVol;
        roundStartCoroutine = null;
    }
}
