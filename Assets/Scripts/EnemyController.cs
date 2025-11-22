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
    [Tooltip("Material to flash on damage (optional)")]
    public Material damageMaterial;
    
    [Tooltip("Duration of damage flash (seconds)")]
    public float damageFlashDuration = 0.1f;
    
    private Material originalMaterial;
    private Renderer enemyRenderer;
    private float damageFlashTimer = 0f;
    
    // Events
    public event Action<EnemyController> OnEnemyDeath;
    public event Action<EnemyController, float> OnEnemyDamaged;
    
    void Start()
    {
        currentHealth = maxHealth;
        enemyRenderer = GetComponent<Renderer>();
        
        if (enemyRenderer != null)
        {
            originalMaterial = enemyRenderer.material;
        }
    }
    
    void Update()
    {
        // Handle damage flash
        if (damageFlashTimer > 0f)
        {
            damageFlashTimer -= Time.deltaTime;
            if (damageFlashTimer <= 0f && enemyRenderer != null && originalMaterial != null)
            {
                enemyRenderer.material = originalMaterial;
            }
        }
    }
    
    /// <summary>
    /// Apply damage to this enemy
    /// </summary>
    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0f) return; // Already dead
        
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0f, currentHealth);
        
        // Trigger damage event
        OnEnemyDamaged?.Invoke(this, damageAmount);
        
        // Visual feedback
        if (damageMaterial != null && enemyRenderer != null)
        {
            enemyRenderer.material = damageMaterial;
            damageFlashTimer = damageFlashDuration;
        }
        
        // Check for death
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

