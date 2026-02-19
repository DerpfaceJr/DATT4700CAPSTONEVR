using UnityEngine;
using System.Collections;

public class MeleeDurability : MonoBehaviour
{
    [Header("Durability")]
    public int hitCount = 0;
    public int hitCountMax = 5;

    [Header("Respawn")]
    public float respawnDelay = 2f;

    [Header("Effects")]
    public ParticleSystem breakEffect;

    private Vector3 startPosition;
    private Quaternion startRotation;

    [Header("Icons")]
    public SpriteRenderer iconRenderer;
    public SwordPhysicsFix meleeObj;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        ResetWeapon();
    }

    private void Update()
    {
        if (meleeObj != null)
            iconRenderer.enabled = !meleeObj.isGrabbed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        hitCount++;

        if (hitCount >= hitCountMax)
        {
            StartCoroutine(BreakAndRespawn());
        }
    }

    IEnumerator BreakAndRespawn()
    {
        // Spawn particle BEFORE disabling
        if (breakEffect != null)
        {
            ParticleSystem fx = Instantiate(
                breakEffect,
                transform.position,
                Quaternion.identity
            );
            Destroy(fx.gameObject, 2f);
        }

        // Disable weapon visuals + collider
        SetWeaponActive(false);

        yield return new WaitForSeconds(respawnDelay);

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

        SetWeaponActive(true);

        if (iconRenderer != null)
            iconRenderer.enabled = true;
    }
}
