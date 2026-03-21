using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When an enemy touches the player, deal damage.
/// Works when the player walks into an enemy (OnControllerColliderHit) and when
/// the enemy walks into the player (OnTriggerEnter via a body trigger collider).
/// Attach to the same GameObject as CharacterController (e.g. VRMovementController).
/// </summary>
public class PlayerLoseOnEnemyTouch : MonoBehaviour
{
    [Tooltip("Damage dealt when an enemy touches the player (player has 100 HP)")]
    public float touchDamage = 25f;

    [Tooltip("Tag used for enemy GameObjects")]
    public string enemyTag = "Enemy";

    [Tooltip("Extra radius/height on the body trigger so enemies are detected from the sides (not only when jumping on them). 0 = use CharacterController size only.")]
    [Range(0f, 0.5f)]
    public float triggerPadding = 0.25f;

    [Tooltip("Optional world-space origin for proximity checks (e.g. chest). If null, uses PlayerHealth transform.")]
    public Transform touchCheckOrigin;

    [Tooltip("If set and isTrigger, used as the body trigger when no CharacterController trigger exists on this object.")]
    public CapsuleCollider explicitBodyTrigger;

    [Tooltip("Fallback: check distance to enemies every frame (use when trigger/collision fails, e.g. VR rig setup).")]
    public bool useProximityFallback = true;
    [Tooltip("Proximity radius for fallback (tighter when using EnemyAttackDamageWindow attack volumes).")]
    public float proximityTouchRadius = 0.4f;
    [Tooltip("Cooldown between touch damage per enemy instance (prevents multi-hit from physics jitter).")]
    public float touchCooldown = 0.4f;
    [Tooltip("Optional: also enforce a minimum time between any two enemy hits.")]
    public bool useGlobalTouchCooldown = false;
    public float globalTouchCooldown = 0.15f;
    [Tooltip("Max vertical distance for touch (ignore enemy if more than this above/below player).")]
    public float maxVerticalTouchDistance = 1.2f;

    private PlayerHealth playerHealth;
    private CharacterController playerCharacterController;
    private CapsuleCollider bodyTrigger;
    private float lastGlobalTouchTime = -999f;
    private readonly Dictionary<int, float> lastTouchByEnemyInstanceId = new Dictionary<int, float>(32);

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

        playerCharacterController = GetComponent<CharacterController>();
        if (playerCharacterController == null)
            playerCharacterController = GetComponentInParent<CharacterController>();

        EnsureBodyTrigger();
    }

    /// <summary>
    /// Ensures a trigger collider exists so we detect enemies walking INTO the player
    /// (OnControllerColliderHit only fires when the player moves into the enemy).
    /// When no CharacterController on this object, relies on proximity fallback or explicitBodyTrigger.
    /// </summary>
    private void EnsureBodyTrigger()
    {
        if (explicitBodyTrigger != null && explicitBodyTrigger.isTrigger)
        {
            bodyTrigger = explicitBodyTrigger;
            return;
        }

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
        if (useProximityFallback && playerHealth != null)
            CheckProximityTouch();
    }

    void OnCollisionEnter(Collision collision)
    {
        GameObject enemyRoot = GetEnemyRoot(collision.gameObject);
        if (enemyRoot != null)
            TryDealTouchDamage(enemyRoot, collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        GameObject enemyRoot = GetEnemyRoot(other.gameObject);
        if (enemyRoot != null)
            TryDealTouchDamage(enemyRoot, other);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit == null || hit.gameObject == null) return;
        GameObject enemyRoot = GetEnemyRoot(hit.gameObject);
        if (enemyRoot != null)
            TryDealTouchDamage(enemyRoot, hit.collider);
    }

    private void TryDealTouchDamage(GameObject enemyRoot, Collider enemyHitCollider = null)
    {
        if (playerHealth == null) return;
        if (playerHealth.IsInvulnerable()) return;

        int enemyId = enemyRoot.GetInstanceID();
        if (lastTouchByEnemyInstanceId.TryGetValue(enemyId, out float lastForEnemy))
        {
            if (Time.time - lastForEnemy < touchCooldown)
                return;
        }
        if (useGlobalTouchCooldown && Time.time - lastGlobalTouchTime < globalTouchCooldown)
            return;

        EnemyAttackDamageWindow damageWindow = enemyRoot.GetComponentInChildren<EnemyAttackDamageWindow>();
        EnemySeekAndAttack seekAttack = enemyRoot.GetComponentInChildren<EnemySeekAndAttack>();

        if (damageWindow != null)
        {
            if (!PassesAttackVolumeGate(damageWindow, enemyHitCollider))
                return;
        }
        else
        {
            if (seekAttack != null && !seekAttack.IsAttacking)
                return;
        }

        lastTouchByEnemyInstanceId[enemyId] = Time.time;
        lastGlobalTouchTime = Time.time;
        TrimTouchCooldownMapIfNeeded();

        float damage = touchDamage;
        MiniBossMarker boss = enemyRoot.GetComponent<MiniBossMarker>();
        if (boss != null)
            damage *= boss.damageMultiplier;

        playerHealth.TakeDamage(damage);
        if (playerHealth != null && !playerHealth.IsAlive())
            EnsureLosePanelShown();
    }

    private void TrimTouchCooldownMapIfNeeded()
    {
        if (lastTouchByEnemyInstanceId.Count > 128)
            lastTouchByEnemyInstanceId.Clear();
    }

    private void EnsureLosePanelShown()
    {
        RoundManager rm = FindObjectOfType<RoundManager>();
        if (rm != null)
            rm.TriggerGameOver();
    }

    private void CheckProximityTouch()
    {
        Vector3 playerPos = touchCheckOrigin != null ? touchCheckOrigin.position : playerHealth.transform.position;
        Collider[] nearby = Physics.OverlapSphere(playerPos, proximityTouchRadius);
        foreach (Collider c in nearby)
        {
            if (c == null) continue;
            GameObject enemyRoot = GetEnemyRoot(c.gameObject);
            if (enemyRoot == null) continue;
            float verticalDiff = Mathf.Abs(c.transform.position.y - playerPos.y);
            if (verticalDiff > maxVerticalTouchDistance) continue;

            EnemyAttackDamageWindow w = enemyRoot.GetComponentInChildren<EnemyAttackDamageWindow>();
            if (w != null && !PassesAttackVolumeGate(w, c))
                continue;

            TryDealTouchDamage(enemyRoot, c);
        }
    }

    private bool PassesAttackVolumeGate(EnemyAttackDamageWindow w, Collider enemyHitCollider)
    {
        if (w.contactMode != EnemyContactDamageMode.AttackWindowOnly)
            return true;
        if (!w.IsDamageActive)
            return false;
        if (w.attackDamageCollider == null)
            return true;
        bool hitVol = w.IsHitFromAttackVolume(enemyHitCollider);
        if (w.requireExactHitCollider)
            return hitVol;
        if (hitVol)
            return true;
        return TryGetPlayerBounds(out Bounds pb) && w.AttackVolumeOverlapsPlayerBounds(pb);
    }

    private bool TryGetPlayerBounds(out Bounds bounds)
    {
        if (playerCharacterController != null)
        {
            bounds = playerCharacterController.bounds;
            return true;
        }
        if (bodyTrigger != null)
        {
            bounds = bodyTrigger.bounds;
            return true;
        }
        bounds = default;
        return false;
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
