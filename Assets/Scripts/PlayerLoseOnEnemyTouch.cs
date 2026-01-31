using UnityEngine;

/// <summary>
/// When an enemy touches the player, deal 1 damage (player has 1 HP = game over).
/// Works when the player walks into an enemy (OnControllerColliderHit) and when
/// the enemy walks into the player (OnTriggerEnter via a body trigger collider).
/// Attach to the same GameObject as CharacterController (e.g. VRMovementController).
/// </summary>
public class PlayerLoseOnEnemyTouch : MonoBehaviour
{
    [Tooltip("Damage dealt when an enemy touches the player (1 = instant death with 1 HP)")]
    public float touchDamage = 1f;

    [Tooltip("Tag used for enemy GameObjects")]
    public string enemyTag = "Enemy";

    [Tooltip("Extra radius/height on the body trigger so enemies are detected from the sides (not only when jumping on them). 0 = use CharacterController size only.")]
    [Range(0f, 0.5f)]
    public float triggerPadding = 0.15f;

    private PlayerHealth playerHealth;
    private CapsuleCollider bodyTrigger;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
        }
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }
        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerLoseOnEnemyTouch: No PlayerHealth found on player. Enemy touch will not deal damage.");
        }

        EnsureBodyTrigger();
    }

    /// <summary>
    /// Ensures a trigger collider exists so we detect enemies walking INTO the player
    /// (OnControllerColliderHit only fires when the player moves into the enemy).
    /// </summary>
    private void EnsureBodyTrigger()
    {
        CapsuleCollider existingTrigger = GetComponent<CapsuleCollider>();
        if (existingTrigger != null && existingTrigger.isTrigger)
        {
            bodyTrigger = existingTrigger;
            return;
        }

        CharacterController cc = GetComponent<CharacterController>();
        if (cc == null) return;

        bodyTrigger = gameObject.AddComponent<CapsuleCollider>();
        bodyTrigger.isTrigger = true;
        bodyTrigger.radius = cc.radius + triggerPadding;
        bodyTrigger.height = cc.height + (triggerPadding * 2f);
        bodyTrigger.center = cc.center;
        bodyTrigger.direction = 1; // Y-axis
    }

    void OnCollisionEnter(Collision collision)
    {
        if (IsEnemy(collision.gameObject) && playerHealth != null)
        {
            playerHealth.TakeDamage(touchDamage);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsEnemy(other.gameObject) && playerHealth != null)
        {
            playerHealth.TakeDamage(touchDamage);
        }
    }

    /// <summary>
    /// Called when CharacterController hits a collider. Use this when the script
    /// is on the same GameObject as the CharacterController (e.g. VRMovementController).
    /// </summary>
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit == null || hit.gameObject == null) return;
        if (IsEnemy(hit.gameObject) && playerHealth != null)
        {
            playerHealth.TakeDamage(touchDamage);
        }
    }

    private bool IsEnemy(GameObject obj)
    {
        if (obj == null) return false;
        Transform t = obj.transform;
        while (t != null)
        {
            if (t.CompareTag(enemyTag)) return true;
            t = t.parent;
        }
        return false;
    }
}
