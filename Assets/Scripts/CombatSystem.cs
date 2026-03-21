using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles melee (sword) and projectile damage
/// Fully compatible with EnemySpawner + SwordPhysicsFix
/// </summary>
public class CombatSystem : MonoBehaviour
{
    /* =========================================================
     * VARIABLES REQUIRED BY EnemySpawner (DO NOT REMOVE)
     * ========================================================= */
    [Header("Projectile Damage (to enemies)")]
    [Tooltip("Fallback damage multiplier for velocity-based projectile damage (when throwable has no fixed damagePerHit)")]
    public float damageMultiplier = 1f;
    [Tooltip("Min projectile speed to register a hit")]
    public float minDamageVelocity = 1.5f;

    [Header("Instakill (disabled by default — use per-throwable damagePerHit instead)")]
    public bool instakillEnabled = false;
    public float instakillMinVelocity = 1.5f;

    /* =========================================================
     * MELEE SETTINGS
     * ========================================================= */
    [Header("Melee Settings")]
    public bool meleeWeaponsEnabled = true;
    [Tooltip("Fallback damage when a melee weapon has no SwordPhysicsFix.damagePerHit or it is <= 0 (34 ≈ 3 hits vs 100 HP)")]
    public float meleeDamagePerHitFallback = 34f;
    public float meleeCooldown = 0.3f;

    private HashSet<Rigidbody> recentWeapons = new HashSet<Rigidbody>();

    /* =========================================================
     * MELEE — TRIGGER BASED (HELD SWORD)
     * ========================================================= */
    private void OnTriggerEnter(Collider other)
    {
        if (!meleeWeaponsEnabled) return;

        Rigidbody weaponRb = other.attachedRigidbody;
        if (weaponRb == null) return;

        if (recentWeapons.Contains(weaponRb)) return;

        SwordPhysicsFix sword = weaponRb.GetComponent<SwordPhysicsFix>();
        if (sword == null) return;

        // ONLY blade colliders
        if (!sword.IsBladeCollider(other)) return;

        float damage = sword.damagePerHit > 0f ? sword.damagePerHit : meleeDamagePerHitFallback;

        // Use GetComponentInParent so colliders on child bones still find the root EnemyController
        EnemyController enemy = GetComponentInParent<EnemyController>();
        if (enemy == null) return;

        enemy.TakeDamage(damage);
        Debug.Log($"[CombatSystem] MELEE HIT for {damage} dmg (HP: {enemy.currentHealth}/{enemy.maxHealth})");

        // Sync durability: only count when damage is actually dealt (1 hit = 1 durability tick)
        MeleeDurability durability = weaponRb.GetComponentInChildren<MeleeDurability>();
        if (durability != null)
            durability.RecordHit();

        recentWeapons.Add(weaponRb);
        StartCoroutine(ClearWeaponCooldown(weaponRb, meleeCooldown));
    }

    /* =========================================================
     * PROJECTILES / THROWN OBJECTS — COLLISION BASED
     * Only damage when the throwable/weapon is moving fast (was thrown),
     * not when the enemy walks into stationary objects.
     * ========================================================= */
    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb == null) return;

        // Sword handled by trigger
        if (rb.CompareTag("Sword")) return;

        // Healing projectiles (e.g. apple) heal player instead of damaging enemy
        if (rb.GetComponent<HealingProjectile>() != null) return;

        // Use object speed; when projectile had velocity, also consider relativeVelocity as fallback
        // (physics may reduce velocity at impact, making hits more reliable)
        float objectSpeed = rb.linearVelocity.magnitude;
        float relativeSpeed = collision.relativeVelocity.magnitude;
        float effectiveSpeed = objectSpeed;
        if (objectSpeed > 0.5f && relativeSpeed > objectSpeed)
            effectiveSpeed = relativeSpeed;

        float requiredVelocity = instakillEnabled ? instakillMinVelocity : minDamageVelocity;
        if (effectiveSpeed < requiredVelocity) return;

        // Use GetComponentInParent so colliders on child bones still find the root EnemyController
        EnemyController enemy = GetComponentInParent<EnemyController>();
        if (enemy == null) return;

        if (instakillEnabled)
        {
            enemy.TakeDamage(enemy.maxHealth * 100f);
            Debug.Log($"[CombatSystem] PROJECTILE INSTAKILL (v={effectiveSpeed:F2})");
        }
        else
        {
            float damage;
            BuildingBlockProjectile projectile = rb.GetComponent<BuildingBlockProjectile>();
            if (projectile != null && projectile.damagePerHit > 0f)
            {
                damage = projectile.damagePerHit;
            }
            else
            {
                float mult = (projectile != null && projectile.damageMultiplier > 0f) ? projectile.damageMultiplier : damageMultiplier;
                damage = effectiveSpeed * rb.mass * mult;
            }
            enemy.TakeDamage(damage);
            Debug.Log($"[CombatSystem] PROJECTILE HIT for {damage} dmg (HP: {enemy.currentHealth}/{enemy.maxHealth})");
        }
    }

    /* ========================================================= */
    private IEnumerator ClearWeaponCooldown(Rigidbody rb, float delay)
    {
        yield return new WaitForSeconds(delay);
        recentWeapons.Remove(rb);
    }
}
