using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Drives enemy Animator based on movement state.
/// Sets Speed parameter when walking toward player (NavMeshAgent velocity).
/// Does not use VR IK - uses Animator-driven locomotion only.
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemyAnimatorController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Animator (auto-found in children if not set)")]
    public Animator animator;
    
    [Tooltip("NavMeshAgent for velocity (auto-found if not set)")]
    public NavMeshAgent navAgent;
    
    [Header("Settings")]
    [Tooltip("Animator parameter name for movement speed")]
    public string speedParameterName = "Speed";
    
    [Tooltip("Scale factor for velocity -> Speed (tune to match animation)")]
    public float speedScale = 1f;
    
    [Tooltip("Velocity magnitude below this = idle")]
    public float idleThreshold = 0.1f;
    
    [Tooltip("When true, always stay in Idle (Speed=0). Used for Round 1 static enemy.")]
    public bool forceIdle = false;
    
    private int _speedHash;
    
    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        if (animator == null)
        {
            Debug.LogWarning($"[EnemyAnimatorController] No Animator found on {gameObject.name}");
            enabled = false;
            return;
        }
        
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();
        
        _speedHash = Animator.StringToHash(speedParameterName);
    }
    
    void Update()
    {
        if (animator == null) return;
        
        float speed = 0f;
        if (!forceIdle && navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            Vector3 vel = navAgent.velocity;
            vel.y = 0f;
            speed = vel.magnitude * speedScale;
        }
        
        if (speed < idleThreshold)
            speed = 0f;
        
        animator.SetFloat(_speedHash, speed);
    }
}
