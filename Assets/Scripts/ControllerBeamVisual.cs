using UnityEngine;

/// <summary>
/// Updates a LineRenderer to show a beam from the controller forward direction.
/// Attach to each [BuildingBlock] Controller Tracking (Left/Right) that has a LineRenderer.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ControllerBeamVisual : MonoBehaviour
{
    [Tooltip("Length of the beam in meters")]
    public float beamLength = 5f;

    [Tooltip("Width at the start (controller)")]
    public float startWidth = 0.005f;

    [Tooltip("Width at the end of the beam")]
    public float endWidth = 0.002f;

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null) return;

        _lineRenderer.positionCount = 2;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = startWidth;
        _lineRenderer.endWidth = endWidth;
        _lineRenderer.enabled = true;

        // Ensure beam is visible: use a simple material if none assigned
        if (_lineRenderer.material == null || _lineRenderer.material.name.StartsWith("Default"))
        {
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.2f, 0.6f, 1f, 0.9f);
                _lineRenderer.material = mat;
            }
        }
    }

    private void Update()
    {
        if (_lineRenderer == null) return;

        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * beamLength;
        _lineRenderer.SetPosition(0, start);
        _lineRenderer.SetPosition(1, end);
    }
}
