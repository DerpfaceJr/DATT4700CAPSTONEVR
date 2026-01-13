using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enemy AI that replays exact player movements from previous round
/// Ghost replay system
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("Replay Settings")]
    [Tooltip("Movement data to replay")]
    public List<MovementFrame> movementData;
    
    [Tooltip("Time offset for this enemy (allows multiple enemies to replay same path)")]
    public float timeOffset = 0f;
    
    [Tooltip("Position offset for this enemy")]
    public Vector3 positionOffset = Vector3.zero;
    
    [Tooltip("Use interpolation between frames")]
    public bool useInterpolation = true;
    
    private int currentFrameIndex = 0;
    private float replayStartTime = 0f;
    private bool isReplaying = false;
    private Vector3 startPosition;
    private Rigidbody rb; // Cache Rigidbody for physics-compatible movement
    
    void Start()
    {
        // Cache Rigidbody reference
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }
        
        startPosition = transform.position;
        replayStartTime = Time.time;
        isReplaying = movementData != null && movementData.Count > 0;
        
        if (isReplaying)
        {
            // Set initial position from first frame (only horizontal, let gravity handle Y)
            if (movementData.Count > 0)
            {
                Vector3 initialPos = movementData[0].position + positionOffset;
                // Only apply horizontal position, keep current Y or let gravity handle it
                Vector3 targetPos = new Vector3(initialPos.x, transform.position.y, initialPos.z);
                
                if (rb != null && !rb.isKinematic)
                {
                    rb.MovePosition(targetPos);
                }
                else
                {
                    transform.position = targetPos;
                }
                
                transform.rotation = movementData[0].rotation;
                Debug.Log($"EnemyAI: Started replay with {movementData.Count} frames, starting at position {initialPos}");
            }
        }
        else
        {
            Debug.LogWarning($"EnemyAI: No movement data to replay! movementData is {(movementData == null ? "null" : "empty")}");
        }
    }
    
    void Update()
    {
        if (!isReplaying || movementData == null || movementData.Count == 0)
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
                // Interpolate between current and next frame (horizontal only)
                float frameDelta = nextFrame.timestamp - currentFrame.timestamp;
                float t = frameDelta > 0 ? 
                    (currentReplayTime - currentFrame.timestamp) / frameDelta : 0f;
                t = Mathf.Clamp01(t);
                
                // Interpolate horizontal position only, preserve current Y for gravity
                Vector3 targetPos = Vector3.Lerp(
                    currentFrame.position,
                    nextFrame.position,
                    t
                ) + positionOffset;
                
                // Only apply X, Z coordinates - let gravity handle Y
                Vector3 horizontalPos = new Vector3(targetPos.x, transform.position.y, targetPos.z);
                
                if (rb != null && !rb.isKinematic)
                {
                    rb.MovePosition(horizontalPos);
                }
                else
                {
                    transform.position = horizontalPos;
                }
                
                transform.rotation = Quaternion.Slerp(
                    currentFrame.rotation,
                    nextFrame.rotation,
                    t
                );
            }
            else
            {
                // Use exact frame data (horizontal only)
                Vector3 targetPos = currentFrame.position + positionOffset;
                // Only apply X, Z coordinates - let gravity handle Y
                Vector3 horizontalPos = new Vector3(targetPos.x, transform.position.y, targetPos.z);
                
                if (rb != null && !rb.isKinematic)
                {
                    rb.MovePosition(horizontalPos);
                }
                else
                {
                    transform.position = horizontalPos;
                }
                
                transform.rotation = currentFrame.rotation;
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
        currentFrameIndex = 0;
        
        if (isReplaying && movementData.Count > 0)
        {
            // Set initial position immediately (horizontal only, let gravity handle Y)
            Vector3 initialPos = movementData[0].position + positionOffset;
            Vector3 horizontalPos = new Vector3(initialPos.x, transform.position.y, initialPos.z);
            
            if (rb != null && !rb.isKinematic)
            {
                rb.MovePosition(horizontalPos);
            }
            else
            {
                transform.position = horizontalPos;
            }
            
            transform.rotation = movementData[0].rotation;
            Debug.Log($"EnemyAI.SetMovementData: Set {movementData.Count} frames, starting replay at time {replayStartTime}");
        }
    }
    
    /// <summary>
    /// Reset the replay to start
    /// </summary>
    public void ResetReplay()
    {
        replayStartTime = Time.time;
        currentFrameIndex = 0;
    }
}

