using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Records player position and rotation at fixed intervals
/// Stores movement data per round for enemies to replay
/// </summary>
public class PlayerMovementRecorder : MonoBehaviour
{
    [Header("Recording Settings")]
    [Tooltip("Time interval between recordings (seconds)")]
    public float recordInterval = 0.1f;
    
    [Tooltip("Maximum number of frames to record (0 = unlimited)")]
    public int maxFrames = 0;
    
    private List<MovementFrame> currentRoundMovements = new List<MovementFrame>();
    private float lastRecordTime = 0f;
    private float recordingStartTime = 0f;
    private bool isRecording = false;
    private Transform playerTransform;
    private IKTargetFollowVRRig playerIKRig;
    
    void Start()
    {
        // Find player transform (XR Origin or Camera Rig)
        GameObject xrOrigin = GameObject.Find("XR Origin");
        if (xrOrigin == null)
        {
            xrOrigin = GameObject.Find("[BuildingBlock] Camera Rig");
        }
        
        if (xrOrigin != null)
        {
            playerTransform = xrOrigin.transform;
        }
        else
        {
            // Fallback to camera
            if (Camera.main != null)
            {
                playerTransform = Camera.main.transform;
            }
            else
            {
                playerTransform = transform;
            }
        }
        
        // Find player rigged character by Player tag to get IK rig
        GameObject playerCharacter = GameObject.FindGameObjectWithTag("Player");
        if (playerCharacter != null)
        {
            playerIKRig = playerCharacter.GetComponent<IKTargetFollowVRRig>();
            if (playerIKRig == null)
            {
                // Try finding in children
                playerIKRig = playerCharacter.GetComponentInChildren<IKTargetFollowVRRig>();
            }
            
            if (playerIKRig != null)
            {
                Debug.Log("PlayerMovementRecorder: Found IK rig, will record IK target data");
            }
            else
            {
                Debug.LogWarning("PlayerMovementRecorder: Player tag found but no IKTargetFollowVRRig component. Will record basic movement only.");
            }
        }
        else
        {
            Debug.LogWarning("PlayerMovementRecorder: No GameObject with Player tag found. Will record basic movement only.");
        }
    }
    
    void Update()
    {
        if (isRecording && playerTransform != null)
        {
            if (Time.time - lastRecordTime >= recordInterval)
            {
                RecordFrame();
                lastRecordTime = Time.time;
            }
        }
    }
    
    /// <summary>
    /// Start recording player movements
    /// </summary>
    public void StartRecording()
    {
        isRecording = true;
        currentRoundMovements.Clear();
        recordingStartTime = Time.time;
        lastRecordTime = Time.time;
        RecordFrame(); // Record initial frame
    }
    
    /// <summary>
    /// Stop recording and return the recorded movement data
    /// </summary>
    public List<MovementFrame> StopRecording()
    {
        isRecording = false;
        return new List<MovementFrame>(currentRoundMovements);
    }
    
    /// <summary>
    /// Get the current round's movement data (without stopping recording)
    /// </summary>
    public List<MovementFrame> GetMovementData()
    {
        return new List<MovementFrame>(currentRoundMovements);
    }
    
    /// <summary>
    /// Clear all recorded movement data
    /// </summary>
    public void ClearData()
    {
        currentRoundMovements.Clear();
    }
    
    private void RecordFrame()
    {
        if (playerTransform == null) return;
        
        // Use relative timestamp (time since recording started)
        float relativeTime = Time.time - recordingStartTime;
        
        MovementFrame frame;
        
        // Record IK target data if available
        if (playerIKRig != null && playerIKRig.head.ikTarget != null && 
            playerIKRig.leftHand.ikTarget != null && playerIKRig.rightHand.ikTarget != null)
        {
            // Record with IK data
            frame = new MovementFrame(
                relativeTime,
                playerTransform.position,
                playerTransform.rotation,
                playerIKRig.head.ikTarget.position,
                playerIKRig.head.ikTarget.rotation,
                playerIKRig.leftHand.ikTarget.position,
                playerIKRig.leftHand.ikTarget.rotation,
                playerIKRig.rightHand.ikTarget.position,
                playerIKRig.rightHand.ikTarget.rotation
            );
        }
        else
        {
            // Fallback to basic recording (backward compatible)
            frame = new MovementFrame(
                relativeTime,
                playerTransform.position,
                playerTransform.rotation
            );
        }
        
        currentRoundMovements.Add(frame);
        
        // Limit frames if maxFrames is set
        if (maxFrames > 0 && currentRoundMovements.Count > maxFrames)
        {
            currentRoundMovements.RemoveAt(0);
        }
    }
}

