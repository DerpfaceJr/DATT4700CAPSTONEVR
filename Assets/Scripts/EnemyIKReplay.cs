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
    
    [Header("Replay Settings")]
    [Tooltip("Movement data to replay")]
    public List<MovementFrame> movementData;
    
    [Tooltip("Time offset for this enemy (allows multiple enemies to replay same path)")]
    public float timeOffset = 0f;
    
    [Tooltip("Use interpolation between frames")]
    public bool useInterpolation = true;
    
    private float replayStartTime = 0f;
    private bool isReplaying = false;
    private Rigidbody rb; // Cache Rigidbody for physics-based movement
    private float minYPosition = 0f; // Minimum Y position to prevent clipping through floor
    private bool hasEstablishedMinY = false; // Track if we've established the minimum Y
    
    void Start()
    {
        // Cache Rigidbody for physics-based movement
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }
        
        // Calculate minimum Y position based on collider to prevent floor clipping
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            // Minimum Y is where the bottom of the collider should be at ground level (Y=0)
            // Bottom of collider = center.y - (height/2)
            // For character to stand on ground, root Y should be: 0 + (center.y - height/2)
            minYPosition = capsule.center.y - (capsule.height / 2f);
            Debug.Log($"EnemyIKReplay: Set minimum Y position = {minYPosition} (collider center.y={capsule.center.y}, height={capsule.height})");
        }
        else
        {
            minYPosition = 0.1f; // Small default offset
            Debug.LogWarning($"EnemyIKReplay: No CapsuleCollider found, using default min Y = {minYPosition}");
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
            Debug.Log($"EnemyIKReplay on {gameObject.name}: Started replay with {movementData.Count} frames, using recorded Y position with floor constraint. Rigidbody={(rb != null ? "found" : "not found")}");
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
        
        // Establish minimum Y position after landing (detect ground once)
        if (!hasEstablishedMinY)
        {
            EstablishGroundLevel();
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
        }
    }
    
    /// <summary>
    /// Establish ground level after character has landed
    /// </summary>
    private void EstablishGroundLevel()
    {
        // Check if character has landed (low velocity or raycast hit)
        if (rb != null && rb.linearVelocity.magnitude < 0.2f)
        {
            // Use raycast to find actual ground level
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 2f))
            {
                // Update minimum Y to actual ground level detected
                minYPosition = hit.point.y + (minYPosition > 0 ? minYPosition : 0.1f);
                hasEstablishedMinY = true;
                Debug.Log($"EnemyIKReplay: Established ground level at Y={minYPosition} (detected ground at {hit.point.y})");
            }
            else if (transform.position.y < minYPosition + 0.5f)
            {
                // Character is close to expected ground level, accept current position
                minYPosition = Mathf.Max(transform.position.y, minYPosition);
                hasEstablishedMinY = true;
                Debug.Log($"EnemyIKReplay: Established ground level at Y={minYPosition} (from current position)");
            }
        }
    }
    
    /// <summary>
    /// Apply a single frame of movement data
    /// </summary>
    private void ApplyFrame(MovementFrame frame)
    {
        // Apply root transform using the recorded position (includes Y position)
        // Use recorded Y position but constrain to minimum to prevent floor clipping
        Vector3 targetPos = frame.position;
        
        // Constrain Y position to not go below minimum (prevents clipping through floor)
        // Use recorded Y if it's above minimum, otherwise use minimum
        float constrainedY = Mathf.Max(targetPos.y, minYPosition);
        Vector3 constrainedPos = new Vector3(targetPos.x, constrainedY, targetPos.z);
        
        if (rb != null && !rb.isKinematic)
        {
            rb.MovePosition(constrainedPos);
            rb.MoveRotation(frame.rotation);
        }
        else
        {
            // Fallback to direct transform if no Rigidbody or kinematic
            transform.position = constrainedPos;
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
        // Interpolate root transform using recorded positions (includes Y position)
        // Use recorded Y position but constrain to minimum to prevent floor clipping
        Vector3 targetPosition = Vector3.Lerp(current.position, next.position, t);
        Quaternion targetRotation = Quaternion.Slerp(current.rotation, next.rotation, t);
        
        // Constrain Y position to not go below minimum (prevents clipping through floor)
        float constrainedY = Mathf.Max(targetPosition.y, minYPosition);
        Vector3 constrainedPos = new Vector3(targetPosition.x, constrainedY, targetPosition.z);
        
        if (rb != null && !rb.isKinematic)
        {
            rb.MovePosition(constrainedPos);
            rb.MoveRotation(targetRotation);
        }
        else
        {
            // Fallback to direct transform if no Rigidbody or kinematic
            transform.position = constrainedPos;
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
        if (isReplaying && movementData != null && movementData.Count > 0)
        {
            ApplyFrame(movementData[0]);
        }
    }
}

