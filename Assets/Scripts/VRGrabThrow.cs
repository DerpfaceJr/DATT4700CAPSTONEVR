using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// VR Grab and Throw script for Meta Quest controllers
/// Allows picking up and throwing objects in VR
/// Works with Quest 3 controllers and cross-platform (Windows/Mac)
/// Attach this to controller GameObjects (LeftHandAnchor, RightHandAnchor, or Controller objects)
/// </summary>
public class VRGrabThrow : MonoBehaviour
{
    [Header("Grab Settings")]
    [Tooltip("Distance at which objects can be grabbed")]
    public float grabDistance = 0.5f; // Increased for better usability with balls
    
    [Tooltip("Force multiplier for throwing")]
    public float throwForce = 1.0f;
    
    [Tooltip("Layer mask for grabbable objects (leave as Everything to grab all objects)")]
    public LayerMask grabbableLayer = -1;
    
    [Tooltip("Minimum grip pressure to grab (0-1, Quest controllers use analog grip)")]
    [Range(0.1f, 1.0f)]
    public float gripThreshold = 0.5f;
    
    [Header("Controller Settings")]
    [Tooltip("Which hand this controller is for (auto-detected if not set)")]
    public bool isRightHand = true;
    
    [Tooltip("Enable debug logging")]
    public bool debugMode = false;
    
    private Rigidbody grabbedObject = null;
    private Transform grabPoint = null;
    private InputDevice controllerDevice;
    private bool isGrabbing = false;
    private Vector3 previousPosition;
    private Vector3 currentVelocity;
    private Vector3[] velocityHistory = new Vector3[5];
    private int velocityIndex = 0;
    private float lastGripValue = 0f;
    private float lastTriggerValue = 0f;
    private int deviceInitAttempts = 0;
    private const int MAX_DEVICE_INIT_ATTEMPTS = 10;
    
    void Start()
    {
        // Create grab point
        GameObject grabPointObj = new GameObject("GrabPoint");
        grabPointObj.transform.SetParent(transform);
        grabPointObj.transform.localPosition = Vector3.zero;
        grabPoint = grabPointObj.transform;
        
        // Auto-detect which hand based on GameObject name
        string objName = gameObject.name.ToLower();
        if (objName.Contains("left"))
        {
            isRightHand = false;
        }
        else if (objName.Contains("right"))
        {
            isRightHand = true;
        }
        
        // Initialize controller device
        InitializeController();
        
        // Subscribe to device connection events for better controller detection
        InputDevices.deviceConnected += OnDeviceConnected;
        
        // Initialize velocity tracking
        previousPosition = transform.position;
        for (int i = 0; i < velocityHistory.Length; i++)
        {
            velocityHistory[i] = Vector3.zero;
        }
    }
    
    void InitializeController()
    {
        var controllers = new List<InputDevice>();
        InputDeviceCharacteristics characteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller;
        
        // Find controller based on hand preference
        if (isRightHand)
        {
            InputDevices.GetDevicesWithCharacteristics(characteristics | InputDeviceCharacteristics.Right, controllers);
        }
        else
        {
            InputDevices.GetDevicesWithCharacteristics(characteristics | InputDeviceCharacteristics.Left, controllers);
        }
        
        if (controllers.Count > 0)
        {
            controllerDevice = controllers[0];
            if (debugMode)
            {
                Debug.Log($"[VRGrabThrow] Initialized {(isRightHand ? "Right" : "Left")} controller: {controllerDevice.name}");
            }
            deviceInitAttempts = 0;
        }
        else
        {
            deviceInitAttempts++;
            if (deviceInitAttempts < MAX_DEVICE_INIT_ATTEMPTS && debugMode)
            {
                Debug.LogWarning($"[VRGrabThrow] {(isRightHand ? "Right" : "Left")} controller not found. Attempt {deviceInitAttempts}/{MAX_DEVICE_INIT_ATTEMPTS}");
            }
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        InputDevices.deviceConnected -= OnDeviceConnected;
    }
    
    void OnDeviceConnected(InputDevice device)
    {
        // Re-initialize if this is our controller
        if ((device.characteristics & InputDeviceCharacteristics.Controller) != 0)
        {
            bool isRightController = (device.characteristics & InputDeviceCharacteristics.Right) != 0;
            bool isLeftController = (device.characteristics & InputDeviceCharacteristics.Left) != 0;
            
            if ((isRightHand && isRightController) || (!isRightHand && isLeftController))
            {
                controllerDevice = device;
                deviceInitAttempts = 0;
                if (debugMode)
                {
                    Debug.Log($"[VRGrabThrow] {(isRightHand ? "Right" : "Left")} controller connected: {device.name}");
                }
            }
        }
    }
    
    void Update()
    {
        // Re-initialize if device lost (with retry limit and periodic attempts)
        if (!controllerDevice.isValid)
        {
            if (deviceInitAttempts < MAX_DEVICE_INIT_ATTEMPTS)
            {
                // Try to re-initialize every second
                if (Time.frameCount % 60 == 0) // Approximately once per second at 60fps
                {
                    InitializeController();
                }
            }
            else if (deviceInitAttempts == MAX_DEVICE_INIT_ATTEMPTS && debugMode)
            {
                Debug.LogWarning($"[VRGrabThrow] {(isRightHand ? "Right" : "Left")} controller not found after {MAX_DEVICE_INIT_ATTEMPTS} attempts. Enable Debug Mode and check controller connection.");
                deviceInitAttempts++; // Prevent spam
            }
        }
        
        if (!controllerDevice.isValid) return;
        
        // Check for grab input - Quest 3 uses analog grip and trigger
        bool backButtonPressed = false;
        bool primaryButtonPressed = false;
        bool gripButtonPressed = false;
        bool triggerButtonPressed = false;
        float gripValue = 0f;
        float triggerValue = 0f;
        
        // Try to get button states
        controllerDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out backButtonPressed);
        controllerDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButtonPressed);
        controllerDevice.TryGetFeatureValue(CommonUsages.gripButton, out gripButtonPressed);
        controllerDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggerButtonPressed);
        
        // Try to get analog values (Quest 3 controllers support analog grip/trigger)
        controllerDevice.TryGetFeatureValue(CommonUsages.grip, out gripValue);
        controllerDevice.TryGetFeatureValue(CommonUsages.trigger, out triggerValue);
        
        // Use analog values if available, otherwise fall back to boolean
        // Improved: Check if analog value is very small (near zero) and use boolean as fallback
        bool gripActive = gripValue > gripThreshold || (gripButtonPressed && gripValue <= 0.01f);
        bool triggerActive = triggerValue > 0.3f || triggerButtonPressed;
        
        // Primary grab input: Grip button (analog or boolean)
        // Secondary: Trigger or back button
        bool grabInput = gripActive || triggerActive || backButtonPressed;
        
        // Debug logging (only log when button is pressed to avoid spam)
        if (debugMode && (gripActive || triggerActive || backButtonPressed || primaryButtonPressed))
        {
            Debug.Log($"[VRGrabThrow] {(isRightHand ? "Right" : "Left")} - Grip: {gripValue:F2} (thresh: {gripThreshold}), Trigger: {triggerValue:F2}, Back: {backButtonPressed}, Primary: {primaryButtonPressed}");
        }
        
        // Handle grab/release
        if (grabInput && !isGrabbing)
        {
            TryGrab();
        }
        else if (!grabInput && isGrabbing)
        {
            Release();
        }
        
        // Update grabbed object position
        if (isGrabbing && grabbedObject != null)
        {
            UpdateGrabbedObject();
        }
        
        // Calculate velocity for throwing (use average of recent velocities for smoother throwing)
        if (Time.deltaTime > 0)
        {
            Vector3 frameVelocity = (transform.position - previousPosition) / Time.deltaTime;
            velocityHistory[velocityIndex] = frameVelocity;
            velocityIndex = (velocityIndex + 1) % velocityHistory.Length;
            
            // Calculate average velocity
            Vector3 avgVelocity = Vector3.zero;
            for (int i = 0; i < velocityHistory.Length; i++)
            {
                avgVelocity += velocityHistory[i];
            }
            currentVelocity = avgVelocity / velocityHistory.Length;
        }
        
        previousPosition = transform.position;
        lastGripValue = gripValue;
        lastTriggerValue = triggerValue;
    }
    
    void TryGrab()
    {
        // Safety check
        if (grabPoint == null)
        {
            if (debugMode)
            {
                Debug.LogWarning("[VRGrabThrow] Grab point is null!");
            }
            return;
        }
        
        // Use multiple detection methods for better reliability
        Rigidbody targetRb = null;
        float closestDistance = float.MaxValue;
        
        // Method 1: OverlapSphere at grab point (best for close objects like balls)
        Collider[] colliders = Physics.OverlapSphere(grabPoint.position, grabDistance, grabbableLayer);
        
        if (debugMode)
        {
            Debug.Log($"[VRGrabThrow] TryGrab - Position: {grabPoint.position}, Grab Distance: {grabDistance}, Found {colliders.Length} colliders");
        }
        
        // Check all colliders found in sphere
        foreach (Collider col in colliders)
        {
            if (col == null) continue;
            
            Rigidbody rb = col.attachedRigidbody;
            if (rb == null)
            {
                // Try getting from parent if collider doesn't have one
                rb = col.GetComponentInParent<Rigidbody>();
            }
            
            if (rb != null && rb != grabbedObject)
            {
                // Allow grabbing kinematic objects (they'll be made non-kinematic)
                float distance = Vector3.Distance(grabPoint.position, rb.transform.position);
                
                if (debugMode)
                {
                    Debug.Log($"[VRGrabThrow] Found object: {rb.name}, Distance: {distance:F3}, isKinematic: {rb.isKinematic}");
                }
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    targetRb = rb;
                }
            }
        }
        
        // Method 2: Raycast forward if no object found (for reaching out)
        if (targetRb == null)
        {
            RaycastHit[] hits = Physics.RaycastAll(grabPoint.position, transform.forward, grabDistance * 2f, grabbableLayer);
            
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                
                Rigidbody rb = hit.collider.attachedRigidbody;
                if (rb == null)
                {
                    rb = hit.collider.GetComponentInParent<Rigidbody>();
                }
                
                if (rb != null && rb != grabbedObject)
                {
                    float distance = hit.distance;
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        targetRb = rb;
                    }
                }
            }
        }
        
        // Method 3: Try sphere cast for better detection
        if (targetRb == null)
        {
            RaycastHit[] sphereHits = Physics.SphereCastAll(
                grabPoint.position, 
                grabDistance * 0.5f, 
                transform.forward, 
                grabDistance, 
                grabbableLayer
            );
            
            foreach (RaycastHit hit in sphereHits)
            {
                if (hit.collider == null) continue;
                
                Rigidbody rb = hit.collider.attachedRigidbody;
                if (rb == null)
                {
                    rb = hit.collider.GetComponentInParent<Rigidbody>();
                }
                
                if (rb != null && rb != grabbedObject)
                {
                    float distance = hit.distance;
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        targetRb = rb;
                    }
                }
            }
        }
        
        // Grab the found object
        if (targetRb != null)
        {
            if (debugMode)
            {
                Debug.Log($"[VRGrabThrow] Grabbing object: {targetRb.name} at distance {closestDistance:F3}");
            }
            GrabObject(targetRb);
        }
        else if (debugMode)
        {
            Debug.Log($"[VRGrabThrow] No grabbable object found in range");
        }
    }
    
    void GrabObject(Rigidbody rb)
    {
        if (rb == null) return;
        
        if (debugMode)
        {
            Debug.Log($"[VRGrabThrow] GrabObject called for {rb.name}");
        }
        
        grabbedObject = rb;
        isGrabbing = true;
        
        // Store original kinematic state
        bool wasKinematic = grabbedObject.isKinematic;
        
        // Make object kinematic while grabbed for smooth movement
        grabbedObject.isKinematic = true;
        grabbedObject.transform.SetParent(grabPoint);
        grabbedObject.transform.localPosition = Vector3.zero;
        grabbedObject.transform.localRotation = Quaternion.identity;
        
        // Stop any existing velocity
        #if UNITY_2023_1_OR_NEWER
        if (grabbedObject.linearVelocity != Vector3.zero)
        {
            grabbedObject.linearVelocity = Vector3.zero;
        }
        #else
        if (grabbedObject.velocity != Vector3.zero)
        {
            grabbedObject.velocity = Vector3.zero;
        }
        #endif
        if (grabbedObject.angularVelocity != Vector3.zero)
        {
            grabbedObject.angularVelocity = Vector3.zero;
        }
        
        if (debugMode)
        {
            Debug.Log($"[VRGrabThrow] Successfully grabbed {rb.name}");
        }
        
        // Reset velocity tracking
        for (int i = 0; i < velocityHistory.Length; i++)
        {
            velocityHistory[i] = Vector3.zero;
        }
    }
    
    void UpdateGrabbedObject()
    {
        if (grabbedObject == null) return;
        
        // Keep object at grab point with smooth following
        grabbedObject.transform.position = grabPoint.position;
        grabbedObject.transform.rotation = grabPoint.rotation;
    }
    
    void Release()
    {
        if (grabbedObject == null) return;
        
        Rigidbody rbToRelease = grabbedObject;
        
        // Restore physics before applying velocity
        rbToRelease.isKinematic = false;
        rbToRelease.transform.SetParent(null);
        
        // Calculate throw velocity based on controller movement
        Vector3 throwVelocity = currentVelocity * throwForce;
        
        // Apply throw velocity (use linearVelocity for Unity 2023+, velocity for older versions)
        #if UNITY_2023_1_OR_NEWER
        rbToRelease.linearVelocity = throwVelocity;
        #else
        rbToRelease.velocity = throwVelocity;
        #endif
        
        // Add some angular velocity for realistic spin (based on throw direction)
        Vector3 spinAxis = Vector3.Cross(transform.forward, throwVelocity.normalized);
        if (spinAxis.magnitude > 0.1f)
        {
            rbToRelease.angularVelocity = spinAxis.normalized * throwVelocity.magnitude * 0.5f;
        }
        else
        {
            // Fallback to random spin if no clear axis
            rbToRelease.angularVelocity = Random.insideUnitSphere * Mathf.Min(throwVelocity.magnitude * 0.3f, 10f);
        }
        
        if (debugMode)
        {
            Debug.Log($"[VRGrabThrow] Released {rbToRelease.name} with velocity: {throwVelocity.magnitude:F2} m/s");
        }
        
        grabbedObject = null;
        isGrabbing = false;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw grab range
        Gizmos.color = Color.yellow;
        
        // Draw sphere at grab point
        Vector3 grabPos = grabPoint != null ? grabPoint.position : transform.position;
        Gizmos.DrawWireSphere(grabPos, grabDistance);
        
        // Draw forward ray
        Gizmos.DrawRay(grabPos, transform.forward * grabDistance * 2f);
        
        // Draw line to show grab point
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, grabPos);
    }
}
