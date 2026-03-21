using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy behaviour: move toward the player using NavMesh pathfinding (no walls, multi-floor).
/// Falls back to direct movement only when not on NavMesh.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemySeekAndAttack : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Speed when moving toward player")]
    public float moveSpeed = 3f;

    [Tooltip("Use NavMesh for pathfinding (stairs, multiple floors). If no NavMesh, falls back to direct movement.")]
    public bool useNavMesh = true;

    [Tooltip("Player transform (auto-found by tag if not set)")]
    public Transform playerTarget;

    [Header("Separation")]
    [Tooltip("Radius to detect nearby enemies (OverlapSphere)")]
    public float separationRadius = 1.5f;
    [Tooltip("Weight for moving toward player (higher = more seek)")]
    public float seekWeight = 1f;
    [Tooltip("Weight for moving away from other enemies (higher = more separation)")]
    public float separationWeight = 1.5f;
    [Tooltip("Distance ahead to set NavMesh destination when using blended direction")]
    public float separationLookAhead = 4f;

    [Header("Map Bounds")]
    [Tooltip("Padding from walls so enemies stay inside play area")]
    public float wallPadding = 1f;

    [Header("Floor Positioning")]
    [Tooltip("Vertical offset from FloorRef (hips) down to feet. FloorRef is at hips, so we raise placement by this so feet touch the floor.")]
    public float floorRefToFeetOffset = 0.2f;
    [Tooltip("NavMeshAgent base offset (lifts agent above NavMesh to clear steps). Increase if enemies get stuck on floor edges.")]
    public float navAgentBaseOffset = 0.2f;

    [Header("Attack")]
    [Tooltip("Distance to player to trigger attack (stop and play Attack animation)")]
    public float attackRange = 1.5f;
    [Tooltip("Duration of attack animation (seconds)")]
    public float attackDuration = 1.5f;
    [Tooltip("Cooldown between attacks (seconds)")]
    public float attackCooldown = 0.5f;
    [Tooltip("Speed of stepping toward player during attack (helps trigger hitbox contact)")]
    public float attackLungeSpeed = 1.2f;
    [Tooltip("Max distance to lunge toward player during attack")]
    public float attackLungeMaxDistance = 0.6f;
    [Tooltip("Do not start attack if |vertical delta| to aim point exceeds this")]
    public float maxAttackVerticalDelta = 2f;
    [Tooltip("World position used for attack range and lunge aim (e.g. player body). Falls back to playerTarget.")]
    public Transform attackRangeReference;

    [Header("Animator sync")]
    [Tooltip("End IsAttacking when animator leaves the attack state (add Attack state name to match your controller)")]
    public bool syncAttackEndWithAnimator;
    [Tooltip("State name to match on animatorLayer (sub-state name is enough for IsName)")]
    public string attackAnimatorStateName = "Attack";
    public int attackAnimatorLayer;

    /// <summary>True while attack animation is playing. Health is only deducted when this is true and enemy touches player.</summary>
    public bool IsAttacking { get; private set; }

    private Rigidbody rb;
    private NavMeshAgent navAgent;
    private bool isKinematic;
    private float boundsMinX, boundsMaxX, boundsMinZ, boundsMaxZ;
    private bool boundsValid;
    private float destinationUpdateInterval = 0.25f;
    private float nextDestinationTime;
    private Vector3 lastDestination;
    private bool hasSetDestination;
    private float lastPlayerTargetSearchTime;
    private float lastWarpAttemptTime = -999f;
    private const float PLAYER_SEARCH_INTERVAL = 0.5f;
    private const float MIN_DESTINATION_UPDATE_DISTANCE = 0.7f;
    private const float MAX_DESTINATION_UPDATE_INTERVAL = 1f;
    private const float MIN_VERTICAL_UPDATE_DISTANCE = 0.6f;
    private const float WARP_RETRY_INTERVAL = 1f;
    private const float NAVMESH_SAMPLE_RADIUS = 5f;
    /// <summary>Child under XR rig / camera rig — created in scenes for attack range aim (chest proxy below HMD).</summary>
    private const string AttackAimReferenceName = "AttackAimReference";

    private EnemyAnimatorController animController;
    private EnemyAttackDamageWindow damageWindow;
    private Animator enemyAnimator;
    private int attackAnimatorStateHash;
    private float attackStartTime = -999f;
    private float lastAttackEndTime = -999f;
    private Vector3 attackStartPosition;

    /// <summary>Optional transform marking floor/feet (e.g. FloorRef). When set, NavMesh snap and grounding use this instead of root.</summary>
    private Transform floorRef;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            isKinematic = rb.isKinematic;
        }
        CacheFloorRef();
        animController = GetComponent<EnemyAnimatorController>();
        if (animController == null) animController = GetComponentInChildren<EnemyAnimatorController>();

        damageWindow = GetComponentInChildren<EnemyAttackDamageWindow>();
        enemyAnimator = GetComponentInChildren<Animator>();
        if (!string.IsNullOrEmpty(attackAnimatorStateName))
            attackAnimatorStateHash = Animator.StringToHash(attackAnimatorStateName);

        if (useNavMesh)
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = gameObject.AddComponent<NavMeshAgent>();
            }
            if (navAgent != null)
            {
                navAgent.enabled = true;
                navAgent.speed = moveSpeed;
                navAgent.angularSpeed = 540f;
                navAgent.acceleration = 16f;
                navAgent.stoppingDistance = 0.2f;
                navAgent.updatePosition = true;
                navAgent.updateRotation = false; // We control rotation so enemy stays upright (Y-only)
                navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                navAgent.areaMask = -1;
                navAgent.baseOffset = navAgentBaseOffset;
                if (rb != null)
                {
                    rb.isKinematic = true; // Let NavMeshAgent control position so enemy doesn't fall
                }
                SnapRootToNavMeshUsingFloorRef();
            }
        }

        CalculateMapBounds();
        EnsurePlayerTarget();

        if (navAgent != null && navAgent.enabled && playerTarget != null)
        {
            if (navAgent.isOnNavMesh)
            {
                lastDestination = playerTarget.position;
                navAgent.SetDestination(lastDestination);
                hasSetDestination = true;
                nextDestinationTime = Time.time + MAX_DESTINATION_UPDATE_INTERVAL;
            }
        }
    }

    /// <summary>Find FloorRef transform in hierarchy (by name). Used to snap position to floor/feet instead of waist.</summary>
    private void CacheFloorRef()
    {
        floorRef = null;
        Transform t = transform.Find("FloorRef");
        if (t != null) { floorRef = t; return; }
        foreach (Transform child in transform)
        {
            if (child.name == "FloorRef") { floorRef = child; return; }
            t = child.Find("FloorRef");
            if (t != null) { floorRef = t; return; }
        }
        // Deep search
        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == "FloorRef") { floorRef = all[i]; return; }
        }
    }

    /// <summary>Sample NavMesh at floor/feet (FloorRef) and warp root so FloorRef is on the NavMesh. Keeps enemy from falling.</summary>
    private void SnapRootToNavMeshUsingFloorRef()
    {
        if (navAgent == null || !navAgent.enabled) return;
        Vector3 sampleAt = (floorRef != null) ? floorRef.position : transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(sampleAt, out hit, NAVMESH_SAMPLE_RADIUS, NavMesh.AllAreas))
        {
            Vector3 rootPos;
            if (floorRef != null)
            {
                Vector3 feetLevel = hit.position + Vector3.up * floorRefToFeetOffset;
                rootPos = feetLevel + (transform.position - floorRef.position);
            }
            else
                rootPos = hit.position;
            navAgent.Warp(rootPos);
        }
        else
        {
            navAgent.Warp(transform.position);
        }
    }

    /// <summary>Raycast down from FloorRef and adjust root position so feet stay on ground (prevents falling when not on NavMesh).</summary>
    private void SnapRootToGroundUsingFloorRef(ref Vector3 rootPos)
    {
        if (floorRef == null) return;
        Vector3 offset = transform.position - floorRef.position;
        Vector3 floorPos = rootPos - offset;
        RaycastHit hit;
        if (Physics.Raycast(floorPos + Vector3.up * 0.5f, Vector3.down, out hit, 2f))
        {
            Vector3 feetLevel = hit.point + Vector3.up * floorRefToFeetOffset;
            rootPos = feetLevel + offset;
        }
    }

    /// <summary>Force rotation to Y-only (standing straight, no tilt).</summary>
    private void ForceUpright()
    {
        Vector3 e = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, e.y, 0f);
    }

    void LateUpdate()
    {
        ForceUpright();
    }

    private Vector3 ComputeSeparationVector()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, separationRadius);
        Vector3 sum = Vector3.zero;
        foreach (Collider c in colliders)
        {
            if (c.transform.root == transform.root) continue;
            if (!c.transform.root.CompareTag("Enemy")) continue;
            Vector3 toNeighbor = c.transform.position - transform.position;
            float dist = toNeighbor.magnitude;
            if (dist < 0.01f) continue;
            float weight = 1f - (dist / separationRadius);
            sum += -(toNeighbor / dist) * weight;
        }
        return sum.sqrMagnitude > 0.01f ? sum.normalized : Vector3.zero;
    }

    private void EnsurePlayerTarget()
    {
        if (playerTarget != null)
        {
            EnsureAttackRangeReference();
            return;
        }
        GameObject xr = GameObject.Find("XR Origin");
        if (xr == null) xr = GameObject.Find("[BuildingBlock] Camera Rig");
        if (xr != null) { playerTarget = xr.transform; EnsureAttackRangeReference(); return; }
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) { playerTarget = player.transform; EnsureAttackRangeReference(); return; }
        if (Camera.main != null) playerTarget = Camera.main.transform;
        EnsureAttackRangeReference();
    }

    /// <summary>
    /// If attackRangeReference is unset, use a child named AttackAimReference under the rig (place under CenterEyeAnchor, local Y ~ -0.45).
    /// </summary>
    private void EnsureAttackRangeReference()
    {
        if (attackRangeReference != null) return;
        if (playerTarget == null) return;
        attackRangeReference = FindChildRecursive(playerTarget, AttackAimReferenceName);
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

    private void CalculateMapBounds()
    {
        float pad = wallPadding;
        PlayerRespawner pr = FindObjectOfType<PlayerRespawner>();
        if (pr != null)
            pad = pr.wallPadding;

        GameObject westWall = GameObject.Find("WestWall");
        GameObject eastWall = GameObject.Find("EastWall");
        GameObject northWall = GameObject.Find("NorthWall");
        GameObject southWall = GameObject.Find("SouthWall");
        if (westWall == null || eastWall == null || northWall == null || southWall == null)
        {
            Transform[] all = FindObjectsOfType<Transform>(true);
            foreach (Transform t in all)
            {
                if (westWall == null && t.name == "WestWall") westWall = t.gameObject;
                if (eastWall == null && t.name == "EastWall") eastWall = t.gameObject;
                if (northWall == null && t.name == "NorthWall") northWall = t.gameObject;
                if (southWall == null && t.name == "SouthWall") southWall = t.gameObject;
            }
        }

        boundsMinX = -10f;
        boundsMaxX = 10f;
        boundsMinZ = -10f;
        boundsMaxZ = 10f;
        if (westWall != null) boundsMinX = westWall.transform.position.x + pad;
        if (eastWall != null) boundsMaxX = eastWall.transform.position.x - pad;
        if (southWall != null) boundsMinZ = southWall.transform.position.z + pad;
        if (northWall != null) boundsMaxZ = northWall.transform.position.z - pad;
        boundsValid = (boundsMaxX > boundsMinX && boundsMaxZ > boundsMinZ);
    }

    void FixedUpdate()
    {
        if (playerTarget == null)
        {
            if (Time.time - lastPlayerTargetSearchTime >= PLAYER_SEARCH_INTERVAL)
            {
                lastPlayerTargetSearchTime = Time.time;
                EnsurePlayerTarget();
            }
            return;
        }

        EnsureAttackRangeReference();

        Vector3 toPlayer = playerTarget.position - transform.position;
        Vector3 toPlayerFlat = toPlayer;
        toPlayerFlat.y = 0f;
        float distanceFlat = toPlayerFlat.magnitude;

        Vector3 playerAimPos = attackRangeReference != null ? attackRangeReference.position : playerTarget.position;
        Vector3 toAim = playerAimPos - transform.position;
        Vector3 toAimFlat = toAim;
        toAimFlat.y = 0f;
        float distanceToAimFlat = toAimFlat.magnitude;

        // Attack state: end attack when duration elapsed or animator leaves attack state
        if (IsAttacking)
        {
            bool endByTime = Time.time - attackStartTime >= attackDuration;
            bool endByAnim = syncAttackEndWithAnimator && ShouldEndAttackFromAnimator();
            if (endByTime || endByAnim)
            {
                EndAttackSequence();
            }
            else
            {
                if (navAgent != null && navAgent.enabled) navAgent.speed = 0f;
                if (toAimFlat.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Euler(0f, Quaternion.LookRotation(toAimFlat).eulerAngles.y, 0f);
                    Vector3 dir = toAimFlat.normalized;
                    float distFromStart = Vector3.Distance(transform.position, attackStartPosition);
                    float remainingLunge = Mathf.Max(0f, attackLungeMaxDistance - distFromStart);
                    float distToAim = toAimFlat.magnitude;
                    float moveDist = Mathf.Min(attackLungeSpeed * Time.fixedDeltaTime, remainingLunge, Mathf.Max(0f, distToAim - 0.15f));
                    if (moveDist > 0.001f)
                    {
                        Vector3 worldDelta = dir * moveDist;
                        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
                        {
                            navAgent.Move(worldDelta);
                        }
                        else if (rb != null)
                        {
                            Vector3 newPosRb = transform.position + worldDelta;
                            if (floorRef != null) SnapRootToGroundUsingFloorRef(ref newPosRb);
                            rb.MovePosition(newPosRb);
                        }
                        else
                        {
                            transform.position = transform.position + worldDelta;
                        }
                    }
                }
                ForceUpright();
                return;
            }
        }

        // Start attack when in range (and not on cooldown)
        if (!IsAttacking && distanceToAimFlat <= attackRange && Mathf.Abs(toAim.y) <= maxAttackVerticalDelta
            && Time.time - lastAttackEndTime >= attackCooldown)
        {
            IsAttacking = true;
            attackStartTime = Time.time;
            attackStartPosition = transform.position;
            if (animController != null) animController.SetAttackTrigger(true);
            if (navAgent != null && navAgent.enabled) navAgent.speed = 0f;
            if (toAimFlat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Euler(0f, Quaternion.LookRotation(toAimFlat).eulerAngles.y, 0f);
            ForceUpright();
            return;
        }

        bool useDirectMovement = true;
        if (navAgent != null && navAgent.enabled && useNavMesh && navAgent.isOnNavMesh)
        {
            useDirectMovement = false;
            Vector3 baseTarget = playerTarget.position;
            Vector3 separationDir = ComputeSeparationVector();
            separationDir.y = 0f;
            Vector3 separationOffset = separationDir * separationWeight * 0.5f;
            Vector3 modifiedTarget = baseTarget + separationOffset;
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(modifiedTarget, out navHit, 3f, NavMesh.AllAreas))
                modifiedTarget = navHit.position;
            Vector3 targetPos = modifiedTarget;
            float distFromLastDest = hasSetDestination ? Vector3.Distance(targetPos, lastDestination) : float.MaxValue;
            float verticalDiff = hasSetDestination ? Mathf.Abs(targetPos.y - lastDestination.y) : float.MaxValue;
            bool shouldUpdateDestination = !hasSetDestination
                || distFromLastDest >= MIN_DESTINATION_UPDATE_DISTANCE
                || verticalDiff >= MIN_VERTICAL_UPDATE_DISTANCE
                || Time.time >= nextDestinationTime;
            if (shouldUpdateDestination)
            {
                navAgent.SetDestination(targetPos);
                lastDestination = targetPos;
                hasSetDestination = true;
                nextDestinationTime = Time.time + MAX_DESTINATION_UPDATE_INTERVAL;
            }
            navAgent.speed = moveSpeed;
            // Face movement direction while staying upright (Y-only rotation)
            if (navAgent.velocity.sqrMagnitude > 0.01f)
            {
                Vector3 flatVel = navAgent.velocity;
                flatVel.y = 0f;
                if (flatVel.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Euler(0f, Quaternion.LookRotation(flatVel).eulerAngles.y, 0f);
            }
            ForceUpright();
        }
        else if (navAgent != null && navAgent.enabled && useNavMesh && !navAgent.isOnNavMesh)
        {
            if (Time.time - lastWarpAttemptTime >= WARP_RETRY_INTERVAL)
            {
                lastWarpAttemptTime = Time.time;
                Vector3 sampleAt = (floorRef != null) ? floorRef.position : transform.position;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(sampleAt, out hit, NAVMESH_SAMPLE_RADIUS, NavMesh.AllAreas))
                {
                    Vector3 rootPos;
                    if (floorRef != null)
                    {
                        Vector3 feetLevel = hit.position + Vector3.up * floorRefToFeetOffset;
                        rootPos = feetLevel + (transform.position - floorRef.position);
                    }
                    else
                        rootPos = hit.position;
                    navAgent.Warp(rootPos);
                }
                else
                {
                    navAgent.enabled = false;
                }
            }
            useDirectMovement = !navAgent.isOnNavMesh;
        }

        if (useDirectMovement && distanceFlat > 0.01f)
        {
            Vector3 desiredDir = toPlayerFlat.normalized;
            Vector3 separationDir = ComputeSeparationVector();
            separationDir.y = 0f;
            Vector3 blendedDir = desiredDir * seekWeight + separationDir * separationWeight;
            Vector3 direction = blendedDir.sqrMagnitude > 0.01f ? blendedDir.normalized : desiredDir;
            Vector3 desiredVelocity = direction * moveSpeed;
            // When using FloorRef, keep kinematic so enemy doesn't fall; snap root so feet stay on ground
            bool useFloorRefGrounding = (floorRef != null && rb != null);

            if (rb != null)
            {
                if (isKinematic || useFloorRefGrounding)
                {
                    Vector3 newPos = transform.position + desiredVelocity * Time.fixedDeltaTime;
                    if (boundsValid)
                    {
                        newPos.x = Mathf.Clamp(newPos.x, boundsMinX, boundsMaxX);
                        newPos.z = Mathf.Clamp(newPos.z, boundsMinZ, boundsMaxZ);
                    }
                    if (useFloorRefGrounding)
                    {
                        rb.isKinematic = true;
                        SnapRootToGroundUsingFloorRef(ref newPos);
                    }
                    rb.MovePosition(newPos);
                    transform.rotation = Quaternion.Euler(0f, Quaternion.LookRotation(direction).eulerAngles.y, 0f);
                }
                else
                {
                    Vector3 vel = rb.linearVelocity;
                    vel.x = desiredVelocity.x;
                    vel.z = desiredVelocity.z;
                    rb.linearVelocity = vel;
                    transform.rotation = Quaternion.Euler(0f, Quaternion.LookRotation(direction).eulerAngles.y, 0f);
                }
            }
            else
            {
                Vector3 newPos = transform.position + desiredVelocity * Time.fixedDeltaTime;
                if (boundsValid)
                {
                    newPos.x = Mathf.Clamp(newPos.x, boundsMinX, boundsMaxX);
                    newPos.z = Mathf.Clamp(newPos.z, boundsMinZ, boundsMaxZ);
                }
                transform.position = newPos;
                transform.rotation = Quaternion.Euler(0f, Quaternion.LookRotation(direction).eulerAngles.y, 0f);
            }
            ForceUpright();
        }

        if (boundsValid && useDirectMovement && rb != null && !rb.isKinematic)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, boundsMinX, boundsMaxX);
            pos.z = Mathf.Clamp(pos.z, boundsMinZ, boundsMaxZ);
            transform.position = pos;
        }
    }

    /// <summary>Attach a StateMachineBehaviour on the Attack state OnStateExit to call this for tight sync.</summary>
    public void NotifyAnimatorLeftAttackState()
    {
        EndAttackSequence();
    }

    private void EndAttackSequence()
    {
        if (damageWindow != null)
            damageWindow.CloseDamageWindow();
        if (!IsAttacking)
            return;
        IsAttacking = false;
        lastAttackEndTime = Time.time;
        if (animController != null) animController.SetAttackTrigger(false);
    }

    private bool ShouldEndAttackFromAnimator()
    {
        if (!syncAttackEndWithAnimator || enemyAnimator == null) return false;
        if (Time.time - attackStartTime < 0.12f) return false;
        AnimatorStateInfo info = enemyAnimator.GetCurrentAnimatorStateInfo(attackAnimatorLayer);
        bool inAttack = info.IsName(attackAnimatorStateName);
        if (!inAttack && attackAnimatorStateHash != 0)
            inAttack = info.shortNameHash == attackAnimatorStateHash;
        return !inAttack;
    }
}
