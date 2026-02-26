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
            Die();
        }
    }
    
    /// <summary>
    /// Handle enemy death
    /// </summary>
    private void Die()
    {
        // Trigger death event
        OnEnemyDeath?.Invoke(this);
        
        // Destroy the enemy GameObject
        Destroy(gameObject);
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

