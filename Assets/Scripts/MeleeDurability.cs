using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR || UNITY_ANDROID
using Oculus.Interaction;
#endif

public class MeleeDurability : MonoBehaviour
{
    [Header("Durability")]
    public int hitCount = 0;
    public int hitCountMax = 5;

    [Header("Respawn")]
    public float respawnDelay = 2f;
    [Tooltip("Optional: use this transform's position/rotation for respawn (e.g. spawn point). If null, uses position at Start().")]
    public Transform spawnPoint;

    [Header("Effects")]
    public ParticleSystem breakEffect;

    private Vector3 startPosition;
    private Quaternion startRotation;

    [Header("Mini Map Icons")]
    public SpriteRenderer iconRenderer;

    [Header("Grab Reference")]
    public SwordPhysicsFix meleeObj;

    [Header("Durability Indicator UI")]
    public Image weaponIcon;
    public Sprite weaponUISprite;
    public TextMeshProUGUI durabilityText;

    void Start()
    {
        // Auto-find SwordPhysicsFix if not assigned
        if (meleeObj == null)
            meleeObj = GetComponent<SwordPhysicsFix>();
        if (meleeObj == null)
            meleeObj = GetComponentInParent<SwordPhysicsFix>();

        if (weaponIcon != null)
        {
            weaponIcon.sprite = weaponUISprite;
            weaponIcon.gameObject.SetActive(false);
        }

        if (spawnPoint != null)
        {
            startPosition = spawnPoint.position;
            startRotation = spawnPoint.rotation;
        }
        else
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
        }
        ResetWeapon();
    }

    private void Update()
    {
        if (meleeObj == null)
            return;

        if (meleeObj.isGrabbed)
        {
            // Hide minimap icon
            if (iconRenderer != null)
                iconRenderer.enabled = false;

            // Show durability indicator
            if (weaponIcon != null)
            {
                weaponIcon.gameObject.SetActive(true);
                if (durabilityText != null)
                    durabilityText.text = $"x{hitCountMax - hitCount}";
            }
               
        }
        else
        {
            // Show minimap icon
            if (iconRenderer != null)
                iconRenderer.enabled = true;

            // Only hide durability indicator if NO weapon is grabbed
            // (otherwise another weapon's Update would hide it after we show it)
            if (weaponIcon != null)
            {
                bool anyOtherGrabbed = false;
                foreach (var d in FindObjectsOfType<MeleeDurability>())
                {
                    if (d != this && d.meleeObj != null && d.meleeObj.isGrabbed)
                    {
                        anyOtherGrabbed = true;
                        break;
                    }
                }
                if (!anyOtherGrabbed)
                    weaponIcon.gameObject.SetActive(false);
            }
        }
    }


    /// <summary>
    /// Called by CombatSystem when this weapon deals melee damage.
    /// Keeps durability in sync with actual hits (1 damage event = 1 durability tick).
    /// </summary>
    public void RecordHit()
    {
        hitCount++;
        if (hitCount >= hitCountMax)
        {
            StartCoroutine(BreakAndRespawn());
        }
    }

    IEnumerator BreakAndRespawn()
    {
        yield return null;

        yield return new WaitForEndOfFrame();

        if (this == null) yield break;

        // Spawn particle BEFORE disabling
        if (breakEffect != null)
        {
            ParticleSystem fx = Instantiate(breakEffect, transform.position, Quaternion.identity);
            Destroy(fx.gameObject, 2f);
        }

        // Force release if grabbed (prevents weapon snapping back to hand)
#if UNITY_EDITOR || UNITY_ANDROID
        if (meleeObj != null && meleeObj.isGrabbed)
        {
            var grabbable = meleeObj.GetComponent<Grabbable>();
            if (grabbable != null)
            {
                grabbable.enabled = false;
                grabbable.enabled = true;
            }
        }
#endif

        // Disable weapon visuals + collider
        SetWeaponActive(false);

        yield return new WaitForSeconds(respawnDelay);

        if (this == null) yield break;
        ResetWeapon();
    }

    void SetWeaponActive(bool active)
    {
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = active;

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = active;
    }

    void ResetWeapon()
    {
        hitCount = 0;
        transform.position = startPosition;
        transform.rotation = startRotation;

        if (durabilityText != null)
            durabilityText.text = $"x{hitCountMax}";

        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            Physics.SyncTransforms();
        }

        SetWeaponActive(true);

        if (iconRenderer != null)
            iconRenderer.enabled = true;
    }
}
