using UnityEngine;

/// <summary>
/// Fixes leg bone positioning for rigged characters
/// Ensures LeftUpLeg and RightUpLeg are properly separated from spine
/// </summary>
public class LegBonePositionFixer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Distance between left and right upper legs (hip width)")]
    public float hipWidth = 0.2f;
    
    [Tooltip("Forward offset for legs relative to spine")]
    public float legForwardOffset = 0.05f;
    
    [Tooltip("Automatically fix leg positions on Start")]
    public bool autoFixOnStart = true;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private Transform leftUpLeg;
    private Transform rightUpLeg;
    private Transform hips; // Spine base or hips bone
    
    void Start()
    {
        if (autoFixOnStart)
        {
            FixLegPositions();
        }
    }
    
    /// <summary>
    /// Find and fix leg bone positions
    /// </summary>
    public void FixLegPositions()
    {
        // Find leg bones in hierarchy
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"[LegBonePositionFixer] No Animator found on {gameObject.name}");
            return;
        }
        
        // Try to get bones from Animator (humanoid rig)
        if (animator.isHuman)
        {
            leftUpLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            rightUpLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        }
        else
        {
            // Fallback: Search by name
            leftUpLeg = FindBoneByName("LeftUpLeg");
            rightUpLeg = FindBoneByName("RightUpLeg");
            hips = FindBoneByName("Hips") ?? FindBoneByName("Spine");
        }
        
        if (leftUpLeg == null || rightUpLeg == null)
        {
            Debug.LogWarning($"[LegBonePositionFixer] Could not find leg bones on {gameObject.name}");
            Debug.LogWarning($"  LeftUpLeg: {(leftUpLeg != null ? "found" : "NOT FOUND")}");
            Debug.LogWarning($"  RightUpLeg: {(rightUpLeg != null ? "found" : "NOT FOUND")}");
            return;
        }
        
        if (hips == null)
        {
            Debug.LogWarning($"[LegBonePositionFixer] Could not find hips/spine bone on {gameObject.name}");
            return;
        }
        
        // Store original positions for debug
        Vector3 originalLeftPos = leftUpLeg.position;
        Vector3 originalRightPos = rightUpLeg.position;
        Vector3 hipsPos = hips.position;
        
        // Check if legs are at same position as spine (problem state)
        float leftDist = Vector3.Distance(leftUpLeg.position, hipsPos);
        float rightDist = Vector3.Distance(rightUpLeg.position, hipsPos);
        
        if (showDebugInfo)
        {
            Debug.Log($"[LegBonePositionFixer] {gameObject.name} leg check:");
            Debug.Log($"  Hips position: {hipsPos}");
            Debug.Log($"  LeftUpLeg position: {originalLeftPos}, distance from hips: {leftDist:F3}");
            Debug.Log($"  RightUpLeg position: {originalRightPos}, distance from hips: {rightDist:F3}");
        }
        
        // If legs are too close to hips (less than 5cm), fix them
        if (leftDist < 0.05f || rightDist < 0.05f)
        {
            Debug.Log($"[LegBonePositionFixer] Legs too close to spine! Fixing positions...");
            
            // Position legs properly relative to hips
            // Left leg: offset to the left
            Vector3 leftOffset = hips.right * -hipWidth + hips.forward * legForwardOffset;
            leftUpLeg.position = hipsPos + leftOffset;
            
            // Right leg: offset to the right
            Vector3 rightOffset = hips.right * hipWidth + hips.forward * legForwardOffset;
            rightUpLeg.position = hipsPos + rightOffset;
            
            if (showDebugInfo)
            {
                Debug.Log($"[LegBonePositionFixer] Fixed leg positions:");
                Debug.Log($"  LeftUpLeg: {originalLeftPos} → {leftUpLeg.position}");
                Debug.Log($"  RightUpLeg: {originalRightPos} → {rightUpLeg.position}");
                Debug.Log($"  Hip width: {hipWidth}, Forward offset: {legForwardOffset}");
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log($"[LegBonePositionFixer] Leg positions look OK (distance from hips > 0.05)");
            }
        }
    }
    
    /// <summary>
    /// Find bone by name in hierarchy
    /// </summary>
    private Transform FindBoneByName(string boneName)
    {
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t.name.Equals(boneName, System.StringComparison.OrdinalIgnoreCase) ||
                t.name.Contains(boneName))
            {
                return t;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Manually trigger fix (can be called from Inspector button or code)
    /// </summary>
    [ContextMenu("Fix Leg Positions Now")]
    public void FixNow()
    {
        FixLegPositions();
    }
    
    /// <summary>
    /// Draw gizmos showing leg and hip positions
    /// </summary>
    void OnDrawGizmos()
    {
        if (leftUpLeg != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(leftUpLeg.position, 0.05f);
        }
        
        if (rightUpLeg != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rightUpLeg.position, 0.05f);
        }
        
        if (hips != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(hips.position, 0.03f);
            
            // Draw lines from hips to legs
            if (leftUpLeg != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(hips.position, leftUpLeg.position);
            }
            if (rightUpLeg != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(hips.position, rightUpLeg.position);
            }
        }
    }
}
