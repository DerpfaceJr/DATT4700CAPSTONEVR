using UnityEngine;
#if UNITY_EDITOR || UNITY_ANDROID
using Oculus.Interaction;
#endif

/// <summary>
/// Configures PhysicsBalls to work like [BuildingBlock] Cube using Oculus Interaction system
/// This ensures balls can be grabbed with controllers and thrown with physics
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ConfigureBallLikeCube : MonoBehaviour
{
    [Header("Physics Settings")]
    [Tooltip("Bounciness for the ball (0-1)")]
    public float bounciness = 0.8f;
    
    [Tooltip("Friction for the ball (0-1)")]
    public float friction = 0.3f;
    
    [Tooltip("Throw velocity multiplier")]
    public float throwVelocityMultiplier = 1.5f;
    
    private Rigidbody rb;
    private Vector3 lastPosition;
    private Vector3 throwVelocity;
    private bool wasGrabbed = false;
    
    #if UNITY_EDITOR || UNITY_ANDROID
    private Grabbable grabbable;
    #endif
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"[ConfigureBallLikeCube] No Rigidbody found on {gameObject.name}");
            return;
        }
        
        // Configure Rigidbody like cube (but keep dynamic for physics)
        rb.useGravity = true;
        rb.isKinematic = false; // Balls should be dynamic for physics
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better collision detection for fast-moving objects
        
        // Apply bouncy physics material
        ApplyBouncyMaterial();
        
        // Configure Oculus Interaction Grabbable
        #if UNITY_EDITOR || UNITY_ANDROID
        ConfigureGrabbable();
        #endif
        
        lastPosition = transform.position;
    }
    
    void ApplyBouncyMaterial()
    {
        SphereCollider collider = GetComponent<SphereCollider>();
        if (collider == null)
        {
            Debug.LogWarning($"[ConfigureBallLikeCube] No SphereCollider found on {gameObject.name}");
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
        
        Debug.Log($"[ConfigureBallLikeCube] Applied bouncy physics to {gameObject.name}: Bounciness={bounciness}, Friction={friction}");
    }
    
    #if UNITY_EDITOR || UNITY_ANDROID
    void ConfigureGrabbable()
    {
        grabbable = GetComponent<Grabbable>();
        if (grabbable == null)
        {
            Debug.LogWarning($"[ConfigureBallLikeCube] No Grabbable component found on {gameObject.name}. Adding one...");
            grabbable = gameObject.AddComponent<Grabbable>();
        }
        
        // Configure Grabbable to work like cube
        // Use reflection to set properties that match the cube's configuration
        try
        {
            // Set kinematic while selected (like cube)
            var kinematicField = grabbable.GetType().GetField("_kinematicWhileSelected", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (kinematicField != null)
            {
                kinematicField.SetValue(grabbable, true);
            }
            
            // Enable throw when unselected (like cube)
            var throwField = grabbable.GetType().GetField("_throwWhenUnselected", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (throwField != null)
            {
                throwField.SetValue(grabbable, true);
            }
            
            Debug.Log($"[ConfigureBallLikeCube] Configured Grabbable on {gameObject.name} to work like cube");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ConfigureBallLikeCube] Could not configure Grabbable properties: {ex.Message}");
        }
    }
    #endif
    
    void Update()
    {
        if (rb == null) return;
        
        #if UNITY_EDITOR || UNITY_ANDROID
        // Track if object is being grabbed
        bool isGrabbed = false;
        if (grabbable != null)
        {
            try
            {
                var pointsProperty = grabbable.GetType().GetProperty("Points");
                if (pointsProperty != null)
                {
                    var points = pointsProperty.GetValue(grabbable);
                    if (points != null)
                    {
                        var countProperty = points.GetType().GetProperty("Count");
                        if (countProperty != null)
                        {
                            int pointCount = (int)countProperty.GetValue(points);
                            isGrabbed = pointCount > 0;
                        }
                    }
                }
            }
            catch (System.Exception) { }
        }
        
        // When released, apply throw velocity
        if (wasGrabbed && !isGrabbed)
        {
            ApplyThrowVelocity();
        }
        
        wasGrabbed = isGrabbed;
        #endif
        
        // Track velocity for throwing
        if (rb.isKinematic)
        {
            // Calculate velocity while kinematic (being grabbed)
            throwVelocity = (transform.position - lastPosition) / Time.deltaTime;
        }
        
        lastPosition = transform.position;
    }
    
    void ApplyThrowVelocity()
    {
        if (rb == null || rb.isKinematic) return;
        
        // Apply throw velocity with multiplier
        Vector3 finalVelocity = throwVelocity * throwVelocityMultiplier;
        rb.linearVelocity = finalVelocity;
        
        // Add some angular velocity for realistic spinning
        rb.angularVelocity = new Vector3(
            Random.Range(-5f, 5f),
            Random.Range(-5f, 5f),
            Random.Range(-5f, 5f)
        );
        
        Debug.Log($"[ConfigureBallLikeCube] Applied throw velocity to {gameObject.name}: {finalVelocity.magnitude:F2} m/s");
    }
}

