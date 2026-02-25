using UnityEngine;

/// <summary>
/// Marks an enemy as a mini-boss. Increases touch damage dealt to player.
/// Added automatically by EnemySpawner when spawning a mini-boss.
/// </summary>
public class MiniBossMarker : MonoBehaviour
{
    [Tooltip("Damage multiplier applied to player touch damage")]
    public float damageMultiplier = 2f;

    /// <summary>True if this enemy is a mini-boss.</summary>
    public bool IsMiniBoss => true;
}
