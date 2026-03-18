using UnityEngine;

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
    [Tooltip("Delay before destroying the object after healing (lets sound/particle play)")]
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
    [Tooltip("Sound when healing projectile hits enemy. If not set, uses SoundManager glass_ping.")]
    public AudioClip hitClip;
    [Tooltip("AudioSource for hit sound. If not set, one is used or added at runtime.")]
    public AudioSource hitAudioSource;

    private Rigidbody rb;
    private PlayerHealth cachedPlayerHealth;
    private bool hasHealed = false;

    void Start()
    {
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
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

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

        cachedPlayerHealth = FindFirstObjectByType<PlayerHealth>();

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

        if (cachedPlayerHealth != null && cachedPlayerHealth.IsAlive())
        {
            cachedPlayerHealth.Heal(healAmount);
            Debug.Log($"[HealingProjectile] Apple hit {enemy.gameObject.name}! Healed player by {healAmount} HP (now {cachedPlayerHealth.currentHealth}/{cachedPlayerHealth.maxHealth})");
        }
        else
        {
            Debug.LogWarning($"[HealingProjectile] Apple hit enemy but PlayerHealth is {(cachedPlayerHealth == null ? "NULL" : "DEAD")}!");
        }

        Vector3 contactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

        // Play impact sound
        if (hitClip != null)
        {
            AudioSource.PlayClipAtPoint(hitClip, contactPoint);
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

        // Destroy the apple
        Destroy(gameObject, destroyDelay);
    }
}
