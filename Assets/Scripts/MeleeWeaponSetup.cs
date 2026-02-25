using UnityEngine;

/// <summary>
/// Auto-configures MeleeDurability on all melee weapons in the scene that have SwordPhysicsFix
/// but are missing MeleeDurability. Place on any persistent GameObject in the SimpleArea scene
/// (e.g. on the RoundManager or a "GameSetup" object).
/// 
/// For each weapon found with SwordPhysicsFix but no MeleeDurability:
///   - Adds MeleeDurability component
///   - Sets default hitCountMax based on weapon type
///   - Wires up the SwordPhysicsFix reference
///   - Creates and attaches a minimap icon SpriteRenderer with LockIconRotation
/// </summary>
public class MeleeWeaponSetup : MonoBehaviour
{
    [Header("Durability Defaults")]
    [Tooltip("Default max hits before weapon breaks (adjustable per weapon type)")]
    public int defaultMaxHits = 5;

    [Tooltip("Respawn delay after breaking")]
    public float respawnDelay = 3f;

    [Header("Minimap Icon")]
    [Tooltip("Optional sprite to use for all melee weapon minimap icons. If null, uses existing or skips.")]
    public Sprite meleeIconSprite;

    [Tooltip("Color for the minimap icon")]
    public Color iconColor = new Color(1f, 0.8f, 0f, 1f);

    [Tooltip("Size of minimap icon")]
    public float iconSize = 0.5f;

    void Start()
    {
        SetupAllMeleeWeapons();
    }

    public void SetupAllMeleeWeapons()
    {
        SwordPhysicsFix[] allMeleeWeapons = FindObjectsOfType<SwordPhysicsFix>();
        int setupCount = 0;

        foreach (SwordPhysicsFix weapon in allMeleeWeapons)
        {
            MeleeDurability durability = weapon.GetComponent<MeleeDurability>();
            if (durability != null) continue; // Already set up

            durability = weapon.gameObject.AddComponent<MeleeDurability>();
            durability.meleeObj = weapon;
            durability.respawnDelay = respawnDelay;

            // Set hit count based on weapon name
            string weaponName = weapon.gameObject.name.ToLower();
            if (weaponName.Contains("pickaxe"))
                durability.hitCountMax = 5;
            else if (weaponName.Contains("crowbar"))
                durability.hitCountMax = 7;
            else if (weaponName.Contains("wrench"))
                durability.hitCountMax = 6;
            else if (weaponName.Contains("mallet") || weaponName.Contains("hammer"))
                durability.hitCountMax = 4;
            else if (weaponName.Contains("sword"))
                durability.hitCountMax = 8;
            else
                durability.hitCountMax = defaultMaxHits;

            // Setup minimap icon if not already present
            EnsureMinimapIcon(weapon.gameObject, durability);

            setupCount++;
            Debug.Log($"[MeleeWeaponSetup] Added MeleeDurability to '{weapon.gameObject.name}' (maxHits={durability.hitCountMax})");
        }

        if (setupCount > 0)
            Debug.Log($"[MeleeWeaponSetup] Set up {setupCount} melee weapons with durability.");
        else
            Debug.Log("[MeleeWeaponSetup] All melee weapons already have MeleeDurability.");
    }

    private void EnsureMinimapIcon(GameObject weapon, MeleeDurability durability)
    {
        // Check if weapon already has a minimap icon sprite renderer
        SpriteRenderer existingIcon = null;
        foreach (Transform child in weapon.transform)
        {
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                existingIcon = sr;
                break;
            }
        }

        if (existingIcon != null)
        {
            durability.iconRenderer = existingIcon;

            // Ensure icon has LockIconRotation
            LockIconRotation lockRot = existingIcon.GetComponent<LockIconRotation>();
            if (lockRot == null)
                existingIcon.gameObject.AddComponent<LockIconRotation>();

            return;
        }

        // No existing icon: create one
        if (meleeIconSprite == null) return;

        GameObject iconObj = new GameObject("MinimapIcon");
        iconObj.transform.SetParent(weapon.transform, false);
        iconObj.transform.localPosition = Vector3.up * 0.5f;

        SpriteRenderer sr2 = iconObj.AddComponent<SpriteRenderer>();
        sr2.sprite = meleeIconSprite;
        sr2.color = iconColor;
        sr2.transform.localScale = Vector3.one * iconSize;
        sr2.sortingOrder = 100;

        LockIconRotation lockRotNew = iconObj.AddComponent<LockIconRotation>();
        lockRotNew.worldScale = iconSize;

        durability.iconRenderer = sr2;
    }
}
