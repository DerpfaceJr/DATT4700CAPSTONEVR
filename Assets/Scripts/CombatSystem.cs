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
    [Header("Damage Settings")]
    [Tooltip("Fallback damage multiplier for velocity-based projectile damage (when throwable has no fixed damage)")]
    public float damageMultiplier = 1f;
    public float minDamageVelocity = 2f;

    [Header("Instakill Settings")]
    public bool instakillEnabled = true;
    public float instakillMinVelocity = 1.5f;

    /* =========================================================
     * MELEE SETTINGS
     * ========================================================= */
    [Header("Melee Settings")]
    public bool meleeWeaponsEnabled = true;
    [Tooltip("Fallback damage when a melee weapon has no SwordPhysicsFix.damagePerHit or it is <= 0")]
    public float meleeDamagePerHitFallback = 25f;
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

        // Use the hitting object's speed, not relative velocity, so enemies don't die
        // when walking into stationary throwables or weapons on the ground
        float objectSpeed = rb.linearVelocity.magnitude;
        float requiredVelocity = instakillEnabled ? instakillMinVelocity : minDamageVelocity;
        if (objectSpeed < requiredVelocity) return;

        // Use GetComponentInParent so colliders on child bones still find the root EnemyController
        EnemyController enemy = GetComponentInParent<EnemyController>();
        if (enemy == null) return;

        if (instakillEnabled)
        {
            enemy.TakeDamage(enemy.maxHealth * 100f);
            Debug.Log($"[CombatSystem] PROJECTILE INSTAKILL (v={objectSpeed:F2})");
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
                damage = objectSpeed * rb.mass * mult;
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
