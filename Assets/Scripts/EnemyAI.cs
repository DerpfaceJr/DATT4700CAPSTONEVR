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
    
    void Start()
    {
        startPosition = transform.position;
        replayStartTime = Time.time;
        isReplaying = movementData != null && movementData.Count > 0;
        
        if (isReplaying)
        {
            // Set initial position from first frame
            if (movementData.Count > 0)
            {
                transform.position = movementData[0].position + positionOffset;
                transform.rotation = movementData[0].rotation;
                Debug.Log($"EnemyAI: Started replay with {movementData.Count} frames, starting at position {movementData[0].position}");
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
                // Interpolate between current and next frame
                float frameDelta = nextFrame.timestamp - currentFrame.timestamp;
                float t = frameDelta > 0 ? 
                    (currentReplayTime - currentFrame.timestamp) / frameDelta : 0f;
                t = Mathf.Clamp01(t);
                
                transform.position = Vector3.Lerp(
                    currentFrame.position,
                    nextFrame.position,
                    t
                ) + positionOffset;
                
                transform.rotation = Quaternion.Slerp(
                    currentFrame.rotation,
                    nextFrame.rotation,
                    t
                );
            }
            else
            {
                // Use exact frame data
                transform.position = currentFrame.position + positionOffset;
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
            // Set initial position immediately
            transform.position = movementData[0].position + positionOffset;
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

