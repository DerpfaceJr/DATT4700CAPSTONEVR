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

    [Header("Map Bounds")]
    [Tooltip("Padding from walls so enemies stay inside play area")]
    public float wallPadding = 1f;

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
            Vector3 rootPos = (floorRef != null) ? hit.position + (transform.position - floorRef.position) : hit.position;
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
            rootPos = hit.point + offset;
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

    private void EnsurePlayerTarget()
    {
        if (playerTarget != null) return;
        GameObject xr = GameObject.Find("XR Origin");
        if (xr == null) xr = GameObject.Find("[BuildingBlock] Camera Rig");
        if (xr != null) { playerTarget = xr.transform; return; }
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) { playerTarget = player.transform; return; }
        if (Camera.main != null) playerTarget = Camera.main.transform;
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

        Vector3 toPlayer = playerTarget.position - transform.position;
        Vector3 toPlayerFlat = toPlayer;
        toPlayerFlat.y = 0f;
        float distanceFlat = toPlayerFlat.magnitude;

        bool useDirectMovement = true;
        if (navAgent != null && navAgent.enabled && useNavMesh && navAgent.isOnNavMesh)
        {
            useDirectMovement = false;
            Vector3 targetPos = playerTarget.position;
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
                    Vector3 rootPos = (floorRef != null) ? hit.position + (transform.position - floorRef.position) : hit.position;
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
            Vector3 direction = toPlayerFlat.normalized;
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
}
