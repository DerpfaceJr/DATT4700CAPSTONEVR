using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// VR Movement Controller that enables smooth locomotion using thumbstick
/// Works with Meta Quest controllers
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class VRMovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed in units per second")]
    public float moveSpeed = 2f;
    
    [Tooltip("Enable smooth movement with thumbstick")]
    public bool enableSmoothMovement = true;
    
    [Tooltip("Enable snap turning")]
    public bool enableSnapTurn = true;
    
    [Tooltip("Snap turn angle in degrees")]
    public float snapTurnAngle = 45f;
    
    [Header("References")]
    [Tooltip("XR Origin/Camera Rig transform")]
    public Transform xrOrigin;
    
    private CharacterController characterController;
    private Vector2 leftThumbstick;
    private Vector2 rightThumbstick;
    private float gravity = -9.81f;
    private Vector3 velocity;
    private InputDevice leftDevice;
    private InputDevice rightDevice;
    private float snapTurnCooldown = 0f;
    private const float SNAP_TURN_COOLDOWN_TIME = 0.3f;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        
        // Find XR Origin if not assigned
        if (xrOrigin == null)
        {
            GameObject xrOriginObj = GameObject.Find("XR Origin");
            if (xrOriginObj == null)
            {
                xrOriginObj = GameObject.Find("[BuildingBlock] Camera Rig");
            }
            if (xrOriginObj != null)
            {
                xrOrigin = xrOriginObj.transform;
            }
        }
        
        // Initialize input devices
        InitializeInputDevices();
    }
    
    void InitializeInputDevices()
    {
        var leftHandedControllers = new List<InputDevice>();
        var desiredCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
        InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, leftHandedControllers);
        
        if (leftHandedControllers.Count > 0)
        {
            leftDevice = leftHandedControllers[0];
        }
        
        var rightHandedControllers = new List<InputDevice>();
        desiredCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
        InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, rightHandedControllers);
        
        if (rightHandedControllers.Count > 0)
        {
            rightDevice = rightHandedControllers[0];
        }
    }
    
    void Update()
    {
        if (xrOrigin == null) return;
        
        // Re-initialize devices if lost
        if (!leftDevice.isValid || !rightDevice.isValid)
        {
            InitializeInputDevices();
        }
        
        // Update snap turn cooldown
        if (snapTurnCooldown > 0)
        {
            snapTurnCooldown -= Time.deltaTime;
        }
        
        // Read thumbstick input
        if (enableSmoothMovement)
        {
            ReadThumbstickInput();
            ApplyMovement();
        }
        
        // Apply gravity
        ApplyGravity();
    }
    
    void ReadThumbstickInput()
    {
        // Read left thumbstick for movement
        if (leftDevice.isValid)
        {
            leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftThumbstick);
        }
        
        // Read right thumbstick for snap turning
        if (enableSnapTurn && rightDevice.isValid && snapTurnCooldown <= 0)
        {
            rightDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightThumbstick);
            
            // Snap turn right
            if (rightThumbstick.x > 0.7f)
            {
                SnapTurn(snapTurnAngle);
                snapTurnCooldown = SNAP_TURN_COOLDOWN_TIME;
            }
            // Snap turn left
            else if (rightThumbstick.x < -0.7f)
            {
                SnapTurn(-snapTurnAngle);
                snapTurnCooldown = SNAP_TURN_COOLDOWN_TIME;
            }
        }
    }
    
    void ApplyMovement()
    {
        if (characterController == null || xrOrigin == null) return;
        
        // Get forward direction from head/camera
        Transform headTransform = Camera.main?.transform ?? xrOrigin;
        Vector3 forward = headTransform.forward;
        Vector3 right = headTransform.right;
        
        // Project forward and right onto horizontal plane
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        // Calculate movement direction
        Vector3 moveDirection = (forward * leftThumbstick.y + right * leftThumbstick.x) * moveSpeed;
        
        // Apply movement
        characterController.Move(moveDirection * Time.deltaTime);
    }
    
    void ApplyGravity()
    {
        if (characterController == null) return;
        
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
        
        characterController.Move(velocity * Time.deltaTime);
    }
    
    void SnapTurn(float angle)
    {
        if (xrOrigin == null) return;
        
        xrOrigin.Rotate(0, angle, 0);
    }
}

