using UnityEngine;

/// <summary>
/// When an enemy touches the player, deal 1 damage (player has 1 HP = game over).
/// Works when the player walks into an enemy (OnControllerColliderHit) and when
/// the enemy walks into the player (OnTriggerEnter via a body trigger collider).
/// Attach to the same GameObject as CharacterController (e.g. VRMovementController).
/// </summary>
public class PlayerLoseOnEnemyTouch : MonoBehaviour
{
    [Tooltip("Damage dealt when an enemy touches the player (1 = instant death with 1 HP)")]
    public float touchDamage = 1f;

    [Tooltip("Tag used for enemy GameObjects")]
    public string enemyTag = "Enemy";

    [Tooltip("Extra radius/height on the body trigger so enemies are detected from the sides (not only when jumping on them). 0 = use CharacterController size only.")]
    [Range(0f, 0.5f)]
    public float triggerPadding = 0.25f;

    [Tooltip("Fallback: check distance to enemies every frame (use when trigger/collision fails, e.g. VR rig setup).")]
    public bool useProximityFallback = true;
    [Tooltip("Proximity radius for fallback touch detection (enemy within this = touch).")]
    public float proximityTouchRadius = 0.55f;
    [Tooltip("Cooldown between touch damage (prevents multi-hit from physics jitter).")]
    public float touchCooldown = 0.4f;
    [Tooltip("Max vertical distance for touch (ignore enemy if more than this above/below player).")]
    public float maxVerticalTouchDistance = 1.2f;

    private PlayerHealth playerHealth;
    private CapsuleCollider bodyTrigger;
    private float lastTouchTime = -999f;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
        }
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }
        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerLoseOnEnemyTouch: No PlayerHealth found on player. Enemy touch will not deal damage.");
        }
        else
        {
            //touchDamage = Mathf.Max(touchDamage, playerHealth.maxHealth);
        }

        EnsureBodyTrigger();
    }

    /// <summary>
    /// Ensures a trigger collider exists so we detect enemies walking INTO the player
    /// (OnControllerColliderHit only fires when the player moves into the enemy).
    /// When no CharacterController on this object, relies on proximity fallback instead.
    /// </summary>
    private void EnsureBodyTrigger()
    {
        CapsuleCollider existingTrigger = GetComponent<CapsuleCollider>();
        if (existingTrigger != null && existingTrigger.isTrigger)
        {
            bodyTrigger = existingTrigger;
            return;
        }

        CharacterController cc = GetComponent<CharacterController>();
        if (cc == null) return;

        bodyTrigger = gameObject.AddComponent<CapsuleCollider>();
        bodyTrigger.isTrigger = true;
        bodyTrigger.radius = cc.radius + triggerPadding;
        bodyTrigger.height = cc.height + (triggerPadding * 2f);
        bodyTrigger.center = cc.center;
        bodyTrigger.direction = 1; // Y-axis
    }

    void FixedUpdate()
    {
        if (useProximityFallback && playerHealth != null && Time.time - lastTouchTime >= touchCooldown)
        {
            CheckProximityTouch();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        GameObject enemyRoot = GetEnemyRoot(collision.gameObject);
        if (enemyRoot != null)
            TryDealTouchDamage(enemyRoot);
    }

    void OnTriggerEnter(Collider other)
    {
        GameObject enemyRoot = GetEnemyRoot(other.gameObject);
        if (enemyRoot != null)
            TryDealTouchDamage(enemyRoot);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit == null || hit.gameObject == null) return;
        GameObject enemyRoot = GetEnemyRoot(hit.gameObject);
        if (enemyRoot != null)
            TryDealTouchDamage(enemyRoot);
    }

    private void TryDealTouchDamage(GameObject enemyRoot)
    {
        if (playerHealth == null) return;
        if (playerHealth.IsInvulnerable()) return;
        if (Time.time - lastTouchTime < touchCooldown) return;
        var seekAttack = enemyRoot.GetComponentInChildren<EnemySeekAndAttack>();
        if (seekAttack != null && !seekAttack.IsAttacking) return;
        lastTouchTime = Time.time;

        float damage = touchDamage;
        MiniBossMarker boss = enemyRoot.GetComponent<MiniBossMarker>();
        if (boss != null)
            damage *= boss.damageMultiplier;

        playerHealth.TakeDamage(damage);
        if (playerHealth != null && !playerHealth.IsAlive())
            EnsureLosePanelShown();
    }

    private void EnsureLosePanelShown()
    {
        RoundManager rm = FindObjectOfType<RoundManager>();
        if (rm != null)
            rm.TriggerGameOver();
    }

    private void CheckProximityTouch()
    {
        Transform playerTransform = playerHealth.transform;
        Vector3 playerPos = playerTransform.position;
        Collider[] nearby = Physics.OverlapSphere(playerPos, proximityTouchRadius);
        foreach (Collider c in nearby)
        {
            if (c == null) continue;
            GameObject enemyRoot = GetEnemyRoot(c.gameObject);
            if (enemyRoot == null) continue;
            float verticalDiff = Mathf.Abs(c.transform.position.y - playerPos.y);
            if (verticalDiff > maxVerticalTouchDistance) continue;
            TryDealTouchDamage(enemyRoot);
            return;
        }
    }

    private GameObject GetEnemyRoot(GameObject obj)
    {
        if (obj == null) return null;
        Transform t = obj.transform;
        while (t != null)
        {
            if (t.CompareTag(enemyTag)) return t.gameObject;
            t = t.parent;
        }
        return null;
    }

    private bool IsEnemy(GameObject obj)
    {
        return GetEnemyRoot(obj) != null;
    }
}
