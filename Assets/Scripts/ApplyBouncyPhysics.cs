using UnityEngine;

/// <summary>
/// Script to apply bouncy physics material to balls
/// Attach this to PhysicsBall objects
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class ApplyBouncyPhysics : MonoBehaviour
{
    [Header("Bouncy Physics Settings")]
    [Tooltip("Bounciness value (0-1)")]
    public float bounciness = 0.8f;
    
    [Tooltip("Friction value (0-1)")]
    public float friction = 0.3f;
    
    void Start()
    {
        ApplyBouncyMaterial();
    }
    
    [ContextMenu("Apply Bouncy Material")]
    void ApplyBouncyMaterial()
    {
        SphereCollider collider = GetComponent<SphereCollider>();
        if (collider == null)
        {
            Debug.LogWarning($"No SphereCollider found on {gameObject.name}");
            return;
        }
        
        // Create or get bouncy physics material
        PhysicsMaterial bouncyMaterial = collider.material;
        if (bouncyMaterial == null)
        {
            bouncyMaterial = new PhysicsMaterial("BouncyBall");
        }
        
        // Set bouncy properties
        bouncyMaterial.bounciness = bounciness;
        bouncyMaterial.dynamicFriction = friction;
        bouncyMaterial.staticFriction = friction;
        bouncyMaterial.frictionCombine = PhysicsMaterialCombine.Average;
        bouncyMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;
        
        // Apply to collider
        collider.material = bouncyMaterial;
        
        Debug.Log($"Applied bouncy physics to {gameObject.name}: Bounciness={bounciness}, Friction={friction}");
    }
}

