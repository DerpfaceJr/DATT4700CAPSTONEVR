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

        EnemyController enemy = GetComponent<EnemyController>();
        if (enemy == null) return;

        enemy.TakeDamage(enemy.maxHealth * 100f);
        Debug.Log("[CombatSystem] MELEE SWORD KILL");

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

        EnemyController enemy = GetComponent<EnemyController>();
        if (enemy == null) return;

        if (instakillEnabled)
        {
            enemy.TakeDamage(enemy.maxHealth * 100f);
            Debug.Log($"[CombatSystem] PROJECTILE INSTAKILL (v={objectSpeed:F2})");
        }
        else
        {
            float damage = objectSpeed * rb.mass * damageMultiplier;
            enemy.TakeDamage(damage);
        }
    }

    /* ========================================================= */
    private IEnumerator ClearWeaponCooldown(Rigidbody rb, float delay)
    {
        yield return new WaitForSeconds(delay);
        recentWeapons.Remove(rb);
    }
}
