using UnityEngine;

/// <summary>
/// Locks a minimap icon sprite to always face straight up (for top-down minimap camera)
/// and maintains consistent world-space scale regardless of parent transform changes.
/// Prevents icon distortion when the parent weapon is rotated, grabbed, or thrown.
/// </summary>
public class LockIconRotation : MonoBehaviour
{
    [Tooltip("World rotation for the icon. (90,0,0) = faces up for a top-down camera.")]
    public Vector3 targetEulerAngles = new Vector3(90f, 0f, 0f);

    [Tooltip("Desired world-space uniform scale for the icon. 0 = auto-detect from Start().")]
    public float worldScale = 0f;

    private Vector3 initialLossyScale;

    void Start()
    {
        initialLossyScale = transform.lossyScale;
        if (worldScale <= 0f)
            worldScale = Mathf.Max(Mathf.Abs(initialLossyScale.x), 0.01f);
    }

    void LateUpdate()
    {
        // Lock world rotation so icon always faces the minimap camera
        transform.rotation = Quaternion.Euler(targetEulerAngles);

        // Compensate for parent scale changes to keep icon size consistent
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        float sx = Mathf.Abs(parentScale.x) > 0.001f ? worldScale / Mathf.Abs(parentScale.x) : worldScale;
        float sy = Mathf.Abs(parentScale.y) > 0.001f ? worldScale / Mathf.Abs(parentScale.y) : worldScale;
        float sz = Mathf.Abs(parentScale.z) > 0.001f ? worldScale / Mathf.Abs(parentScale.z) : worldScale;
        transform.localScale = new Vector3(sx, sy, sz);
    }
}
