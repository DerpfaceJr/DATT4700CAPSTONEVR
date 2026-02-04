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
     * ========================================================= */
    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb == null) return;

        // Sword handled by trigger
        if (rb.CompareTag("Sword")) return;

        float velocity = collision.relativeVelocity.magnitude;
        float requiredVelocity = instakillEnabled ? instakillMinVelocity : minDamageVelocity;
        if (velocity < requiredVelocity) return;

        EnemyController enemy = GetComponent<EnemyController>();
        if (enemy == null) return;

        if (instakillEnabled)
        {
            enemy.TakeDamage(enemy.maxHealth * 100f);
            Debug.Log($"[CombatSystem] PROJECTILE INSTAKILL (v={velocity:F2})");
        }
        else
        {
            float damage = velocity * rb.mass * damageMultiplier;
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
