using UnityEngine;

/// <summary>
/// Helper script to automatically configure PhysicsBalls to work like [BuildingBlock] Cube
/// Attach this to any GameObject in the scene and it will configure all PhysicsBalls on Start
/// </summary>
public class SetupBallLikeCube : MonoBehaviour
{
    void Start()
    {
        // Find all PhysicsBall objects
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Untagged");
        
        int configuredCount = 0;
        foreach (GameObject ball in balls)
        {
            // Check if it's a PhysicsBall by name
            if (ball.name.Contains("PhysicsBall"))
            {
                // Check if it already has ConfigureBallLikeCube
                if (ball.GetComponent<ConfigureBallLikeCube>() == null)
                {
                    ConfigureBallLikeCube config = ball.AddComponent<ConfigureBallLikeCube>();
                    config.bounciness = 0.8f;
                    config.friction = 0.3f;
                    config.throwVelocityMultiplier = 1.5f;
                    configuredCount++;
                    Debug.Log($"Added ConfigureBallLikeCube to {ball.name}");
                }
                else
                {
                    Debug.Log($"{ball.name} already has ConfigureBallLikeCube component");
                }
            }
        }
        
        if (configuredCount > 0)
        {
            Debug.Log($"SetupBallLikeCube: Configured {configuredCount} PhysicsBall(s) to work like cube");
        }
        else
        {
            Debug.LogWarning("SetupBallLikeCube: No PhysicsBalls found to configure");
        }
    }
}

