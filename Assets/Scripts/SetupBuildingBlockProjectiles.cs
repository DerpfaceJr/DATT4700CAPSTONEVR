using UnityEngine;

/// <summary>
/// Automatically adds BuildingBlockProjectile component to all building block objects
/// Attach this to any GameObject in the scene to set up building blocks as projectiles
/// </summary>
public class SetupBuildingBlockProjectiles : MonoBehaviour
{
    [Header("Setup Settings")]
    [Tooltip("Search for objects with 'BuildingBlock' in the name")]
    public bool searchByName = true;
    
    [Tooltip("Search for objects with BuildingBlock component")]
    public bool searchByComponent = true;
    
    void Start()
    {
        int configuredCount = 0;
        
        // Find all GameObjects in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // Apple throwables heal the player — only match [BuildingBlock] apple objects,
            // not scene decoration ("Apple Shelf", "Apples" containers, etc.)
            if (IsAppleThrowable(obj))
            {
                // Apple must NOT have BuildingBlockProjectile (it would conflict with healing)
                BuildingBlockProjectile existingDamage = obj.GetComponent<BuildingBlockProjectile>();
                if (existingDamage != null)
                {
                    Destroy(existingDamage);
                    Debug.Log($"Removed BuildingBlockProjectile from {obj.name} (apple is a healer, not a weapon)");
                }

                // Remove duplicate HealingProjectile if present
                HealingProjectile[] existing = obj.GetComponents<HealingProjectile>();
                if (existing.Length > 1)
                {
                    for (int i = 1; i < existing.Length; i++)
                        Destroy(existing[i]);
                    Debug.Log($"Removed {existing.Length - 1} duplicate HealingProjectile(s) from {obj.name}");
                }

                if (existing.Length == 0)
                {
                    EnsureThrowablePhysics(obj);
                    HealingProjectile healing = obj.AddComponent<HealingProjectile>();
                    healing.healAmount = 10f;
                    healing.minHitVelocity = 1.0f;
                    configuredCount++;
                    Debug.Log($"Added HealingProjectile to {obj.name} (heals player 10 HP on enemy hit)");
                }
                continue;
            }

            bool isBuildingBlock = false;
            
            // Check by name
            if (searchByName && obj.name.Contains("[BuildingBlock]"))
            {
                isBuildingBlock = true;
            }
            
            // Check by component
            if (searchByComponent && !isBuildingBlock)
            {
                #if UNITY_EDITOR || UNITY_ANDROID
                // Check for Meta.XR.BuildingBlocks.BuildingBlock component
                Component buildingBlockComponent = obj.GetComponent("BuildingBlock");
                if (buildingBlockComponent == null)
                {
                    // Try to find by type name
                    System.Type buildingBlockType = null;
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        buildingBlockType = assembly.GetType("Meta.XR.BuildingBlocks.BuildingBlock");
                        if (buildingBlockType != null) break;
                    }
                    if (buildingBlockType != null)
                    {
                        buildingBlockComponent = obj.GetComponent(buildingBlockType);
                    }
                }
                if (buildingBlockComponent != null)
                {
                    isBuildingBlock = true;
                }
                #endif
            }
            
            if (isBuildingBlock)
            {
                // Check if it already has BuildingBlockProjectile
                if (obj.GetComponent<BuildingBlockProjectile>() == null)
                {
                    BuildingBlockProjectile projectile = obj.AddComponent<BuildingBlockProjectile>();
                    projectile.minThrowVelocity = 1.0f;
                    projectile.damagePerHit = 20f;
                    projectile.damageMultiplier = 1.0f;
                    configuredCount++;
                    Debug.Log($"Added BuildingBlockProjectile to {obj.name}");
                }
            }
        }
        
        if (configuredCount > 0)
        {
            Debug.Log($"SetupBuildingBlockProjectiles: Configured {configuredCount} building block object(s) as projectiles");
        }
        else
        {
            Debug.LogWarning("SetupBuildingBlockProjectiles: No building block objects found to configure");
        }
    }

    /// <summary>
    /// Returns true only for apple objects that should be throwable healers.
    /// Matches "[BuildingBlock] apple" prefabs and loose "apple (N)" FBX instances with a Rigidbody.
    /// Excludes containers like "Apple Shelf", "Apples", etc.
    /// </summary>
    private bool IsAppleThrowable(GameObject obj)
    {
        string lower = obj.name.ToLowerInvariant();
        if (!lower.Contains("apple")) return false;

        // Exclude shelf/container objects (e.g. "Apple Shelf", "Apples", "Apples (2)")
        if (lower.Contains("shelf")) return false;
        if (lower.StartsWith("apples")) return false;

        // [BuildingBlock] apple → always a throwable
        if (lower.Contains("[buildingblock]")) return true;

        // Loose "apple" / "apple (1)" FBX instances — only if already set up as throwable (has Rigidbody)
        if (obj.GetComponent<Rigidbody>() != null) return true;

        return false;
    }

    /// <summary>Ensure object has Rigidbody and Collider for throwing (apples from FBX may lack these).</summary>
    private void EnsureThrowablePhysics(GameObject obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.collisionDetectionMode = UnityEngine.CollisionDetectionMode.Continuous;
        }
        Collider col = obj.GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sphere = obj.AddComponent<SphereCollider>();
            sphere.radius = 0.15f;
        }
        else
        {
            col.isTrigger = false;
        }
    }
}

