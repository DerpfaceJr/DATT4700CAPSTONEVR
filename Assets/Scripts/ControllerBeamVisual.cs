using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Updates a LineRenderer to show a beam from the controller forward direction.
/// Attach to each [BuildingBlock] Controller Tracking (Left/Right) that has a LineRenderer.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ControllerBeamVisual : MonoBehaviour
{
    static readonly Color BeamColor = new Color(0.2f, 0.6f, 1f, 0.9f);

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
        _lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;

        EnsureBeamMaterial();
    }

    private void Start()
    {
        // Pipeline/shaders can be ready after Awake; fix pink error material if needed.
        EnsureBeamMaterial();
    }

    void EnsureBeamMaterial()
    {
        if (_lineRenderer == null) return;

        bool needsReplace = _lineRenderer.sharedMaterial == null;
        if (!needsReplace && _lineRenderer.sharedMaterial != null)
        {
            var sh = _lineRenderer.sharedMaterial.shader;
            if (sh == null || !sh.isSupported)
                needsReplace = true;
            else if (sh.name != null && (sh.name.Contains("Hidden") && sh.name.Contains("Error")))
                needsReplace = true;
        }

        if (!needsReplace)
            return;

        Shader shader = ResolveBeamShader();
        if (shader == null)
            return;

        var mat = new Material(shader);
        ApplyBeamColor(mat);
        _lineRenderer.sharedMaterial = mat;
    }

    static Shader ResolveBeamShader()
    {
        bool urp = GraphicsSettings.currentRenderPipeline != null;
        if (urp)
        {
            var s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s != null && s.isSupported) return s;
            s = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (s != null && s.isSupported) return s;
        }

        var fallback = Shader.Find("Sprites/Default")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Universal Render Pipeline/Unlit");
        if (fallback != null && fallback.isSupported)
            return fallback;
        return null;
    }

    static void ApplyBeamColor(Material mat)
    {
        if (mat == null) return;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", BeamColor);
        else if (mat.HasProperty("_Color"))
            mat.color = BeamColor;
        else
            mat.color = BeamColor;
    }

    private void Update()
    {
        if (_lineRenderer == null || !_lineRenderer.enabled) return;

        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * beamLength;
        _lineRenderer.SetPosition(0, start);
        _lineRenderer.SetPosition(1, end);
    }
}
