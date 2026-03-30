using UnityEngine;
using System.Collections;

/// <summary>
/// When thrown at an enemy and it hits, heals the player instead of damaging the enemy.
/// The apple does ZERO damage. CombatSystem skips damage for objects with this component.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class HealingProjectile : MonoBehaviour
{
    [Header("Healing Settings")]
    [Tooltip("HP restored to player when this hits an enemy")]
    public float healAmount = 10f;

    [Tooltip("Minimum velocity to count as a valid hit (must be thrown, not just rolling)")]
    public float minHitVelocity = 1.0f;

    [Header("Break on Heal")]
    [Tooltip("Delay before hiding the object after healing (lets sound/particle play). Object respawns next round via ObjectPositionResetter.")]
    public float destroyDelay = 0.15f;

    [Tooltip("Particle effect prefab to spawn when the apple breaks (e.g. HitParticle). If null, no particles.")]
    public GameObject breakEffectPrefab;

    [Tooltip("Number of particle instances to spawn (creates a fuller burst)")]
    [Min(1)]
    public int healEffectCount = 2;

    [Tooltip("Healing particle color range - random between start and end (green tones)")]
    public Color healEffectColorStart = new Color(0.3f, 1f, 0.4f, 1f);
    public Color healEffectColorEnd = new Color(0.2f, 0.8f, 0.3f, 1f);

    [Header("Impact Sound")]
    [Tooltip("Optional override when heal succeeds (actual HP gained). If not set, uses SoundManager healing clip.")]
    public AudioClip hitClip;
    [Tooltip("AudioSource for hit sound. If not set, one is used or added at runtime.")]
    public AudioSource hitAudioSource;

    private Rigidbody rb;
    private PlayerHealth cachedPlayerHealth;
    private bool hasHealed = false;

    void Awake()
    {
        cachedPlayerHealth = FindFirstObjectByType<PlayerHealth>();
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"[HealingProjectile] No Rigidbody found on {gameObject.name}");
            return;
        }

        // Remove conflicting BuildingBlockProjectile if still present
        BuildingBlockProjectile bbp = GetComponent<BuildingBlockProjectile>();
        if (bbp != null)
        {
            Destroy(bbp);
            Debug.Log($"[HealingProjectile] Removed conflicting BuildingBlockProjectile from {gameObject.name}");
        }

        rb.useGravity = true;
        //rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            if (GetComponent<MeshFilter>() != null)
            {
                MeshFilter mf = GetComponent<MeshFilter>();
                if (mf.sharedMesh != null)
                {
                    SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
                    sphereCol.radius = 0.3f;
                }
            }
        }
        else
        {
            col.isTrigger = false;
        }

        //cachedPlayerHealth = FindFirstObjectByType<PlayerHealth>();

        PlayerBallCollisionIgnore collisionIgnore = FindFirstObjectByType<PlayerBallCollisionIgnore>();
        if (collisionIgnore != null && collisionIgnore.ignoreBallCollisions)
        {
            collisionIgnore.AddBallToIgnore(gameObject);
        }

        Debug.Log($"[HealingProjectile] Initialized on {gameObject.name}, healAmount={healAmount}, playerHealth={(cachedPlayerHealth != null ? "found" : "NOT FOUND")}");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHealed) return;
        if (rb == null) return;

        //float objectSpeed = rb.linearVelocity.magnitude;
        float objectSpeed = rb.linearVelocity.magnitude;
        float relativeSpeed = collision.relativeVelocity.magnitude;
        float effectiveSpeed = Mathf.Max(objectSpeed, relativeSpeed);

        if (effectiveSpeed < minHitVelocity) return;

        // Check if we hit an enemy (look in parent hierarchy and on the object itself)
        GameObject hitObj = collision.gameObject;
        EnemyController enemy = hitObj.GetComponentInParent<EnemyController>();
        if (enemy == null) enemy = hitObj.GetComponent<EnemyController>();
        if (enemy == null && hitObj.transform.root != null)
            enemy = hitObj.transform.root.GetComponentInChildren<EnemyController>();

        if (enemy == null)
        {
            Debug.Log($"[HealingProjectile] Hit {hitObj.name} (tag={hitObj.tag}) but no EnemyController found, speed={effectiveSpeed:F2}");
            return;
        }

        hasHealed = true;

        if (cachedPlayerHealth == null)
            cachedPlayerHealth = FindFirstObjectByType<PlayerHealth>();

        float healedAmount = 0f;
        if (cachedPlayerHealth != null && cachedPlayerHealth.IsAlive())
        {
            healedAmount = cachedPlayerHealth.Heal(healAmount);
            Debug.Log($"[HealingProjectile] Apple hit {enemy.gameObject.name}! Healed player by {healedAmount} HP (now {cachedPlayerHealth.currentHealth}/{cachedPlayerHealth.maxHealth})");
        }
        else
        {
            Debug.LogWarning($"[HealingProjectile] Apple hit enemy but PlayerHealth is {(cachedPlayerHealth == null ? "NULL" : "DEAD")}!");
        }

        Vector3 contactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

        // Healing success: healing SFX (or custom hitClip). Otherwise: glass ping (no heal / at full HP / dead player).
        if (healedAmount > 0f)
        {
            if (hitClip != null)
                AudioSource.PlayClipAtPoint(hitClip, contactPoint);
            else if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFXAtPosition("healing", contactPoint);
        }
        else if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFXAtPosition("glass_ping", contactPoint);
        }

        // Spawn healing burst particle effect (green)
        if (breakEffectPrefab != null)
        {
            for (int i = 0; i < healEffectCount; i++)
            {
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-0.15f, 0.15f),
                    UnityEngine.Random.Range(-0.1f, 0.2f),
                    UnityEngine.Random.Range(-0.15f, 0.15f));
                Vector3 spawnPos = contactPoint + offset;
                GameObject fx = Instantiate(breakEffectPrefab, spawnPos, breakEffectPrefab.transform.rotation);
                foreach (ParticleSystem ps in fx.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = ps.main;
                    main.startColor = new ParticleSystem.MinMaxGradient(healEffectColorStart, healEffectColorEnd);
                    ps.Clear();
                    ps.Play();
                }
                Destroy(fx, 3f);
            }
        }

        // Hide instead of destroy so ObjectPositionResetter can restore the apple next round
        StartCoroutine(HideAfterHealCoroutine());
    }

    private IEnumerator HideAfterHealCoroutine()
    {
        yield return new WaitForSeconds(destroyDelay);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Called when a round resets positions. Reactivates a used apple and clears heal state.
    /// </summary>
    public void ResetForNewRound()
    {
        hasHealed = false;
        cachedPlayerHealth = FindFirstObjectByType<PlayerHealth>();
        gameObject.SetActive(true);
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        PlayerBallCollisionIgnore collisionIgnore = FindFirstObjectByType<PlayerBallCollisionIgnore>();
        if (collisionIgnore != null && collisionIgnore.ignoreBallCollisions)
            collisionIgnore.AddBallToIgnore(gameObject);
    }
}
