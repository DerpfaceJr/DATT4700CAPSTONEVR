using UnityEngine;
using System;

/// <summary>
/// Base enemy behavior component
/// Handles health, damage, and death
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Starting health points")]
    public float maxHealth = 100f;
    
    [Tooltip("Current health points")]
    public float currentHealth;
    
    [Header("Visual Feedback")]
    [Tooltip("Material to flash on damage (optional). If null, uses red tint on all renderers.")]
    public Material damageMaterial;
    
    [Tooltip("Duration of damage flash (seconds)")]
    public float damageFlashDuration = 0.15f;
    
    [Header("Hit Effect")]
    [Tooltip("Particle effect prefab to spawn when taking damage (e.g. HitParticle)")]
    public GameObject hitEffectPrefab;
    
    [Tooltip("Number of hit effect instances to spawn per hit (creates a fuller burst)")]
    [Min(1)]
    public int hitEffectCount = 2;
    
    [Tooltip("Particle size (overrides prefab when > 0)")]
    [Min(0)]
    public float hitEffectSize = 0.5f;
    
    [Tooltip("Particle lifetime in seconds (overrides prefab when > 0)")]
    [Min(0)]
    public float hitEffectLifetime = 0.8f;
    
    [Tooltip("Particle color range - random between start and end")]
    public Color hitEffectColorStart = new Color(1f, 0.3f, 0.1f, 1f);
    public Color hitEffectColorEnd = new Color(1f, 0.6f, 0.2f, 1f);
    
    [Tooltip("Scale multiplier for the hit effect (affects overall size)")]
    [Min(0.1f)]
    public float hitEffectScale = 2.5f;

    [Header("Hit Sound")]
    [Tooltip("Sound when hit by projectile or melee. Played from this enemy's AudioSource. If not set, uses SoundManager enemy_hit at this position.")]
    public AudioClip hitSoundClip;
    [Tooltip("AudioSource on this enemy for hit sound. If not set, one is used or added at runtime.")]
    public AudioSource hitAudioSource;

    [Header("Special")]
    [Tooltip("If true, this enemy cannot die (used for tutorial dummies)")]
    public bool isInvulnerable = false;
    private Renderer[] enemyRenderers;
    private Material[] originalMaterials;
    private static Material s_redFlashMaterial;
    private float damageFlashTimer = 0f;
    
    // Events
    public event Action<EnemyController> OnEnemyDeath;
    public event Action<EnemyController, float> OnEnemyDamaged;
    
    void Awake()
    {
        if (currentHealth <= 0f)
            currentHealth = maxHealth;
    }

    void Start()
    {
        // Find mesh renderers (SkinnedMeshRenderer, MeshRenderer) - exclude SpriteRenderer (icons)
        var all = GetComponentsInChildren<Renderer>();
        var list = new System.Collections.Generic.List<Renderer>();
        foreach (Renderer r in all)
        {
            if (r != null && !(r is SpriteRenderer))
                list.Add(r);
        }
        enemyRenderers = list.ToArray();
        if (enemyRenderers != null && enemyRenderers.Length > 0)
        {
            originalMaterials = new Material[enemyRenderers.Length];
            for (int i = 0; i < enemyRenderers.Length; i++)
            {
                if (enemyRenderers[i] != null)
                    originalMaterials[i] = enemyRenderers[i].material;
            }
        }
    }
    
    void Update()
    {
        // Handle damage flash
        if (damageFlashTimer > 0f)
        {
            damageFlashTimer -= Time.deltaTime;
            if (damageFlashTimer <= 0f && enemyRenderers != null && originalMaterials != null)
            {
                for (int i = 0; i < enemyRenderers.Length && i < originalMaterials.Length; i++)
                {
                    if (enemyRenderers[i] != null && originalMaterials[i] != null)
                        enemyRenderers[i].material = originalMaterials[i];
                }
            }
        }
    }
    
    /// <summary>
    /// Apply damage to this enemy
    /// </summary>
    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0f) return;
        
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0f, currentHealth);
        
        // Trigger damage event
        OnEnemyDamaged?.Invoke(this, damageAmount);
        
        // Play hit sound (from enemy's AudioSource or SoundManager at position)
        if (hitSoundClip != null)
        {
            if (hitAudioSource == null)
                hitAudioSource = GetComponent<AudioSource>();
            if (hitAudioSource == null)
                hitAudioSource = gameObject.AddComponent<AudioSource>();
            if (hitAudioSource != null)
                hitAudioSource.PlayOneShot(hitSoundClip);
        }
        else if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFXAtPosition("enemy_hit", GetHitEffectSpawnCenter());
        }
        
        // Spawn hit particle effect
        if (hitEffectPrefab != null)
        {
            Vector3 center = GetHitEffectSpawnCenter();
            // Offset forward and up so effect is visible (not buried in mesh)
            Vector3 viewOffset = Vector3.up * 0.4f + transform.forward * 0.3f;
            for (int i = 0; i < hitEffectCount; i++)
            {
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-0.15f, 0.15f),
                    UnityEngine.Random.Range(-0.1f, 0.2f),
                    UnityEngine.Random.Range(-0.15f, 0.15f));
                Vector3 spawnPos = center + viewOffset + offset;
                Quaternion rot = hitEffectPrefab.transform.rotation;
                GameObject effect = Instantiate(hitEffectPrefab, spawnPos, rot);
                effect.transform.localScale = Vector3.one * hitEffectScale;
                foreach (ParticleSystem ps in effect.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = ps.main;
                    if (hitEffectSize > 0f) main.startSize = hitEffectSize;
                    if (hitEffectLifetime > 0f) main.startLifetime = hitEffectLifetime;
                    main.startColor = new ParticleSystem.MinMaxGradient(hitEffectColorStart, hitEffectColorEnd);
                    ps.Clear();
                    ps.Play();
                }
                Destroy(effect, 2f);
            }
        }
        
        // Visual feedback: flash red on all renderers
        Material flashMat = damageMaterial;
        if (flashMat == null)
        {
            if (s_redFlashMaterial == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Legacy Shaders/Diffuse");
                s_redFlashMaterial = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
                s_redFlashMaterial.color = new Color(1f, 0.2f, 0.2f, 1f);
            }
            flashMat = s_redFlashMaterial;
        }
        if (enemyRenderers != null && flashMat != null)
        {
            foreach (Renderer r in enemyRenderers)
            {
                if (r != null)
                    r.material = flashMat;
            }
            damageFlashTimer = damageFlashDuration;
        }
        
        // Invulnerable enemies (tutorial dummies) take damage visually but don't die
        if (isInvulnerable)
        {
            currentHealth = maxHealth;
            return;
        }

        if (currentHealth <= 0f)
        {
            OnEnemyDeath?.Invoke(this);
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Get spawn center for hit effects (center of character bounds, or position + up)
    /// </summary>
    private Vector3 GetHitEffectSpawnCenter()
    {
        if (enemyRenderers != null && enemyRenderers.Length > 0)
        {
            Bounds bounds = enemyRenderers[0].bounds;
            for (int i = 1; i < enemyRenderers.Length; i++)
            {
                if (enemyRenderers[i] != null)
                    bounds.Encapsulate(enemyRenderers[i].bounds);
            }
            return bounds.center;
        }
        return transform.position + Vector3.up * 1f;
    }
    
    /// <summary>
    /// Get health percentage (0-1)
    /// </summary>
    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? currentHealth / maxHealth : 0f;
    }
    
    /// <summary>
    /// Check if enemy is alive
    /// </summary>
    public bool IsAlive()
    {
        return currentHealth > 0f;
    }
}

