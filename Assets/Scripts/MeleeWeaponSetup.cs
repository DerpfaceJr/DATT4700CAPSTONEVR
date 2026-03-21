using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public int defaultMaxHits = 10;

    [Tooltip("Respawn delay after breaking")]
    public float respawnDelay = 3f;

    [Header("Minimap Icon")]
    [Tooltip("Optional sprite to use for all melee weapon minimap icons. If null, uses existing or skips.")]
    public Sprite meleeIconSprite;

    [Tooltip("Color for the minimap icon")]
    public Color iconColor = new Color(1f, 0.8f, 0f, 1f);

    [Tooltip("Size of minimap icon")]
    public float iconSize = 0.5f;

    void Awake()
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
            if (durability != null)
            {
                // Already has durability: ensure icon setup and update hitCountMax if too low
                EnsureMinimapIcon(weapon.gameObject, durability);
                ApplyHitCountMax(weapon.gameObject.name, durability);
                EnsureDurabilityUI(durability);
                continue;
            }

            durability = weapon.gameObject.AddComponent<MeleeDurability>();
            durability.meleeObj = weapon;
            durability.respawnDelay = respawnDelay;

            ApplyHitCountMax(weapon.gameObject.name, durability);

            // Setup minimap icon
            EnsureMinimapIcon(weapon.gameObject, durability);

            // Wire up player canvas durability UI
            EnsureDurabilityUI(durability);

            setupCount++;
            Debug.Log($"[MeleeWeaponSetup] Added MeleeDurability to '{weapon.gameObject.name}' (maxHits={durability.hitCountMax})");
        }

        if (setupCount > 0)
            Debug.Log($"[MeleeWeaponSetup] Set up {setupCount} melee weapons with durability.");
        else
            Debug.Log("[MeleeWeaponSetup] All melee weapons already have MeleeDurability.");
    }

    private void ApplyHitCountMax(string weaponName, MeleeDurability durability)
    {
        // ~3 melee hits to kill 100 HP enemy at 34 dmg. Doubled durability vs prior tuning for more kills per weapon.
        string name = weaponName.ToLower();
        if (name.Contains("pickaxe"))
            durability.hitCountMax = 24;
        else if (name.Contains("crowbar"))
            durability.hitCountMax = 28;
        else if (name.Contains("wrench"))
            durability.hitCountMax = 24;
        else if (name.Contains("mallet") || name.Contains("hammer"))
            durability.hitCountMax = 20;
        else if (name.Contains("sword"))
            durability.hitCountMax = 32;
        else
            durability.hitCountMax = Mathf.Max(defaultMaxHits, 24);
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

            // Ensure icon has LockIconRotation and correct world rotation (X90 Y0 Z0)
            existingIcon.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
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
        iconObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        SpriteRenderer sr2 = iconObj.AddComponent<SpriteRenderer>();
        sr2.sprite = meleeIconSprite;
        sr2.color = iconColor;
        sr2.transform.localScale = Vector3.one * iconSize;
        sr2.sortingOrder = 100;

        LockIconRotation lockRotNew = iconObj.AddComponent<LockIconRotation>();
        lockRotNew.worldScale = iconSize;

        durability.iconRenderer = sr2;
    }

    /// <summary>
    /// Wire the shared player canvas durability UI (weaponIcon, durabilityText) to this weapon
    /// if not already assigned. Finds "Weapon Icon" (Image) and "Durability Indicator" (TextMeshProUGUI).
    /// </summary>
    private void EnsureDurabilityUI(MeleeDurability durability)
    {
        if (durability.weaponIcon != null && durability.durabilityText != null)
            return;

        // Find Player Canvas -> Weapon Icon (Image) and Durability Indicator (TextMeshProUGUI)
        GameObject playerCanvas = GameObject.Find("Player Canvas");
        if (playerCanvas == null) return;

        Transform weaponIconT = playerCanvas.transform.Find("Weapon Icon");
        if (weaponIconT == null) return;

        Image icon = weaponIconT.GetComponent<Image>();
        if (icon == null) icon = weaponIconT.GetComponentInChildren<Image>();

        Transform durabilityT = weaponIconT.Find("Durability Indicator");
        TextMeshProUGUI text = null;
        if (durabilityT != null)
            text = durabilityT.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = weaponIconT.GetComponentInChildren<TextMeshProUGUI>();

        if (durability.weaponIcon == null && icon != null)
            durability.weaponIcon = icon;
        if (durability.durabilityText == null && text != null)
            durability.durabilityText = text;

        // Assign weaponUISprite if missing (from another weapon or minimap icon)
        if (durability.weaponUISprite == null)
        {
            MeleeDurability[] all = FindObjectsOfType<MeleeDurability>();
            foreach (MeleeDurability d in all)
            {
                if (d.weaponUISprite != null)
                {
                    durability.weaponUISprite = d.weaponUISprite;
                    break;
                }
                if (d.iconRenderer != null && d.iconRenderer.sprite != null)
                {
                    durability.weaponUISprite = d.iconRenderer.sprite;
                    break;
                }
            }
        }

        // Apply sprite to icon now (in case MeleeDurability.Start already ran)
        if (durability.weaponIcon != null && durability.weaponUISprite != null)
            durability.weaponIcon.sprite = durability.weaponUISprite;
    }
}
