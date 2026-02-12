using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Manages player health and death
/// Takes damage from enemy projectiles
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum player health")]
    public float maxHealth = 1f;
    
    [Tooltip("Current player health")]
    public float currentHealth = 1f;
    
    [Tooltip("Invulnerability duration after taking damage")]
    public float invulnerabilityDuration = 1.0f;
    
    [Header("Death Settings")]
    [Tooltip("Respawn player on death (false = game over handled by RoundManager)")]
    public bool autoRespawn = false;
    
    [Tooltip("Delay before respawning")]
    public float respawnDelay = 2.0f;
    
    [Header("Events")]
    public UnityEvent<float> onDamageTaken; // Passes damage amount
    public UnityEvent<float> onHealthChanged; // Passes current health
    public UnityEvent onDeath;
    public UnityEvent onRespawn;
    
    private bool isInvulnerable = false;
    private float invulnerabilityEndTime = 0f;
    private PlayerRespawner respawner;
    private bool isDead = false;

    [Header("Health Bar Red")]
    public Image healthBar;
    
    void Start()
    {
        currentHealth = maxHealth;
        healthBar.fillAmount = currentHealth / maxHealth;

        Debug.Log($"[PlayerHealth Start] currentHealth={currentHealth}, fill={healthBar.fillAmount}");

        respawner = FindObjectOfType<PlayerRespawner>();
        
        if (respawner == null)
        {
            Debug.LogWarning("PlayerHealth: No PlayerRespawner found in scene!");
        }

        ActivateInvulnerability();
    }
    
    void Update()
    {
        // Check invulnerability expiration
        if (isInvulnerable && Time.time >= invulnerabilityEndTime)
        {
            isInvulnerable = false;
        }
    }
    
    /// <summary>
    /// Take damage from an enemy projectile or other source
    /// </summary>
    public void TakeDamage(float damage)
    {
        // Ignore damage if invulnerable or already dead
        if (isInvulnerable || isDead)
        {
            Debug.Log($"PlayerHealth: Damage blocked (invulnerable: {isInvulnerable}, dead: {isDead})");
            return;
        }
        
        // Apply damage
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);
        healthBar.fillAmount = currentHealth / maxHealth;
        
        Debug.Log($"PlayerHealth: Took {damage:F1} damage! Current health: {currentHealth:F1}/{maxHealth:F1}");
        
        // Invoke events
        onDamageTaken?.Invoke(damage);
        onHealthChanged?.Invoke(currentHealth);
        healthBar.fillAmount = currentHealth / maxHealth;

        // Check for death
        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            // Activate invulnerability
            ActivateInvulnerability();
        }
    }
    
    /// <summary>
    /// Heal the player
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;
        
        float oldHealth = currentHealth;
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        float actualHealed = currentHealth - oldHealth;
        
        if (actualHealed > 0)
        {
            Debug.Log($"PlayerHealth: Healed {actualHealed:F1}! Current health: {currentHealth:F1}/{maxHealth:F1}");
            onHealthChanged?.Invoke(currentHealth);
        }
    }
    
    /// <summary>
    /// Activate temporary invulnerability
    /// </summary>
    private void ActivateInvulnerability()
    {
        isInvulnerable = true;
        invulnerabilityEndTime = Time.time + invulnerabilityDuration;
        Debug.Log($"PlayerHealth: Invulnerable for {invulnerabilityDuration:F1} seconds");
    }

    public bool IsInvulnerable()
    {
        return isInvulnerable;
    }

    /// <summary>
    /// Handle player death
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("PlayerHealth: Player died!");
        
        onDeath?.Invoke();
        
        // Respawn if enabled
        if (autoRespawn)
        {
            Invoke(nameof(Respawn), respawnDelay);
        }
    }
    
    /// <summary>
    /// Respawn the player
    /// </summary>
    private void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        healthBar.fillAmount = currentHealth / maxHealth;
        ActivateInvulnerability();

        //isInvulnerable = false;
        
        Debug.Log("PlayerHealth: Player respawned!");
        
        // Trigger respawner if available
        if (respawner != null)
        {
            respawner.RespawnPlayer();
        }
        
        onRespawn?.Invoke();
        onHealthChanged?.Invoke(currentHealth);
    }
    
    /// <summary>
    /// Get current health as percentage (0-1)
    /// </summary>
    public float GetHealthPercent()
    {
        return maxHealth > 0 ? currentHealth / maxHealth : 0f;
    }
    
    /// <summary>
    /// Check if player is alive
    /// </summary>
    public bool IsAlive()
    {
        return !isDead && currentHealth > 0f;
    }
    
    /// <summary>
    /// Reset health to maximum
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        healthBar.fillAmount = currentHealth / maxHealth;
        isDead = false;
        ActivateInvulnerability();

        onHealthChanged?.Invoke(currentHealth);
    }
}
