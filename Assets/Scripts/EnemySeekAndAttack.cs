using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy behaviour: move toward the player (NavMesh pathfinding) and attack (ranged projectile).
/// Uses the map's NavMesh so enemies can chase the player across floors/stairs.
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

    [Header("Ranged Attack")]
    [Tooltip("Distance at which enemy will shoot at player")]
    public float attackRange = 8f;

    [Tooltip("Cooldown between shots (seconds)")]
    public float attackCooldown = 2f;

    [Tooltip("Projectile prefab (e.g. Enemy Ball). If not set, uses EnemySpawner.enemyBallPrefab")]
    public GameObject projectilePrefab;

    [Tooltip("Speed of thrown projectile toward player")]
    public float projectileSpeed = 12f;

    [Tooltip("Height offset for spawn (e.g. hand height)")]
    public float projectileSpawnHeightOffset = 0.5f;

    [Tooltip("Tag for enemy projectiles")]
    public string enemyProjectileTag = "EnemyProjectile";

    [Tooltip("Layer name for enemy projectiles (optional)")]
    public string enemyProjectileLayer = "EnemyProjectile";

    [Header("Map Bounds")]
    [Tooltip("Padding from walls so enemies stay inside play area")]
    public float wallPadding = 1f;

    private Rigidbody rb;
    private NavMeshAgent navAgent;
    private float lastAttackTime = -999f;
    private bool isKinematic;
    private float boundsMinX, boundsMaxX, boundsMinZ, boundsMaxZ;
    private bool boundsValid;
    private float destinationUpdateInterval = 0.25f;
    private float nextDestinationTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            isKinematic = rb.isKinematic;
        }

        if (useNavMesh)
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = gameObject.AddComponent<NavMeshAgent>();
            }
            if (navAgent != null)
            {
                navAgent.speed = moveSpeed;
                navAgent.angularSpeed = 360f;
                navAgent.acceleration = 8f;
                navAgent.stoppingDistance = 0.5f;
                navAgent.updatePosition = true;
                navAgent.updateRotation = true;
                navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                if (rb != null)
                {
                    rb.isKinematic = true; // Let NavMeshAgent control position
                }
            }
        }

        CalculateMapBounds();

        if (playerTarget == null)
        {
            GameObject xr = GameObject.Find("XR Origin");
            if (xr == null)
            {
                xr = GameObject.Find("[BuildingBlock] Camera Rig");
            }
            if (xr != null)
            {
                playerTarget = xr.transform;
            }
            else
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTarget = player.transform;
                }
            }
        }

        if (projectilePrefab == null)
        {
            EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
            if (spawner != null)
            {
                projectilePrefab = spawner.enemyBallPrefab;
            }
        }
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
        if (playerTarget == null) return;

        Vector3 toPlayer = playerTarget.position - transform.position;
        float distance = toPlayer.magnitude;
        Vector3 toPlayerFlat = toPlayer;
        toPlayerFlat.y = 0f;
        float distanceFlat = toPlayerFlat.magnitude;

        // Movement: NavMesh pathfinding (multi-floor) or direct movement
        if (navAgent != null && navAgent.enabled && useNavMesh)
        {
            if (navAgent.isOnNavMesh)
            {
                if (Time.time >= nextDestinationTime)
                {
                    navAgent.SetDestination(playerTarget.position);
                    nextDestinationTime = Time.time + destinationUpdateInterval;
                }
                navAgent.speed = moveSpeed;
            }
        }
        else if (distanceFlat > 0.01f)
        {
            // Fallback: direct movement (no NavMesh or not on mesh)
            Vector3 direction = toPlayerFlat.normalized;
            Vector3 desiredVelocity = direction * moveSpeed;

            if (rb != null)
            {
                if (isKinematic)
                {
                    Vector3 newPos = transform.position + desiredVelocity * Time.fixedDeltaTime;
                    if (boundsValid)
                    {
                        newPos.x = Mathf.Clamp(newPos.x, boundsMinX, boundsMaxX);
                        newPos.z = Mathf.Clamp(newPos.z, boundsMinZ, boundsMaxZ);
                    }
                    rb.MovePosition(newPos);
                    transform.rotation = Quaternion.LookRotation(direction);
                }
                else
                {
                    Vector3 vel = rb.linearVelocity;
                    vel.x = desiredVelocity.x;
                    vel.z = desiredVelocity.z;
                    rb.linearVelocity = vel;
                    transform.rotation = Quaternion.LookRotation(direction);
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
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        if (boundsValid && (navAgent == null || !navAgent.isOnNavMesh) && rb != null && !rb.isKinematic)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, boundsMinX, boundsMaxX);
            pos.z = Mathf.Clamp(pos.z, boundsMinZ, boundsMaxZ);
            transform.position = pos;
        }

        // Ranged attack when in range and cooldown elapsed (use flat distance for 2D range check)
        if (distanceFlat <= attackRange && (Time.time - lastAttackTime) >= attackCooldown)
        {
            TryRangedAttack(toPlayerFlat.normalized, distanceFlat);
        }
    }

    private void TryRangedAttack(Vector3 directionToPlayer, float distance)
    {
        if (projectilePrefab == null) return;

        lastAttackTime = Time.time;

        Vector3 spawnPos = transform.position + Vector3.up * projectileSpawnHeightOffset;
        Quaternion spawnRot = Quaternion.LookRotation(directionToPlayer);
        GameObject projectile = Instantiate(projectilePrefab, spawnPos, spawnRot);
        projectile.name = "Enemy Ball (Seek)";

        projectile.tag = enemyProjectileTag;
        int layerIndex = LayerMask.NameToLayer(enemyProjectileLayer);
        if (layerIndex >= 0)
        {
            projectile.layer = layerIndex;
        }

        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
        if (projectileRb != null)
        {
            Vector3 velocity = directionToPlayer * projectileSpeed;
            velocity.y = 0f;
            projectileRb.linearVelocity = velocity;
            projectileRb.angularVelocity = Vector3.zero;
            projectileRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        if (projectile.GetComponent<BuildingBlockProjectile>() == null)
        {
            projectile.AddComponent<BuildingBlockProjectile>();
        }
        if (projectile.GetComponent<EnemyProjectileDamage>() == null)
        {
            projectile.AddComponent<EnemyProjectileDamage>();
        }

        DisablePlayerGrabbing(projectile);
    }

    private void DisablePlayerGrabbing(GameObject projectile)
    {
        MonoBehaviour[] allComponents = projectile.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in allComponents)
        {
            if (component == null) continue;
            string typeName = component.GetType().Name.ToLower();
            if ((typeName.Contains("grab") || typeName.Contains("interactable")) &&
                !(component is BuildingBlockProjectile) && !(component is EnemyProjectileDamage))
            {
                Destroy(component);
            }
        }
    }
}
