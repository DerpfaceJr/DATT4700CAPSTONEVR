using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// VR Grab and Throw script for Meta Quest controllers
/// Allows picking up and throwing objects in VR
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class VRGrabThrow : MonoBehaviour
{
    [Header("Grab Settings")]
    [Tooltip("Distance at which objects can be grabbed")]
    public float grabDistance = 0.5f;
    
    [Tooltip("Force multiplier for throwing")]
    public float throwForce = 10f;
    
    [Tooltip("Layer mask for grabbable objects")]
    public LayerMask grabbableLayer = -1;
    
    private Rigidbody grabbedObject = null;
    private Transform grabPoint = null;
    private InputDevice controllerDevice;
    private bool isGrabbing = false;
    private Vector3 previousPosition;
    private Vector3 currentVelocity;
    
    void Start()
    {
        // Create grab point
        GameObject grabPointObj = new GameObject("GrabPoint");
        grabPointObj.transform.SetParent(transform);
        grabPointObj.transform.localPosition = Vector3.zero;
        grabPoint = grabPointObj.transform;
        
        // Initialize controller device
        InitializeController();
    }
    
    void InitializeController()
    {
        var controllers = new List<InputDevice>();
        InputDeviceCharacteristics characteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller;
        
        // Try to find right controller first, then left
        InputDevices.GetDevicesWithCharacteristics(characteristics | InputDeviceCharacteristics.Right, controllers);
        if (controllers.Count == 0)
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
        
        // Check for grab input
        bool gripPressed = false;
        bool triggerPressed = false;
        controllerDevice.TryGetFeatureValue(CommonUsages.gripButton, out gripPressed);
        controllerDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
        
        bool grabInput = gripPressed || triggerPressed;
        
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
        
        // Calculate velocity for throwing
        currentVelocity = (transform.position - previousPosition) / Time.deltaTime;
        previousPosition = transform.position;
    }
    
    void TryGrab()
    {
        // Raycast to find grabbable objects
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, grabDistance, grabbableLayer))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                grabbedObject = rb;
                isGrabbing = true;
                
                // Make object kinematic while grabbed for smooth movement
                grabbedObject.isKinematic = true;
                grabbedObject.transform.SetParent(grabPoint);
                grabbedObject.transform.localPosition = Vector3.zero;
            }
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
        
        // Apply throw velocity
        grabbedObject.linearVelocity = currentVelocity * throwForce;
        
        // Add some angular velocity for spin
        grabbedObject.angularVelocity = Random.insideUnitSphere * 2f;
        
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
