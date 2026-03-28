using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When an enemy touches the player, deal damage.
/// Works when the player walks into an enemy (OnControllerColliderHit) and when
/// the enemy walks into the player (OnTriggerEnter via a body trigger collider).
/// Attach to the player (often with PlayerHealth). By default, damage is limited to strike windows / attacking state; disable requireAttackWindowGating for always-on contact damage (e.g. VR debugging).
/// </summary>
public class PlayerLoseOnEnemyTouch : MonoBehaviour
{
    [Tooltip("Damage dealt when an enemy touches the player (player has 100 HP)")]
    public float touchDamage = 25f;

    [Tooltip("Tag used for enemy GameObjects")]
    public string enemyTag = "Enemy";

    [Header("Contact damage rules")]
    [Tooltip("When on (default), damage only during EnemyAttackDamageWindow strike frames (or overlap rules), or while EnemySeekAndAttack.IsAttacking if there is no window. When off, any contact deals damage.")]
    public bool requireAttackWindowGating = true;
    [Tooltip("When attack window gating is on: at most one touch damage per strike window (or per seek attack if there is no window). Stops repeated hits from physics/proximity before the swing finishes.")]
    public bool oneTouchPerStrikeWindow = true;

    [Tooltip("Extra radius/height on the body trigger so enemies are detected from the sides (not only when jumping on them). 0 = use CharacterController size only.")]
    [Range(0f, 0.5f)]
    public float triggerPadding = 0.25f;

    [Tooltip("Optional world-space origin for proximity checks (e.g. chest). If null, uses tracked damage center or PlayerHealth transform.")]
    public Transform touchCheckOrigin;

    [Header("VR combat bounds")]
    [Tooltip("When set (or auto-found), attack overlap uses this position + damageBoundsSize so stand/crouch matches the HMD/chest, not only CharacterController at the feet.")]
    public Transform damageBoundsCenter;
    [Tooltip("World-space box size for damage overlap (center = damageBoundsCenter).")]
    public Vector3 damageBoundsSize = new Vector3(0.7f, 1.15f, 0.7f);
    [Tooltip("If true and damageBoundsCenter is resolved, overlap tests prefer this box over CharacterController.bounds.")]
    public bool useTrackedDamageBounds = true;
    [Tooltip("If damageBoundsCenter is unset, search hierarchy and XR rig for AttackAimReference (same as enemy aim target).")]
    public bool autoFindAttackAimReference = true;
    [Tooltip("When AttackAimReference is missing, use Main Camera position minus this height as combat bounds center (follows stand/crouch).")]
    public float torsoOffsetBelowCamera = 0.45f;

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
    private CapsuleCollider managedExternalTrigger;
    private PlayerLoseTouchPhysicsProxy managedProxy;
    private bool createdExternalTrigger;
    private bool createdExternalProxy;
    private float lastGlobalTouchTime = -999f;
    private readonly Dictionary<int, float> lastTouchByEnemyInstanceId = new Dictionary<int, float>(32);
    private readonly Dictionary<int, bool> strikeTouchDealtThisCycle = new Dictionary<int, bool>(32);

    private const string AttackAimReferenceName = "AttackAimReference";
    private static readonly string[] XrRigSearchNames =
    {
        "[BuildingBlock] Camera Rig",
        "[BuildingBlock] Camera Rig ",
        "XR Origin",
    };

    private int lateFindAttempts;

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

        ResolveCharacterController();
        EnsureBodyTrigger();
        EnsureDamageBoundsCenter();
    }

    void OnDestroy()
    {
        TearDownExternalPhysicsArtifacts();
    }

    /// <summary>
    /// Resolves the movement body CharacterController (often on VRMovementController), which may be a sibling
    /// of the avatar object that holds PlayerHealth / this script.
    /// </summary>
    private void ResolveCharacterController()
    {
        playerCharacterController = GetComponent<CharacterController>()
            ?? GetComponentInParent<CharacterController>()
            ?? GetComponentInChildren<CharacterController>();
        if (playerCharacterController != null)
            return;

        VRMovementController[] movers = FindObjectsByType<VRMovementController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (VRMovementController mover in movers)
        {
            if (mover == null) continue;
            Transform origin = mover.xrOrigin;
            if (origin != null && (transform == origin || transform.IsChildOf(origin)))
            {
                playerCharacterController = mover.GetComponent<CharacterController>();
                if (playerCharacterController != null)
                    return;
            }
        }

        if (movers.Length == 1 && movers[0] != null)
            playerCharacterController = movers[0].GetComponent<CharacterController>();
    }

    void LateUpdate()
    {
        if (!autoFindAttackAimReference || damageBoundsCenter != null || lateFindAttempts >= 30)
            return;
        lateFindAttempts++;
        EnsureDamageBoundsCenter();
    }

    private void EnsureDamageBoundsCenter()
    {
        if (!autoFindAttackAimReference || damageBoundsCenter != null)
            return;
        Transform start = playerHealth != null ? playerHealth.transform : transform;
        damageBoundsCenter = FindChildRecursive(start, AttackAimReferenceName);
        if (damageBoundsCenter == null)
            damageBoundsCenter = FindChildRecursive(start.root, AttackAimReferenceName);
        if (damageBoundsCenter == null)
        {
            foreach (string rigName in XrRigSearchNames)
            {
                GameObject rig = GameObject.Find(rigName);
                if (rig == null) continue;
                damageBoundsCenter = FindChildRecursive(rig.transform, AttackAimReferenceName);
                if (damageBoundsCenter != null) return;
            }
        }
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null) return null;
        foreach (Transform t in parent)
        {
            if (t.name == childName) return t;
            Transform nested = FindChildRecursive(t, childName);
            if (nested != null) return nested;
        }
        return null;
    }

    /// <summary>
    /// Ensures a trigger collider exists so we detect enemies walking INTO the player
    /// (OnControllerColliderHit only fires when the player moves into the enemy).
    /// When the CharacterController lives on another object (typical VR: VRMovementController vs Main character),
    /// the trigger and a small proxy component are added there so Unity delivers trigger/controller-hit messages.
    /// </summary>
    private void EnsureBodyTrigger()
    {
        if (explicitBodyTrigger != null && explicitBodyTrigger.isTrigger)
        {
            TearDownExternalPhysicsArtifacts();
            bodyTrigger = explicitBodyTrigger;
            return;
        }

        CapsuleCollider existingOnSelf = GetComponent<CapsuleCollider>();
        if (existingOnSelf != null && existingOnSelf.isTrigger)
        {
            TearDownExternalPhysicsArtifacts();
            bodyTrigger = existingOnSelf;
            return;
        }

        CharacterController cc = playerCharacterController;
        if (cc == null)
        {
            TearDownExternalPhysicsArtifacts();
            return;
        }

        if (cc.gameObject == gameObject)
        {
            TearDownExternalPhysicsArtifacts();
            bodyTrigger = gameObject.AddComponent<CapsuleCollider>();
            bodyTrigger.isTrigger = true;
            bodyTrigger.radius = cc.radius + triggerPadding;
            bodyTrigger.height = cc.height + (triggerPadding * 2f);
            bodyTrigger.center = cc.center;
            bodyTrigger.direction = 1; // Y-axis
            return;
        }

        TearDownExternalPhysicsArtifacts();
        GameObject host = cc.gameObject;
        managedExternalTrigger = host.AddComponent<CapsuleCollider>();
        createdExternalTrigger = true;
        managedExternalTrigger.isTrigger = true;
        managedExternalTrigger.radius = cc.radius + triggerPadding;
        managedExternalTrigger.height = cc.height + (triggerPadding * 2f);
        managedExternalTrigger.center = cc.center;
        managedExternalTrigger.direction = 1;

        managedProxy = host.AddComponent<PlayerLoseTouchPhysicsProxy>();
        createdExternalProxy = true;
        managedProxy.target = this;

        bodyTrigger = managedExternalTrigger;
    }

    private void TearDownExternalPhysicsArtifacts()
    {
        if (createdExternalTrigger && managedExternalTrigger != null)
        {
            Destroy(managedExternalTrigger);
            managedExternalTrigger = null;
        }
        createdExternalTrigger = false;

        if (createdExternalProxy && managedProxy != null)
        {
            Destroy(managedProxy);
            managedProxy = null;
        }
        createdExternalProxy = false;
    }

    /// <summary>Used by <see cref="PlayerLoseTouchPhysicsProxy"/> on the movement CharacterController object.</summary>
    public void ForwardTriggerFromProxy(Collider other)
    {
        if (!isActiveAndEnabled || other == null) return;
        GameObject enemyRoot = GetEnemyRoot(other.gameObject);
        if (enemyRoot != null)
            TryDealTouchDamage(enemyRoot, other);
    }

    /// <summary>Used by <see cref="PlayerLoseTouchPhysicsProxy"/> on the movement CharacterController object.</summary>
    public void ForwardControllerHitFromProxy(ControllerColliderHit hit)
    {
        if (!isActiveAndEnabled || hit == null || hit.gameObject == null) return;
        GameObject enemyRoot = GetEnemyRoot(hit.gameObject);
        if (enemyRoot != null)
            TryDealTouchDamage(enemyRoot, hit.collider);
    }

    /// <summary>Used by <see cref="PlayerLoseTouchPhysicsProxy"/> on the movement CharacterController object.</summary>
    public void ForwardCollisionFromProxy(Collision collision)
    {
        if (!isActiveAndEnabled || collision == null) return;
        GameObject enemyRoot = GetEnemyRoot(collision.gameObject);
        if (enemyRoot != null)
            TryDealTouchDamage(enemyRoot, collision.collider);
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
        EnemyAttackDamageWindow damageWindow = enemyRoot.GetComponentInChildren<EnemyAttackDamageWindow>();
        EnemySeekAndAttack seekAttack = enemyRoot.GetComponentInChildren<EnemySeekAndAttack>();

        if (damageWindow != null && damageWindow.contactMode == EnemyContactDamageMode.AttackWindowOnly)
        {
            if (!damageWindow.IsDamageActive)
                strikeTouchDealtThisCycle.Remove(enemyId);
        }
        else if (seekAttack != null && !seekAttack.IsAttacking)
        {
            strikeTouchDealtThisCycle.Remove(enemyId);
        }

        if (lastTouchByEnemyInstanceId.TryGetValue(enemyId, out float lastForEnemy))
        {
            if (Time.time - lastForEnemy < touchCooldown)
                return;
        }
        if (useGlobalTouchCooldown && Time.time - lastGlobalTouchTime < globalTouchCooldown)
            return;

        if (requireAttackWindowGating)
        {
            if (damageWindow != null)
            {
                if (!PassesAttackVolumeGate(damageWindow, enemyHitCollider))
                    return;
            }
            else if (seekAttack != null && !seekAttack.IsAttacking)
            {
                return;
            }

            if (oneTouchPerStrikeWindow && ShouldLimitToOneTouchPerStrike(damageWindow, seekAttack))
            {
                if (strikeTouchDealtThisCycle.TryGetValue(enemyId, out bool already) && already)
                    return;
            }
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

        if (requireAttackWindowGating && oneTouchPerStrikeWindow && ShouldLimitToOneTouchPerStrike(damageWindow, seekAttack))
            strikeTouchDealtThisCycle[enemyId] = true;
    }

    private static bool ShouldLimitToOneTouchPerStrike(EnemyAttackDamageWindow dw, EnemySeekAndAttack seek)
    {
        if (dw != null && dw.contactMode == EnemyContactDamageMode.AttackWindowOnly)
            return true;
        return dw == null && seek != null;
    }

    private void TrimTouchCooldownMapIfNeeded()
    {
        if (lastTouchByEnemyInstanceId.Count > 128)
            lastTouchByEnemyInstanceId.Clear();
        if (strikeTouchDealtThisCycle.Count > 128)
            strikeTouchDealtThisCycle.Clear();
    }

    private void EnsureLosePanelShown()
    {
        RoundManager rm = FindObjectOfType<RoundManager>();
        if (rm != null)
            rm.TriggerGameOver();
    }

    private void CheckProximityTouch()
    {
        Vector3 playerPos = GetProximityCheckPosition();
        bool useHighAnchor = touchCheckOrigin != null
            || (useTrackedDamageBounds && (damageBoundsCenter != null || Camera.main != null));
        Collider[] nearby = Physics.OverlapSphere(playerPos, proximityTouchRadius);
        foreach (Collider c in nearby)
        {
            if (c == null) continue;
            GameObject enemyRoot = GetEnemyRoot(c.gameObject);
            if (enemyRoot == null) continue;
            if (requireAttackWindowGating)
            {
                if (!PassesVerticalProximityFilter(c, playerPos, useHighAnchor))
                    continue;

                EnemyAttackDamageWindow w = enemyRoot.GetComponentInChildren<EnemyAttackDamageWindow>();
                if (w != null && !PassesAttackVolumeGate(w, c))
                    continue;
            }

            TryDealTouchDamage(enemyRoot, c);
        }
    }

    private Vector3 GetProximityCheckPosition()
    {
        if (touchCheckOrigin != null)
            return touchCheckOrigin.position;
        if (useTrackedDamageBounds)
        {
            if (damageBoundsCenter != null)
                return damageBoundsCenter.position;
            if (Camera.main != null)
                return Camera.main.transform.position - Vector3.up * torsoOffsetBelowCamera;
        }
        return playerHealth != null ? playerHealth.transform.position : transform.position;
    }

    /// <summary>
    /// When the proximity origin is chest/head height, enemy colliders at the feet used to fail the old Y test (diff &gt; 1.2m). Compare using collider bounds instead.
    /// </summary>
    private bool PassesVerticalProximityFilter(Collider enemyCollider, Vector3 playerAnchorPos, bool anchorIsHigh)
    {
        if (!anchorIsHigh)
        {
            float verticalDiff = Mathf.Abs(enemyCollider.transform.position.y - playerAnchorPos.y);
            return verticalDiff <= maxVerticalTouchDistance;
        }
        Bounds eb = enemyCollider.bounds;
        float anchorY = playerAnchorPos.y;
        return anchorY >= eb.min.y - maxVerticalTouchDistance && anchorY <= eb.max.y + maxVerticalTouchDistance;
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
        if (useTrackedDamageBounds)
        {
            Vector3 center = GetTrackedCombatWorldCenter();
            bounds = new Bounds(center, damageBoundsSize);
            return true;
        }
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

    private Vector3 GetTrackedCombatWorldCenter()
    {
        if (damageBoundsCenter != null)
            return damageBoundsCenter.position;
        if (Camera.main != null)
            return Camera.main.transform.position - Vector3.up * torsoOffsetBelowCamera;
        if (playerHealth != null)
            return playerHealth.transform.position;
        return transform.position;
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
