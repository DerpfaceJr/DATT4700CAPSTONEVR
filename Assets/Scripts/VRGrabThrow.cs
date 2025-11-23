using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// VR Grab and Throw script for Meta Quest controllers
/// Allows picking up and throwing objects in VR
/// Attach this to controller GameObjects (LeftHandAnchor, RightHandAnchor, or Controller objects)
/// </summary>
public class VRGrabThrow : MonoBehaviour
{
    [Header("Grab Settings")]
    [Tooltip("Distance at which objects can be grabbed")]
    public float grabDistance = 2.0f;
    
    [Tooltip("Force multiplier for throwing")]
    public float throwForce = 1.0f;
    
    [Tooltip("Layer mask for grabbable objects (leave as Everything to grab all objects)")]
    public LayerMask grabbableLayer = -1;
    
    [Header("Controller Settings")]
    [Tooltip("Which hand this controller is for (auto-detected if not set)")]
    public bool isRightHand = true;
    
    private Rigidbody grabbedObject = null;
    private Transform grabPoint = null;
    private InputDevice controllerDevice;
    private bool isGrabbing = false;
    private Vector3 previousPosition;
    private Vector3 currentVelocity;
    private Vector3[] velocityHistory = new Vector3[5];
    private int velocityIndex = 0;
    
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
        }
    }
    
    void Update()
    {
        // Re-initialize if device lost
        if (!controllerDevice.isValid)
        {
            InitializeController();
        }
        
        if (!controllerDevice.isValid) return;
        
        // Check for grab input (back button - B on right controller, Y on left controller)
        bool backButtonPressed = false;
        bool primaryButtonPressed = false;
        bool gripPressed = false;
        bool triggerPressed = false;
        
        controllerDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out backButtonPressed);
        controllerDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButtonPressed);
        controllerDevice.TryGetFeatureValue(CommonUsages.gripButton, out gripPressed);
        controllerDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
        
        // Use back button, but also allow grip/trigger as fallback
        bool grabInput = backButtonPressed || gripPressed || triggerPressed;
        
        // Debug logging (only log when button is pressed to avoid spam)
        if (backButtonPressed || primaryButtonPressed || gripPressed || triggerPressed)
        {
            Debug.Log($"[VRGrabThrow] Button pressed - Back: {backButtonPressed}, Primary: {primaryButtonPressed}, Grip: {gripPressed}, Trigger: {triggerPressed}");
        }
        
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
        
        previousPosition = transform.position;
    }
    
    void TryGrab()
    {
        // Try sphere cast first (better for grabbing objects)
        Collider[] colliders = Physics.OverlapSphere(transform.position, grabDistance, grabbableLayer);
        
        Debug.Log($"[VRGrabThrow] TryGrab - Position: {transform.position}, Grab Distance: {grabDistance}, Found {colliders.Length} colliders");
        
        // If no objects found with sphere cast, try raycast
        if (colliders.Length == 0)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, grabDistance, grabbableLayer))
            {
                Debug.Log($"[VRGrabThrow] Raycast hit: {hit.collider.name}");
                Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic && rb != grabbedObject)
                {
                    Debug.Log($"[VRGrabThrow] Grabbing via raycast: {rb.name}");
                    GrabObject(rb);
                    return;
                }
            }
            else
            {
                Debug.Log($"[VRGrabThrow] No objects in grab range");
            }
        }
        else
        {
            // Find the closest grabbable object
            Rigidbody closestRb = null;
            float closestDistance = float.MaxValue;
            
            foreach (Collider col in colliders)
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Debug.Log($"[VRGrabThrow] Found Rigidbody on {col.name}, isKinematic: {rb.isKinematic}");
                    if (!rb.isKinematic && rb != grabbedObject)
                    {
                        float distance = Vector3.Distance(transform.position, rb.transform.position);
                        Debug.Log($"[VRGrabThrow] Distance to {col.name}: {distance}");
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestRb = rb;
                        }
                    }
                }
                else
                {
                    Debug.Log($"[VRGrabThrow] No Rigidbody on {col.name}");
                }
            }
            
            if (closestRb != null)
            {
                Debug.Log($"[VRGrabThrow] Grabbing closest object: {closestRb.name}");
                GrabObject(closestRb);
            }
            else
            {
                Debug.Log($"[VRGrabThrow] No valid grabbable object found");
            }
        }
    }
    
    void GrabObject(Rigidbody rb)
    {
        Debug.Log($"[VRGrabThrow] GrabObject called for {rb.name}");
        grabbedObject = rb;
        isGrabbing = true;
        
        // Make object kinematic while grabbed for smooth movement
        grabbedObject.isKinematic = true;
        grabbedObject.transform.SetParent(grabPoint);
        grabbedObject.transform.localPosition = Vector3.zero;
        grabbedObject.transform.localRotation = Quaternion.identity;
        
        Debug.Log($"[VRGrabThrow] Successfully grabbed {rb.name}");
        
        // Reset velocity tracking
        for (int i = 0; i < velocityHistory.Length; i++)
        {
            velocityHistory[i] = Vector3.zero;
        }
    }
    
    void UpdateGrabbedObject()
    {
        if (grabbedObject == null) return;
        
        // Keep object at grab point
        grabbedObject.transform.position = grabPoint.position;
        grabbedObject.transform.rotation = grabPoint.rotation;
    }
    
    void Release()
    {
        if (grabbedObject == null) return;
        
        // Restore physics
        grabbedObject.isKinematic = false;
        grabbedObject.transform.SetParent(null);
        
        // Apply throw velocity (use currentVelocity which is averaged for smoother throws)
        grabbedObject.linearVelocity = currentVelocity * throwForce;
        
        // Add some angular velocity for realistic spin
        grabbedObject.angularVelocity = Random.insideUnitSphere * 5f;
        
        grabbedObject = null;
        isGrabbing = false;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw grab range
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * grabDistance);
    }
}
