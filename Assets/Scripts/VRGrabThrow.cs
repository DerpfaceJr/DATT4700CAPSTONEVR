using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_ANDROID
using Oculus.Interaction;
#endif

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
    public float grabDistance = 3.0f; // Increased for better usability with balls
    
    [Tooltip("Force multiplier for throwing")]
    public float throwForce = 1.0f;
    
    [Tooltip("Layer mask for grabbable objects (leave as Everything to grab all objects)")]
    public LayerMask grabbableLayer = -1;
    
    [Tooltip("Minimum grip pressure to grab (0-1, Quest controllers use analog grip)")]
    [Range(0.1f, 1.0f)]
    public float gripThreshold = 0.3f; // Lowered for easier grabbing
    
    [Header("Controller Settings")]
    [Tooltip("Which hand this controller is for (auto-detected if not set)")]
    public bool isRightHand = true;
    
    [Tooltip("Enable debug logging")]
    public bool debugMode = false;
    
    [Header("Forward Throw Settings")]
    [Tooltip("Enable forward-only throwing (prevents sideways/backward throws)")]
    public bool forwardThrowEnabled = true;
    
    [Tooltip("Minimum forward speed (prevents weak/backward throws)")]
    public float minForwardSpeed = 0.3f;
    
    [Tooltip("Side movement filter (0-1, higher = more filtering, 1.0 = forward only)")]
    [Range(0f, 1f)]
    public float sideMovementFilter = 0.9f;
    
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
            deviceInitAttempts = 0;
            // Always log controller initialization for debugging
            Debug.Log($"[VRGrabThrow] ✓ Initialized {(isRightHand ? "Right" : "Left")} controller: {controllerDevice.name}");
        }
        else
        {
            deviceInitAttempts++;
            // Always log warnings about missing controllers (first 3 attempts only)
            if (deviceInitAttempts <= 3)
            {
                Debug.LogWarning($"[VRGrabThrow] ✗ {(isRightHand ? "Right" : "Left")} controller not found. Attempt {deviceInitAttempts}/3");
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
        
        // Always log button presses when they're pressed (not just in debug mode) to help troubleshoot
        if (gripActive || triggerActive || backButtonPressed || primaryButtonPressed)
        {
            Debug.Log($"[VRGrabThrow] {(isRightHand ? "Right" : "Left")} - Grip: {gripValue:F2} (thresh: {gripThreshold}), Trigger: {triggerValue:F2}, Back: {backButtonPressed}, Primary: {primaryButtonPressed}, GrabInput: {grabInput}");
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
    
    /// <summary>
    /// Check if an object uses Oculus Interaction Grabbable and should be handled by Oculus system
    /// </summary>
    bool ShouldSkipOculusGrabbable(Rigidbody rb)
    {
        if (rb == null) return false;
        
        #if UNITY_EDITOR || UNITY_ANDROID
        try
        {
            Component grabbable = rb.GetComponent("Grabbable");
            if (grabbable == null)
            {
                // Search for Grabbable type
                System.Type grabbableType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    grabbableType = assembly.GetType("Oculus.Interaction.Grabbable");
                    if (grabbableType != null) break;
                }
                if (grabbableType != null)
                {
                    grabbable = rb.GetComponent(grabbableType);
                }
            }
            
            // If object has Grabbable component, let Oculus Interaction system handle it
            if (grabbable != null)
            {
                return true;
            }
        }
        catch (System.Exception) { }
        #endif
        
        return false;
    }
    
    /// <summary>
    /// Check if an object should be skipped (controller-related objects)
    /// </summary>
    bool ShouldSkipObject(Rigidbody rb)
    {
        if (rb == null) return true;
        
        string objName = rb.name;
        
        // Skip controller-related objects
        if (objName.Contains("ControllerGrabLocation") ||
            objName.Contains("PlayerController") ||
            objName.Contains("HandAnchor") ||
            objName.Contains("Camera Rig") ||
            objName.Contains("TrackingSpace") ||
            objName.Contains("OVRCameraRig"))
        {
            return true;
        }
        
        // Skip if it's a child of the controller
        Transform parent = rb.transform.parent;
        while (parent != null)
        {
            if (parent.name.Contains("HandAnchor") || 
                parent.name.Contains("Controller") ||
                parent.name.Contains("Camera Rig"))
            {
                return true;
            }
            parent = parent.parent;
        }
        
        return false;
    }
    
    void TryGrab()
    {
        // Safety check
        if (grabPoint == null)
        {
            Debug.LogError("[VRGrabThrow] Grab point is null! This should not happen.");
            return;
        }
        
        // Use multiple detection methods for better reliability
        Rigidbody targetRb = null;
        float closestDistance = float.MaxValue;
        
        // Use larger radius for better detection (declare once and reuse)
        float detectionRadius = Mathf.Max(grabDistance, 2.0f);
        
        // Method 1: OverlapSphere at grab point (best for close objects like balls)
        Collider[] colliders = Physics.OverlapSphere(grabPoint.position, detectionRadius, grabbableLayer);
        
        // Always log grab attempts to help debug
        Debug.Log($"[VRGrabThrow] TryGrab - Position: {grabPoint.position}, Detection Radius: {detectionRadius}, Found {colliders.Length} colliders");
        
        // Log all collider names for debugging
        if (colliders.Length > 0 && debugMode)
        {
            string colliderNames = string.Join(", ", System.Array.ConvertAll(colliders, c => c != null ? c.name : "null"));
            Debug.Log($"[VRGrabThrow] Colliders found: {colliderNames}");
        }
        
        // Check all colliders found in sphere
        foreach (Collider col in colliders)
        {
            if (col == null) continue;
            
            Rigidbody rb = null;
            
            // First, check if this object has Oculus Interaction Grabbable (like BuildingBlock Cube and PhysicsBalls)
            #if UNITY_EDITOR || UNITY_ANDROID
            try
            {
                Component grabbableComponent = col.GetComponent("Grabbable");
                if (grabbableComponent == null)
                {
                    // GetComponentInParent requires a Type, so we need to get the type first
                    // Search all loaded assemblies for the Grabbable type
                    System.Type grabbableType = null;
                    try
                    {
                        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            grabbableType = assembly.GetType("Oculus.Interaction.Grabbable");
                            if (grabbableType != null) break;
                        }
                    }
                    catch (System.Exception) { }
                    
                    if (grabbableType != null)
                    {
                        grabbableComponent = col.GetComponentInParent(grabbableType);
                    }
                }
                
                if (grabbableComponent != null)
                {
                    // First try to get Rigidbody from the GameObject
                    rb = grabbableComponent.GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        // Try to get Rigidbody from the GameObject the component is on
                        rb = grabbableComponent.gameObject.GetComponent<Rigidbody>();
                    }
                    if (rb == null)
                    {
                        // Try to get Rigidbody property from Grabbable using reflection
                        var rigidbodyProperty = grabbableComponent.GetType().GetProperty("Rigidbody", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (rigidbodyProperty != null)
                        {
                            rb = rigidbodyProperty.GetValue(grabbableComponent) as Rigidbody;
                        }
                    }
                    if (rb == null)
                    {
                        // Try to get Rigidbody field from Grabbable using reflection
                        var rigidbodyField = grabbableComponent.GetType().GetField("_rigidbody", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (rigidbodyField != null)
                        {
                            rb = rigidbodyField.GetValue(grabbableComponent) as Rigidbody;
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                // Oculus Interaction not available, fall through to Rigidbody check
            }
            #endif
            
            // Fallback: Check for Rigidbody directly (for objects without Grabbable component)
            if (rb == null)
            {
                // Try multiple methods to get Rigidbody
                rb = col.attachedRigidbody;
                if (rb == null)
                {
                    rb = col.GetComponent<Rigidbody>();
                }
                if (rb == null)
                {
                    rb = col.gameObject.GetComponent<Rigidbody>();
                }
                if (rb == null)
                {
                    // Try getting from parent if collider doesn't have one
                    rb = col.GetComponentInParent<Rigidbody>();
                }
            }
            
            // If we found a Rigidbody, check if it's the closest
            if (rb != null && rb != grabbedObject)
            {
                // Skip controller-related objects
                if (ShouldSkipObject(rb))
                {
                    if (debugMode)
                    {
                        Debug.Log($"[VRGrabThrow] Skipping controller-related object: {rb.name}");
                    }
                    continue;
                }
                
                // Skip objects that use Oculus Interaction Grabbable (let Oculus system handle them)
                #if UNITY_EDITOR || UNITY_ANDROID
                if (ShouldSkipOculusGrabbable(rb))
                {
                    if (debugMode)
                    {
                        Debug.Log($"[VRGrabThrow] Skipping {rb.name} - using Oculus Interaction system (like cube)");
                    }
                    continue;
                }
                #endif
                
                float distance = Vector3.Distance(grabPoint.position, rb.transform.position);
                
                #if UNITY_EDITOR || UNITY_ANDROID
                bool hasGrabbable = false;
                try
                {
                    hasGrabbable = col.GetComponent("Grabbable") != null;
                    if (!hasGrabbable)
                    {
                        // Search all loaded assemblies for the Grabbable type
                        System.Type grabbableType = null;
                        try
                        {
                            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                            {
                                grabbableType = assembly.GetType("Oculus.Interaction.Grabbable");
                                if (grabbableType != null) break;
                            }
                        }
                        catch (System.Exception) { }
                        
                        if (grabbableType != null)
                        {
                            hasGrabbable = col.GetComponentInParent(grabbableType) != null;
                        }
                    }
                }
                catch (System.Exception) { }
                Debug.Log($"[VRGrabThrow] Found {(hasGrabbable ? "Grabbable" : "Rigidbody")} object: {rb.name}, Distance: {distance:F3}, isKinematic: {rb.isKinematic}");
                #else
                Debug.Log($"[VRGrabThrow] Found Rigidbody object: {rb.name}, Distance: {distance:F3}, isKinematic: {rb.isKinematic}");
                #endif
                
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
            RaycastHit[] hits = Physics.RaycastAll(grabPoint.position, transform.forward, detectionRadius * 2f, grabbableLayer);
            
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                
                Rigidbody rb = null;
                
                // Check for Grabbable component first (like BuildingBlock Cube)
                #if UNITY_EDITOR || UNITY_ANDROID
                try
                {
                    Component grabbableComponent = hit.collider.GetComponent("Grabbable");
                    if (grabbableComponent == null)
                    {
                        // Search all loaded assemblies for the Grabbable type
                        System.Type grabbableType = null;
                        try
                        {
                            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                            {
                                grabbableType = assembly.GetType("Oculus.Interaction.Grabbable");
                                if (grabbableType != null) break;
                            }
                        }
                        catch (System.Exception) { }
                        
                        if (grabbableType != null)
                        {
                            grabbableComponent = hit.collider.GetComponentInParent(grabbableType);
                        }
                    }
                    
                    if (grabbableComponent != null)
                    {
                        rb = grabbableComponent.GetComponent<Rigidbody>();
                        if (rb == null)
                        {
                            var rigidbodyProperty = grabbableComponent.GetType().GetProperty("Rigidbody");
                            if (rigidbodyProperty != null)
                            {
                                rb = rigidbodyProperty.GetValue(grabbableComponent) as Rigidbody;
                            }
                        }
                    }
                }
                catch (System.Exception)
                {
                    // Oculus Interaction not available, fall through to Rigidbody check
                }
                #endif
                
                // Fallback: Check for Rigidbody directly
                if (rb == null)
                {
                    // Try multiple methods to get Rigidbody
                    rb = hit.collider.attachedRigidbody;
                    if (rb == null)
                    {
                        rb = hit.collider.GetComponent<Rigidbody>();
                    }
                    if (rb == null)
                    {
                        rb = hit.collider.gameObject.GetComponent<Rigidbody>();
                    }
                    if (rb == null)
                    {
                        rb = hit.collider.GetComponentInParent<Rigidbody>();
                    }
                }
                
                if (rb != null && rb != grabbedObject)
                {
                    // Skip controller-related objects
                    if (ShouldSkipObject(rb))
                    {
                        if (debugMode)
                        {
                            Debug.Log($"[VRGrabThrow] Skipping controller-related object: {rb.name}");
                        }
                        continue;
                    }
                    
                    // Skip objects that use Oculus Interaction Grabbable
                    #if UNITY_EDITOR || UNITY_ANDROID
                    if (ShouldSkipOculusGrabbable(rb))
                    {
                        if (debugMode)
                        {
                            Debug.Log($"[VRGrabThrow] Skipping {rb.name} - using Oculus Interaction system");
                        }
                        continue;
                    }
                    #endif
                    
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
                detectionRadius * 0.5f, 
                transform.forward, 
                detectionRadius, 
                grabbableLayer
            );
            
            foreach (RaycastHit hit in sphereHits)
            {
                if (hit.collider == null) continue;
                
                Rigidbody rb = null;
                
                // Check for Grabbable component first (like BuildingBlock Cube)
                #if UNITY_EDITOR || UNITY_ANDROID
                try
                {
                    Component grabbableComponent = hit.collider.GetComponent("Grabbable");
                    if (grabbableComponent == null)
                    {
                        // Search all loaded assemblies for the Grabbable type
                        System.Type grabbableType = null;
                        try
                        {
                            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                            {
                                grabbableType = assembly.GetType("Oculus.Interaction.Grabbable");
                                if (grabbableType != null) break;
                            }
                        }
                        catch (System.Exception) { }
                        
                        if (grabbableType != null)
                        {
                            grabbableComponent = hit.collider.GetComponentInParent(grabbableType);
                        }
                    }
                    
                    if (grabbableComponent != null)
                    {
                        rb = grabbableComponent.GetComponent<Rigidbody>();
                        if (rb == null)
                        {
                            var rigidbodyProperty = grabbableComponent.GetType().GetProperty("Rigidbody");
                            if (rigidbodyProperty != null)
                            {
                                rb = rigidbodyProperty.GetValue(grabbableComponent) as Rigidbody;
                            }
                        }
                    }
                }
                catch (System.Exception)
                {
                    // Oculus Interaction not available, fall through to Rigidbody check
                }
                #endif
                
                // Fallback: Check for Rigidbody directly
                if (rb == null)
                {
                    // Try multiple methods to get Rigidbody
                    rb = hit.collider.attachedRigidbody;
                    if (rb == null)
                    {
                        rb = hit.collider.GetComponent<Rigidbody>();
                    }
                    if (rb == null)
                    {
                        rb = hit.collider.gameObject.GetComponent<Rigidbody>();
                    }
                    if (rb == null)
                    {
                        rb = hit.collider.GetComponentInParent<Rigidbody>();
                    }
                }
                
                if (rb != null && rb != grabbedObject)
                {
                    // Skip controller-related objects
                    if (ShouldSkipObject(rb))
                    {
                        if (debugMode)
                        {
                            Debug.Log($"[VRGrabThrow] Skipping controller-related object: {rb.name}");
                        }
                        continue;
                    }
                    
                    // Skip objects that use Oculus Interaction Grabbable
                    #if UNITY_EDITOR || UNITY_ANDROID
                    if (ShouldSkipOculusGrabbable(rb))
                    {
                        if (debugMode)
                        {
                            Debug.Log($"[VRGrabThrow] Skipping {rb.name} - using Oculus Interaction system");
                        }
                        continue;
                    }
                    #endif
                    
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
            Debug.Log($"[VRGrabThrow] ✓ Grabbing object: {targetRb.name} at distance {closestDistance:F3}");
            GrabObject(targetRb);
        }
        else
        {
            Debug.LogWarning($"[VRGrabThrow] ✗ No grabbable object found in range. Make sure balls have Rigidbody components!");
        }
    }
    
    void GrabObject(Rigidbody rb)
    {
        if (rb == null) return;
        
        // Check if object is already being grabbed by Oculus Interaction system
        #if UNITY_EDITOR || UNITY_ANDROID
        try
        {
            Component grabbable = rb.GetComponent("Grabbable");
            if (grabbable != null)
            {
                // Check if Grabbable is currently selected (being grabbed)
                var pointsProperty = grabbable.GetType().GetProperty("Points", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (pointsProperty != null)
                {
                    var points = pointsProperty.GetValue(grabbable);
                    if (points != null)
                    {
                        // Check if there are any active grab points
                        var countProperty = points.GetType().GetProperty("Count");
                        if (countProperty != null)
                        {
                            int pointCount = (int)countProperty.GetValue(points);
                            if (pointCount > 0)
                            {
                                Debug.LogWarning($"[VRGrabThrow] Object {rb.name} is already being grabbed by Oculus Interaction system ({pointCount} grab points). Cannot grab with controller.");
                                return;
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            if (debugMode)
            {
                Debug.LogWarning($"[VRGrabThrow] Could not check if object is already grabbed: {ex.Message}");
            }
        }
        #endif
        
        Debug.Log($"[VRGrabThrow] GrabObject called for {rb.name}");
        
        // Disable Oculus Interaction HandGrabInteractable if present to prevent conflicts
        #if UNITY_EDITOR || UNITY_ANDROID
        try
        {
            if (rb == null || rb.gameObject == null) return;
            
            Component handGrabInteractable = rb.GetComponent("HandGrabInteractable");
            if (handGrabInteractable == null)
            {
                // Try to find it by searching for the type
                System.Type handGrabType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        handGrabType = assembly.GetType("Oculus.Interaction.HandGrab.HandGrabInteractable");
                        if (handGrabType != null) break;
                    }
                    catch { }
                }
                if (handGrabType != null && rb != null && rb.gameObject != null)
                {
                    handGrabInteractable = rb.GetComponent(handGrabType);
                }
            }
            
            if (handGrabInteractable != null && handGrabInteractable.gameObject != null)
            {
                // Temporarily disable HandGrabInteractable to prevent conflicts
                var enabledProperty = handGrabInteractable.GetType().GetProperty("enabled");
                if (enabledProperty != null)
                {
                    try
                    {
                        enabledProperty.SetValue(handGrabInteractable, false);
                        if (debugMode)
                        {
                            Debug.Log($"[VRGrabThrow] Disabled HandGrabInteractable on {rb.name} to prevent conflicts");
                        }
                    }
                    catch (System.Exception ex2)
                    {
                        if (debugMode)
                        {
                            Debug.LogWarning($"[VRGrabThrow] Error disabling HandGrabInteractable: {ex2.Message}");
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            // Silently catch to prevent errors from Oculus SDK components
            if (debugMode)
            {
                Debug.LogWarning($"[VRGrabThrow] Could not disable HandGrabInteractable: {ex.Message}");
            }
        }
        #endif
        
        grabbedObject = rb;
        isGrabbing = true;
        
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
        
        // Re-enable Oculus Interaction HandGrabInteractable if it was disabled
        #if UNITY_EDITOR || UNITY_ANDROID
        try
        {
            if (rbToRelease == null || rbToRelease.gameObject == null) return;
            
            Component handGrabInteractable = rbToRelease.GetComponent("HandGrabInteractable");
            if (handGrabInteractable == null)
            {
                System.Type handGrabType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        handGrabType = assembly.GetType("Oculus.Interaction.HandGrab.HandGrabInteractable");
                        if (handGrabType != null) break;
                    }
                    catch { }
                }
                if (handGrabType != null && rbToRelease != null && rbToRelease.gameObject != null)
                {
                    handGrabInteractable = rbToRelease.GetComponent(handGrabType);
                }
            }
            
            if (handGrabInteractable != null && handGrabInteractable.gameObject != null)
            {
                var enabledProperty = handGrabInteractable.GetType().GetProperty("enabled");
                if (enabledProperty != null)
                {
                    try
                    {
                        enabledProperty.SetValue(handGrabInteractable, true);
                        if (debugMode)
                        {
                            Debug.Log($"[VRGrabThrow] Re-enabled HandGrabInteractable on {rbToRelease.name}");
                        }
                    }
                    catch (System.Exception ex2)
                    {
                        if (debugMode)
                        {
                            Debug.LogWarning($"[VRGrabThrow] Error re-enabling HandGrabInteractable: {ex2.Message}");
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            // Silently catch to prevent errors from Oculus SDK components
            if (debugMode)
            {
                Debug.LogWarning($"[VRGrabThrow] Could not re-enable HandGrabInteractable: {ex.Message}");
            }
        }
        #endif
        
        // Restore physics before applying velocity
        rbToRelease.isKinematic = false;
        rbToRelease.transform.SetParent(null);
        
        // Calculate throw velocity based on controller movement
        Vector3 throwVelocity;
        
        if (forwardThrowEnabled)
        {
            // Get forward direction
            Vector3 forwardDirection = transform.forward;
            
            // Project velocity onto forward direction
            float forwardSpeed = Vector3.Dot(currentVelocity, forwardDirection);
            
            // Calculate side movement (perpendicular to forward)
            Vector3 sideMovement = currentVelocity - (forwardDirection * forwardSpeed);
            
            // If forward speed is negative (backward), use movement magnitude instead
            // This handles overhand throws where controller might move backward
            if (forwardSpeed < 0)
            {
                // Use total movement speed but always forward direction
                float movementSpeed = currentVelocity.magnitude;
                forwardSpeed = Mathf.Max(movementSpeed, minForwardSpeed);
                
                if (debugMode)
                {
                    Debug.Log($"[VRGrabThrow] Backward movement detected - Using forward direction with speed: {forwardSpeed:F2} m/s");
                }
            }
            else
            {
                // Apply minimum forward speed threshold (prevents weak throws)
                if (forwardSpeed < minForwardSpeed)
                {
                    forwardSpeed = minForwardSpeed;
                }
            }
            
            // Filter out side movements (reduce by filter amount)
            Vector3 filteredSideMovement = sideMovement * (1f - sideMovementFilter);
            
            // Ensure filtered side movement doesn't have backward components
            // Project filtered side movement onto forward to check
            float sideForwardComponent = Vector3.Dot(filteredSideMovement, forwardDirection);
            if (sideForwardComponent < 0)
            {
                // Remove backward component from side movement
                filteredSideMovement = filteredSideMovement - (forwardDirection * sideForwardComponent);
            }
            
            // Combine forward + filtered side movement (always forward)
            Vector3 filteredVelocity = (forwardDirection * forwardSpeed) + filteredSideMovement;
            
            // Final safety check: ensure throw velocity has forward component
            float finalForwardSpeed = Vector3.Dot(filteredVelocity, forwardDirection);
            if (finalForwardSpeed <= 0)
            {
                // Force forward direction with minimum speed
                filteredVelocity = forwardDirection * minForwardSpeed;
                finalForwardSpeed = minForwardSpeed;
                if (debugMode)
                {
                    Debug.Log($"[VRGrabThrow] Safety check: Forced forward direction with speed: {minForwardSpeed:F2} m/s");
                }
            }
            
            throwVelocity = filteredVelocity * throwForce;
            
            if (debugMode)
            {
                Debug.Log($"[VRGrabThrow] Forward throw - Forward speed: {forwardSpeed:F2} m/s, Final speed: {finalForwardSpeed:F2} m/s, Side filtered: {sideMovement.magnitude * sideMovementFilter:F2} m/s");
            }
        }
        else
        {
            // Original behavior: use raw velocity
            throwVelocity = currentVelocity * throwForce;
        }
        
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
