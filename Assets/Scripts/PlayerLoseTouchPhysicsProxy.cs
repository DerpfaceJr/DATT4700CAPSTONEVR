using UnityEngine;

/// <summary>
/// Forwards physics messages from the GameObject that owns CharacterController (movement body)
/// to <see cref="PlayerLoseOnEnemyTouch"/> on the avatar/health object (often a sibling under the XR rig).
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerLoseTouchPhysicsProxy : MonoBehaviour
{
    public PlayerLoseOnEnemyTouch target;

    void OnTriggerEnter(Collider other)
    {
        if (target != null)
            target.ForwardTriggerFromProxy(other);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (target != null)
            target.ForwardControllerHitFromProxy(hit);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (target != null)
            target.ForwardCollisionFromProxy(collision);
    }
}
