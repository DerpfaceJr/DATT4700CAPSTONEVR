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
}

