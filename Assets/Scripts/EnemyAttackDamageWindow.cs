using UnityEngine;

/// <summary>
/// Defines when an enemy may deal contact damage to the player.
/// AttackWindowOnly: only during OpenDamageWindow..CloseDamageWindow (animation events) or normalized-time fallback.
/// AnyContact: touch damage allowed anytime (e.g. tutorial dummy) without an active strike window.
/// </summary>
public enum EnemyContactDamageMode
{
    AttackWindowOnly,
    AnyContact
}

/// <summary>
/// Gates player damage to an explicit strike window. Call OpenDamageWindow / CloseDamageWindow from attack animation events.
/// The optional attack trigger collider is enabled only while the window is open (AttackWindowOnly mode).
/// </summary>
public class EnemyAttackDamageWindow : MonoBehaviour
{
    [Header("Mode")]
    public EnemyContactDamageMode contactMode = EnemyContactDamageMode.AttackWindowOnly;

    [Header("Hit volume")]
    [Tooltip("Trigger collider that must overlap the player when using AttackWindowOnly (optional; if null, any enemy collider counts while window is open)")]
    public Collider attackDamageCollider;
    [Tooltip("When false, body hits count if attack volume bounds overlap the player while the window is active.")]
    public bool requireExactHitCollider = false;
    [Tooltip("Extra meters added to attack collider bounds for overlap checks (VR / physics jitter).")]
    [Min(0f)]
    public float overlapBoundsPadding = 0.06f;

    [Header("Safety")]
    [Tooltip("Auto-close if CloseDamageWindow is never called (stuck animation, missing event)")]
    public float maxWindowDuration = 1f;

    [Header("Fallback (no animation events)")]
    public bool useNormalizedTimeFallback;
    public Animator animator;
    [Tooltip("State name or sub-state name to match (layer 0)")]
    public string attackStateName = "Attack";
    public int animatorLayer;
    [Range(0f, 1f)] public float normalizedTimeOpen = 0.18f;
    [Range(0f, 1f)] public float normalizedTimeClose = 0.72f;

    public bool IsDamageActive { get; private set; }

    private float _windowOpenedAt = -999f;
    private int _attackStateHash;

    void Awake()
    {
        _attackStateHash = Animator.StringToHash(attackStateName);
        ApplyColliderForMode();
    }

    void Update()
    {
        if (contactMode != EnemyContactDamageMode.AttackWindowOnly)
            return;

        if (useNormalizedTimeFallback && animator != null && animatorLayer >= 0 && animatorLayer < animator.layerCount)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(animatorLayer);
            bool inAttack = info.shortNameHash == _attackStateHash || info.IsName(attackStateName);
            if (inAttack)
            {
                float nt = info.normalizedTime % 1f;
                bool inWindow = nt >= normalizedTimeOpen && nt <= normalizedTimeClose;
                if (inWindow && !IsDamageActive)
                    OpenDamageWindow();
                else if (!inWindow && IsDamageActive)
                    CloseDamageWindow();
            }
            else if (IsDamageActive)
                CloseDamageWindow();
        }

        if (IsDamageActive && maxWindowDuration > 0f && Time.time - _windowOpenedAt >= maxWindowDuration)
            CloseDamageWindow();
    }

    /// <summary>Animation event: start of damage frames.</summary>
    public void OpenDamageWindow()
    {
        if (contactMode != EnemyContactDamageMode.AttackWindowOnly)
            return;
        if (IsDamageActive)
            return;
        IsDamageActive = true;
        _windowOpenedAt = Time.time;
        if (attackDamageCollider != null)
            attackDamageCollider.enabled = true;
    }

    /// <summary>Animation event: end of damage frames.</summary>
    public void CloseDamageWindow()
    {
        if (!IsDamageActive)
            return;
        IsDamageActive = false;
        if (attackDamageCollider != null && contactMode == EnemyContactDamageMode.AttackWindowOnly)
            attackDamageCollider.enabled = false;
    }

    private void ApplyColliderForMode()
    {
        if (attackDamageCollider == null) return;
        if (contactMode == EnemyContactDamageMode.AttackWindowOnly)
            attackDamageCollider.enabled = IsDamageActive;
        else
            attackDamageCollider.enabled = false;
    }

    public bool IsHitFromAttackVolume(Collider hitCollider)
    {
        if (hitCollider == null) return false;
        if (contactMode == EnemyContactDamageMode.AnyContact)
            return true;
        if (attackDamageCollider == null)
            return true;
        if (hitCollider == attackDamageCollider)
            return true;
        return hitCollider.transform.IsChildOf(attackDamageCollider.transform);
    }

    /// <summary>
    /// True if the attack damage collider world bounds intersect the player bounds (after padding). Only while AttackWindowOnly and damage is active.
    /// </summary>
    public bool AttackVolumeOverlapsPlayerBounds(Bounds playerBounds)
    {
        if (contactMode != EnemyContactDamageMode.AttackWindowOnly || !IsDamageActive)
            return false;
        if (attackDamageCollider == null)
            return false;
        Bounds attack = attackDamageCollider.bounds;
        if (overlapBoundsPadding > 0f)
            attack.Expand(overlapBoundsPadding);
        return attack.Intersects(playerBounds);
    }

    public bool AttackVolumeOverlapsPlayerBounds(Collider playerBody)
    {
        if (playerBody == null) return false;
        return AttackVolumeOverlapsPlayerBounds(playerBody.bounds);
    }

    void OnValidate()
    {
        if (normalizedTimeClose < normalizedTimeOpen)
            normalizedTimeClose = normalizedTimeOpen;
    }
}
