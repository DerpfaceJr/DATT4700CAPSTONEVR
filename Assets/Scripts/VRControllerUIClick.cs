using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// Makes VR controller trigger (or primary button) register as UI click when the controller ray hits a Button.
/// Adds hover highlight when the ray is over a button. Add to Canvas or EventSystem.
/// Uses UnityEngine.UI.GraphicRaycaster for screen-position raycasts (not TrackedDeviceGraphicRaycaster).
/// </summary>
[DefaultExecutionOrder(50)]
public class VRControllerUIClick : MonoBehaviour
{
    [Tooltip("Max distance for UI raycast (meters)")]
    public float maxRayDistance = 10f;

    [Tooltip("Trigger value above this counts as click (0-1)")]
    [Range(0.01f, 0.9f)]
    public float triggerClickThreshold = 0.5f;

    [Tooltip("Use primary button (X/A) as click instead of trigger")]
    public bool usePrimaryButtonInsteadOfTrigger = false;

    [Header("Debug")]
    [Tooltip("Log initialization and each click/hover for debugging")]
    public bool debugLogs = true;

    [Header("Optional – auto-found if not set")]
    public Canvas targetCanvas;
    public Camera raycastCamera;
    public Transform leftController;
    public Transform rightController;

    private List<RaycastResult> _raycastResults = new List<RaycastResult>();
    private InputDevice _leftDevice;
    private InputDevice _rightDevice;
    private bool _leftTriggerDown;
    private bool _rightTriggerDown;
    private EventSystem _eventSystem;
    /// <summary>Standard UI raycaster for world-space canvas + screen point from WorldToScreenPoint. TrackedDeviceGraphicRaycaster is for XR module input, not this manual path.</summary>
    private GraphicRaycaster _graphicRaycaster;
    private bool _initialized;
    private GameObject _hoveredObject;
    private PointerEventData _hoverPointerData;

    private void Start()
    {
        if (targetCanvas == null)
            targetCanvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
        if (targetCanvas == null)
        {
            Debug.LogWarning("[VRControllerUIClick] No Canvas found.");
            return;
        }

        // Use the same camera the canvas uses for rendering, so screen-space raycast matches
        raycastCamera = raycastCamera != null ? raycastCamera : (targetCanvas.worldCamera != null ? targetCanvas.worldCamera : null);
        if (raycastCamera == null)
        {
            var centerEye = GameObject.Find("CenterEyeAnchor");
            if (centerEye != null)
                raycastCamera = centerEye.GetComponent<Camera>();
            if (raycastCamera == null)
                raycastCamera = Camera.main;
            if (raycastCamera == null)
            {
                var overlayCam = targetCanvas.transform.Find("Canvas Overlay Camera");
                if (overlayCam != null)
                    raycastCamera = overlayCam.GetComponent<Camera>();
            }
        }
        if (raycastCamera != null && targetCanvas.worldCamera == null)
            targetCanvas.worldCamera = raycastCamera;
        if (raycastCamera == null)
            Debug.LogWarning("[VRControllerUIClick] No camera for UI raycast.");

        _graphicRaycaster = targetCanvas.GetComponent<GraphicRaycaster>();
        if (_graphicRaycaster == null)
            _graphicRaycaster = targetCanvas.GetComponentInChildren<GraphicRaycaster>(true);
        if (_graphicRaycaster != null)
            _graphicRaycaster.enabled = true;
        if (_graphicRaycaster == null && debugLogs)
            Debug.Log("[VRControllerUIClick] No GraphicRaycaster on Canvas – using EventSystem.RaycastAll.");

        _eventSystem = EventSystem.current;
        if (_eventSystem == null)
            Debug.LogWarning("[VRControllerUIClick] No EventSystem in scene.");

        FindControllers();
        EnsureInputDevices();
        _hoverPointerData = new PointerEventData(_eventSystem);

        _initialized = targetCanvas != null && (_graphicRaycaster != null || _eventSystem != null);

        if (debugLogs)
        {
            Debug.Log($"[VRControllerUIClick] Init: canvas={targetCanvas != null}, camera={raycastCamera != null}, graphicRaycaster={_graphicRaycaster != null}, eventSystem={_eventSystem != null}, leftCtrl={leftController != null}, rightCtrl={rightController != null}, leftDev={_leftDevice.isValid}, rightDev={_rightDevice.isValid}, initialized={_initialized}");
        }
    }

    private void FindControllers()
    {
        if (leftController == null)
        {
            var go = GameObject.Find("[BuildingBlock] Controller Tracking Left");
            if (go != null) leftController = go.transform;
        }
        if (rightController == null)
        {
            var go = GameObject.Find("[BuildingBlock] Controller Tracking Right");
            if (go != null) rightController = go.transform;
        }
    }

    private void EnsureInputDevices()
    {
        if (!_leftDevice.isValid)
        {
            var list = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand, list);
            if (list.Count > 0) _leftDevice = list[0];
        }
        if (!_rightDevice.isValid)
        {
            var list = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand, list);
            if (list.Count > 0) _rightDevice = list[0];
        }
    }

    private void Update()
    {
        if (!_initialized) return;

        EnsureInputDevices();

        // Hover: raycast from both controllers and update highlighted button
        UpdateHover();

        // Click on trigger/primary
        bool leftClick = GetControllerClick(_leftDevice, true);
        bool rightClick = GetControllerClick(_rightDevice, false);

        if (leftClick && leftController != null)
            TryClickUI(leftController);
        if (rightClick && rightController != null)
            TryClickUI(rightController);
    }

    private void UpdateHover()
    {
        Button hitButton = null;
        RaycastResult? hitResult = null;
        Vector2 screenPoint = Vector2.zero;

        if (leftController != null && RaycastUIFrom(leftController, out hitButton, out hitResult, out screenPoint)) { }
        else if (rightController != null && RaycastUIFrom(rightController, out hitButton, out hitResult, out screenPoint)) { }

        GameObject newHovered = hitButton != null ? hitButton.gameObject : null;

        if (newHovered != _hoveredObject)
        {
            if (_hoveredObject != null && _eventSystem != null)
            {
                _hoverPointerData.Reset();
                _hoverPointerData.position = screenPoint;
                _hoverPointerData.pointerCurrentRaycast = hitResult ?? default;
                ExecuteEvents.Execute(_hoveredObject, _hoverPointerData, ExecuteEvents.pointerExitHandler);
                if (debugLogs)
                    Debug.Log($"[VRControllerUIClick] Hover exit: {_hoveredObject.name}");
            }
            _hoveredObject = newHovered;
            if (_hoveredObject != null && _eventSystem != null && hitResult.HasValue)
            {
                _hoverPointerData.position = screenPoint;
                _hoverPointerData.pointerCurrentRaycast = hitResult.Value;
                ExecuteEvents.Execute(_hoveredObject, _hoverPointerData, ExecuteEvents.pointerEnterHandler);
                if (debugLogs)
                    Debug.Log($"[VRControllerUIClick] Hover enter: {_hoveredObject.name}");
            }
        }
    }

    private bool GetControllerClick(InputDevice device, bool isLeft)
    {
        if (!device.isValid) return false;

        bool triggerButton = false;
        float triggerValue = 0f;
        bool primaryButton = false;

        device.TryGetFeatureValue(CommonUsages.triggerButton, out triggerButton);
        device.TryGetFeatureValue(CommonUsages.trigger, out triggerValue);
        device.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButton);

        bool pressed;
        if (usePrimaryButtonInsteadOfTrigger)
            pressed = primaryButton;
        else
            pressed = triggerButton || (triggerValue >= triggerClickThreshold);

        if (pressed)
        {
            if (isLeft && !_leftTriggerDown) { _leftTriggerDown = true; return true; }
            if (!isLeft && !_rightTriggerDown) { _rightTriggerDown = true; return true; }
        }
        else
        {
            if (isLeft) _leftTriggerDown = false;
            else _rightTriggerDown = false;
        }
        return false;
    }

    private void TryClickUI(Transform controller)
    {
        Button button;
        RaycastResult? _;
        Vector2 __;
        if (!RaycastUIFrom(controller, out button, out _, out __))
        {
            if (debugLogs)
                Debug.Log("[VRControllerUIClick] Click: ray did not hit any button.");
            return;
        }
        if (button != null && button.interactable)
        {
            if (debugLogs)
                Debug.Log($"[VRControllerUIClick] Click: invoking button '{button.gameObject.name}'");
            button.onClick.Invoke();
        }
        else if (debugLogs)
            Debug.Log($"[VRControllerUIClick] Click: button not interactable.");
    }

    /// <summary>Raycast from a controller; returns true if we hit a Button and sets hitButton, hitResult, screenPoint.</summary>
    private bool RaycastUIFrom(Transform controller, out Button hitButton, out RaycastResult? hitResult, out Vector2 screenPoint)
    {
        hitButton = null;
        hitResult = null;
        screenPoint = Vector2.zero;

        Ray worldRay = new Ray(controller.position, controller.forward);

        // 1) Try screen-space path: canvas plane + GraphicRaycaster (needs camera)
        if (raycastCamera != null)
        {
            RectTransform rect = targetCanvas.GetComponent<RectTransform>();
            if (rect != null)
            {
                Plane planeForward = new Plane(rect.forward, rect.position);
                Plane planeBack = new Plane(-rect.forward, rect.position);
                float enter = float.MaxValue;
                bool hit = planeForward.Raycast(worldRay, out float e1) && e1 > 0.001f && e1 <= maxRayDistance;
                if (hit) enter = e1;
                if (planeBack.Raycast(worldRay, out float e2) && e2 > 0.001f && e2 <= maxRayDistance && e2 < enter)
                {
                    enter = e2;
                    hit = true;
                }
                if (hit)
                {
                    Vector3 worldHit = worldRay.GetPoint(enter);
                    screenPoint = raycastCamera.WorldToScreenPoint(worldHit);
                    _raycastResults.Clear();
                    PointerEventData ped = new PointerEventData(_eventSystem) { position = screenPoint };
                    if (_graphicRaycaster != null)
                        _graphicRaycaster.Raycast(ped, _raycastResults);
                    else if (_eventSystem != null)
                        _eventSystem.RaycastAll(ped, _raycastResults);

                    foreach (var result in _raycastResults)
                    {
                        if (result.gameObject == null) continue;
                        var button = result.gameObject.GetComponent<Button>();
                        if (button == null)
                            button = result.gameObject.GetComponentInParent<Button>();
                        if (button != null)
                        {
                            hitButton = button;
                            hitResult = result;
                            return true;
                        }
                    }
                }
            }
        }

        // 2) Fallback: direct world-space ray vs each button RectTransform (always works if ray hits a button)
        if (RaycastButtonsDirect(worldRay, out Button directHit, out float directEnter))
        {
            hitButton = directHit;
            hitResult = new RaycastResult { gameObject = directHit.gameObject, module = _graphicRaycaster, distance = directEnter };
            screenPoint = raycastCamera != null ? raycastCamera.WorldToScreenPoint(worldRay.GetPoint(directEnter)) : Vector2.zero;
            return true;
        }
        return false;
    }

    /// <summary>Ray against each Button's RectTransform in world space. Works even when GraphicRaycaster fails.</summary>
    private bool RaycastButtonsDirect(Ray worldRay, out Button hitButton, out float hitDistance)
    {
        hitButton = null;
        hitDistance = 0f;
        Button[] buttons = targetCanvas.GetComponentsInChildren<Button>(true);
        float bestDistance = float.MaxValue;
        Button best = null;

        foreach (var button in buttons)
        {
            if (button == null) continue;
            RectTransform rt = button.GetComponent<RectTransform>();
            if (rt == null) continue;

            float enter = float.MaxValue;
            Plane pF = new Plane(rt.forward, rt.position);
            Plane pB = new Plane(-rt.forward, rt.position);
            if (pF.Raycast(worldRay, out float e1) && e1 > 0.001f && e1 <= maxRayDistance) enter = Mathf.Min(enter, e1);
            if (pB.Raycast(worldRay, out float e2) && e2 > 0.001f && e2 <= maxRayDistance) enter = Mathf.Min(enter, e2);
            if (enter > maxRayDistance) continue;

            Vector3 worldHit = worldRay.GetPoint(enter);
            Vector3 local = rt.InverseTransformPoint(worldHit);
            if (!rt.rect.Contains(new Vector2(local.x, local.y)))
                continue;

            if (enter < bestDistance)
            {
                bestDistance = enter;
                best = button;
            }
        }

        if (best != null)
        {
            hitButton = best;
            hitDistance = bestDistance;
            return true;
        }
        return false;
    }
}
