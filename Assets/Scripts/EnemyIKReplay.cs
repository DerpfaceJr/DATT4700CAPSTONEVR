using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Replays rigged character animations for enemies based on recorded IK data
/// Applies both root transform and IK target positions/rotations
/// </summary>
public class EnemyIKReplay : MonoBehaviour
{
    [Header("IK References")]
    [Tooltip("IK rig component on this enemy character")]
    public IKTargetFollowVRRig ikRig;
    
    [Header("Character Positioning")]
    [Tooltip("Y position for character hips/root (adjustable for height calibration)")]
    public float hipHeight = 1.0f;
    
    [Header("Replay Settings")]
    [Tooltip("Movement data to replay")]
    public List<MovementFrame> movementData;
    
    [Tooltip("Time offset for this enemy (allows multiple enemies to replay same path)")]
    public float timeOffset = 0f;
    
    [Tooltip("Use interpolation between frames")]
    public bool useInterpolation = true;
    
    [Header("Projectile Replay")]
    [Tooltip("Enemy Ball prefab for spawning thrown objects")]
    public GameObject enemyBallPrefab;
    
    [Tooltip("Material for shadow projectiles (optional - will be gray if not set)")]
    public Material shadowProjectileMaterial;
    
    [Tooltip("Tag for enemy-thrown projectiles")]
    public string enemyProjectileTag = "EnemyProjectile";
    
    [Tooltip("Layer for enemy projectiles (to prevent player pickup)")]
    public string enemyProjectileLayer = "EnemyProjectile";
    
    private float replayStartTime = 0f;
    private bool isReplaying = false;
    private Rigidbody rb; // Cache Rigidbody for physics-based movement
    private HashSet<float> processedThrowTimestamps = new HashSet<float>(); // Track which throws we've already processed
    
    void Start()
    {
        // Cache Rigidbody for physics-based movement
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }
        
        // Find IK rig if not assigned
        if (ikRig == null)
        {
            ikRig = GetComponent<IKTargetFollowVRRig>();
            if (ikRig == null)
            {
                ikRig = GetComponentInChildren<IKTargetFollowVRRig>();
            }
        }
        
        if (ikRig == null)
        {
            Debug.LogWarning($"EnemyIKReplay on {gameObject.name}: No IKTargetFollowVRRig component found! IK replay will not work.");
        }
        
        replayStartTime = Time.time;
        isReplaying = movementData != null && movementData.Count > 0;
        
        if (isReplaying && movementData.Count > 0)
        {
            ApplyFrame(movementData[0]);
            Debug.Log($"EnemyIKReplay on {gameObject.name}: Started replay with {movementData.Count} frames, hip height={hipHeight}. Rigidbody={(rb != null ? "found" : "not found")}");
        }
        else
        {
            Debug.LogWarning($"EnemyIKReplay on {gameObject.name}: No movement data to replay! movementData is {(movementData == null ? "null" : "empty")}");
        }
    }
    
    void Update()
    {
        if (!isReplaying || movementData == null || movementData.Count == 0)
        {
            return;
        }
        
        if (ikRig == null)
        {
            return;
        }
        
        // Calculate current replay time (relative time since replay started)
        float currentReplayTime = (Time.time - replayStartTime) + timeOffset;
        
        // Find the appropriate frame(s) to use
        MovementFrame currentFrame = null;
        MovementFrame nextFrame = null;
        
        for (int i = 0; i < movementData.Count; i++)
        {
            if (movementData[i].timestamp <= currentReplayTime)
            {
                currentFrame = movementData[i];
                if (i + 1 < movementData.Count)
                {
                    nextFrame = movementData[i + 1];
                }
            }
            else
            {
                break;
            }
        }
        
        // If we've reached the end, loop back to start
        if (currentFrame == null && movementData.Count > 0)
        {
            // Loop back to beginning
            replayStartTime = Time.time;
            currentReplayTime = timeOffset;
            currentFrame = movementData[0];
            if (movementData.Count > 1)
            {
                nextFrame = movementData[1];
            }
        }
        
        if (currentFrame != null)
        {
            if (useInterpolation && nextFrame != null)
            {
                // Interpolate between current and next frame
                float frameDelta = nextFrame.timestamp - currentFrame.timestamp;
                float t = frameDelta > 0 ? 
                    (currentReplayTime - currentFrame.timestamp) / frameDelta : 0f;
                t = Mathf.Clamp01(t);
                
                ApplyFrameInterpolated(currentFrame, nextFrame, t);
            }
            else
            {
                ApplyFrame(currentFrame);
            }
            
            // Check for throw events in current frame
            if (currentFrame.hasThrowEvent && !processedThrowTimestamps.Contains(currentFrame.timestamp))
            {
                ProcessThrowEvent(currentFrame);
                processedThrowTimestamps.Add(currentFrame.timestamp);
            }
        }
    }
    
    
    /// <summary>
    /// Apply a single frame of movement data
    /// </summary>
    private void ApplyFrame(MovementFrame frame)
    {
        // Apply root transform using the recorded position
        // Override Y position with hipHeight for consistent character height
        Vector3 targetPos = frame.position;
        targetPos.y = hipHeight; // Use adjustable hip height instead of recorded Y
        
        // No Y constraint - let IK system handle leg positions naturally
        if (rb != null && !rb.isKinematic)
        {
            rb.MovePosition(targetPos);
            rb.MoveRotation(frame.rotation);
        }
        else
        {
            // Fallback to direct transform if no Rigidbody or kinematic
            transform.position = targetPos;
            transform.rotation = frame.rotation;
        }
        
        // Apply IK targets (if IK data exists - check if not default values)
        if (ikRig != null && ikRig.head.ikTarget != null && 
            ikRig.leftHand.ikTarget != null && ikRig.rightHand.ikTarget != null)
        {
            // Check if IK data was recorded (not default zero/identity values)
            bool hasIKData = frame.headPosition != Vector3.zero || 
                            frame.headRotation != Quaternion.identity ||
                            frame.leftHandPosition != Vector3.zero ||
                            frame.leftHandRotation != Quaternion.identity ||
                            frame.rightHandPosition != Vector3.zero ||
                            frame.rightHandRotation != Quaternion.identity;
            
            if (hasIKData)
            {
                ikRig.head.ikTarget.position = frame.headPosition;
                ikRig.head.ikTarget.rotation = frame.headRotation;
                
                ikRig.leftHand.ikTarget.position = frame.leftHandPosition;
                ikRig.leftHand.ikTarget.rotation = frame.leftHandRotation;
                
                ikRig.rightHand.ikTarget.position = frame.rightHandPosition;
                ikRig.rightHand.ikTarget.rotation = frame.rightHandRotation;
            }
        }
    }
    
    /// <summary>
    /// Apply interpolated frame data between two frames
    /// </summary>
    private void ApplyFrameInterpolated(MovementFrame current, MovementFrame next, float t)
    {
        // Interpolate root transform using recorded positions
        // Override Y position with hipHeight for consistent character height
        Vector3 targetPosition = Vector3.Lerp(current.position, next.position, t);
        targetPosition.y = hipHeight; // Use adjustable hip height instead of recorded Y
        
        Quaternion targetRotation = Quaternion.Slerp(current.rotation, next.rotation, t);
        
        // No Y constraint - let IK system handle leg positions naturally
        if (rb != null && !rb.isKinematic)
        {
            rb.MovePosition(targetPosition);
            rb.MoveRotation(targetRotation);
        }
        else
        {
            // Fallback to direct transform if no Rigidbody or kinematic
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
        
        // Interpolate IK targets (if IK data exists)
        if (ikRig != null && ikRig.head.ikTarget != null && 
            ikRig.leftHand.ikTarget != null && ikRig.rightHand.ikTarget != null)
        {
            // Check if IK data was recorded
            bool hasIKData = current.headPosition != Vector3.zero || 
                            current.headRotation != Quaternion.identity ||
                            current.leftHandPosition != Vector3.zero ||
                            current.leftHandRotation != Quaternion.identity ||
                            current.rightHandPosition != Vector3.zero ||
                            current.rightHandRotation != Quaternion.identity;
            
            if (hasIKData)
            {
                ikRig.head.ikTarget.position = Vector3.Lerp(current.headPosition, next.headPosition, t);
                ikRig.head.ikTarget.rotation = Quaternion.Slerp(current.headRotation, next.headRotation, t);
                
                ikRig.leftHand.ikTarget.position = Vector3.Lerp(current.leftHandPosition, next.leftHandPosition, t);
                ikRig.leftHand.ikTarget.rotation = Quaternion.Slerp(current.leftHandRotation, next.leftHandRotation, t);
                
                ikRig.rightHand.ikTarget.position = Vector3.Lerp(current.rightHandPosition, next.rightHandPosition, t);
                ikRig.rightHand.ikTarget.rotation = Quaternion.Slerp(current.rightHandRotation, next.rightHandRotation, t);
            }
        }
    }
    
    /// <summary>
    /// Set the movement data to replay
    /// </summary>
    public void SetMovementData(List<MovementFrame> data)
    {
        movementData = data;
        isReplaying = data != null && data.Count > 0;
        replayStartTime = Time.time;
        
        if (isReplaying && movementData.Count > 0)
        {
            ApplyFrame(movementData[0]);
            Debug.Log($"EnemyIKReplay.SetMovementData on {gameObject.name}: Set {movementData.Count} frames, starting replay at time {replayStartTime}");
        }
    }
    
    /// <summary>
    /// Reset the replay to start
    /// </summary>
    public void ResetReplay()
    {
        replayStartTime = Time.time;
        processedThrowTimestamps.Clear(); // Reset processed throws
        if (isReplaying && movementData != null && movementData.Count > 0)
        {
            ApplyFrame(movementData[0]);
        }
    }
    
    /// <summary>
    /// Process a throw event from recorded movement data
    /// </summary>
    private void ProcessThrowEvent(MovementFrame frame)
    {
        if (string.IsNullOrEmpty(frame.thrownObjectPrefabName))
        {
            Debug.LogWarning($"EnemyIKReplay: Throw event has no prefab name at time {frame.timestamp}");
            return;
        }
        
        // Always use Enemy Ball prefab for shadow projectiles
        if (enemyBallPrefab == null)
        {
            Debug.LogError($"EnemyIKReplay: Enemy Ball prefab not assigned! Cannot spawn projectile.");
            return;
        }
        
        // Spawn and throw the shadow projectile
        SpawnAndThrowProjectile(enemyBallPrefab, frame);
    }
    
    /// <summary>
    /// Find a throwable prefab from the pool by name (DEPRECATED - now uses Enemy Ball only)
    /// </summary>
    private GameObject FindThrowablePrefab(string prefabName)
    {
        // Always return Enemy Ball prefab
        return enemyBallPrefab;
    }
    
    /// <summary>
    /// Spawn a new projectile and throw it (shadow version that can't be picked up)
    /// </summary>
    private void SpawnAndThrowProjectile(GameObject prefab, MovementFrame frame)
    {
        // Instantiate the shadow projectile (Enemy Ball)
        GameObject projectile = Instantiate(prefab, frame.throwPosition, frame.throwRotation);
        projectile.name = "Enemy Ball (Shadow)";
        
        // Tag it as enemy projectile
        projectile.tag = enemyProjectileTag;
        
        // Set layer to prevent player pickup (if layer exists)
        int enemyProjectileLayerIndex = LayerMask.NameToLayer(enemyProjectileLayer);
        if (enemyProjectileLayerIndex >= 0)
        {
            projectile.layer = enemyProjectileLayerIndex;
        }
        
        // Make it visually distinct (shadow appearance)
        MakeShadowProjectile(projectile);
        
        // Remove or disable any grab components to prevent pickup
        DisablePlayerGrabbing(projectile);
        
        // Apply throw velocity
        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
        if (projectileRb != null)
        {
            projectileRb.linearVelocity = frame.throwVelocity;
            projectileRb.angularVelocity = Vector3.zero; // Reset rotation for clean throw
        }
        
        // Ensure it has BuildingBlockProjectile component for damage
        BuildingBlockProjectile projectileScript = projectile.GetComponent<BuildingBlockProjectile>();
        if (projectileScript == null)
        {
            projectileScript = projectile.AddComponent<BuildingBlockProjectile>();
        }
        
        // Make it able to hurt the player
        MakeProjectileHurtPlayer(projectile);
        
        Debug.Log($"EnemyIKReplay: Enemy threw shadow projectile from {frame.throwPosition} with velocity {frame.throwVelocity.magnitude:F2} m/s");
    }
    
    /// <summary>
    /// Make projectile appear as a shadow (visual feedback)
    /// </summary>
    private void MakeShadowProjectile(GameObject projectile)
    {
        // Get all renderers on the projectile
        Renderer[] renderers = projectile.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            if (shadowProjectileMaterial != null)
            {
                // Use custom shadow material if provided
                renderer.material = shadowProjectileMaterial;
            }
            else
            {
                // Default: make it semi-transparent dark gray
                Material shadowMat = new Material(renderer.material);
                shadowMat.color = new Color(0.3f, 0.3f, 0.3f, 0.7f); // Dark gray, slightly transparent
                
                // Try to enable transparency
                if (shadowMat.HasProperty("_Mode"))
                {
                    shadowMat.SetFloat("_Mode", 3); // Transparent mode
                    shadowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    shadowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    shadowMat.SetInt("_ZWrite", 0);
                    shadowMat.DisableKeyword("_ALPHATEST_ON");
                    shadowMat.EnableKeyword("_ALPHABLEND_ON");
                    shadowMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    shadowMat.renderQueue = 3000;
                }
                
                renderer.material = shadowMat;
            }
        }
    }
    
    /// <summary>
    /// Disable any grabbing/interaction components to prevent player pickup
    /// </summary>
    private void DisablePlayerGrabbing(GameObject projectile)
    {
        // Remove XR grab interactables (if using Unity XR)
        #if UNITY_XR
        var xrGrabInteractable = projectile.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable>();
        if (xrGrabInteractable != null)
        {
            Destroy(xrGrabInteractable);
        }
        #endif
        
        // Remove Oculus/Meta Grabbable (if using Meta XR SDK)
        #if UNITY_EDITOR || UNITY_ANDROID
        var oculusGrabbable = projectile.GetComponent<Oculus.Interaction.Grabbable>();
        if (oculusGrabbable != null)
        {
            Destroy(oculusGrabbable);
        }
        
        var oculusGrabInteractable = projectile.GetComponent<Oculus.Interaction.GrabInteractable>();
        if (oculusGrabInteractable != null)
        {
            Destroy(oculusGrabInteractable);
        }
        #endif
        
        // Generic: look for any component with "Grab" or "Interactable" in the name
        MonoBehaviour[] allComponents = projectile.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in allComponents)
        {
            if (component == null) continue;
            
            string typeName = component.GetType().Name.ToLower();
            if (typeName.Contains("grab") || typeName.Contains("interactable"))
            {
                // Don't destroy BuildingBlockProjectile or EnemyProjectileDamage
                if (!(component is BuildingBlockProjectile) && !(component is EnemyProjectileDamage))
                {
                    Destroy(component);
                }
            }
        }
        
        Debug.Log($"DisablePlayerGrabbing: Removed grab components from {projectile.name}");
    }
    
    /// <summary>
    /// Throw an existing object from the scene (DEPRECATED - now always spawns new)
    /// </summary>
    private void ThrowExistingObject(GameObject obj, MovementFrame frame)
    {
        // No longer used - always spawn new shadow projectiles
        Debug.LogWarning("EnemyIKReplay: ThrowExistingObject called but is deprecated");
    }
    
    /// <summary>
    /// Configure projectile to hurt the player
    /// </summary>
    private void MakeProjectileHurtPlayer(GameObject projectile)
    {
        // Add or get CombatSystem component to detect hits on player
        EnemyProjectileDamage damageScript = projectile.GetComponent<EnemyProjectileDamage>();
        if (damageScript == null)
        {
            damageScript = projectile.AddComponent<EnemyProjectileDamage>();
        }
        
        // Ensure physics layers allow collision with player
        // Layer 0 = Default (most objects), should collide with player
        projectile.layer = 0;
    }
}

