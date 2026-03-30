using UnityEngine;

/// <summary>
/// Keeps a minimap icon independent of its weapon - reparents to world space so the weapon's
/// transform (rotation, scale, grab) never affects the icon. Icon always has world X=90 Y=0 Z=0
/// and uniform scale. When the tracked object is destroyed (e.g. healing apple used up), this
/// component destroys the icon so it does not linger on the minimap.
/// </summary>
public class LockIconRotation : MonoBehaviour
{
    private static readonly Quaternion LockedWorldRotation = Quaternion.Euler(90f, 0f, 0f);

    [Tooltip("World-space uniform scale. 0 = auto from Start.")]
    public float worldScale = 0f;

    [Tooltip("Height offset above weapon position.")]
    public float heightOffset = 0.5f;

    private Transform weaponParent;
    private bool reparented;
    private SpriteRenderer iconSprite;

    void Start()
    {
        weaponParent = transform.parent;
        if (weaponParent == null) return;

        if (worldScale <= 0f)
        {
            Vector3 ls = transform.lossyScale;
            worldScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z), 0.01f);
        }

        ReparentToWorld();
        iconSprite = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        if (reparented && weaponParent != null)
            ReparentToWorld();
    }

    void LateUpdate()
    {
        // Tracked object was destroyed (e.g. healing apple after use) — remove orphaned minimap icon.
        if (weaponParent == null)
        {
            if (reparented)
                Destroy(gameObject);
            return;
        }

        // Healing apple hidden after use (SetActive false) until round reset — hide minimap icon while inactive
        if (!weaponParent.gameObject.activeInHierarchy)
        {
            if (iconSprite != null)
                iconSprite.enabled = false;
            return;
        }

        transform.position = weaponParent.position + Vector3.up * heightOffset;
        transform.rotation = LockedWorldRotation;
        transform.localScale = new Vector3(worldScale, worldScale, worldScale);
    }

    private void ReparentToWorld()
    {
        GameObject container = GameObject.Find("MeleeIconContainer");
        if (container == null)
        {
            container = new GameObject("MeleeIconContainer");
        }
        transform.SetParent(container.transform, true);
        reparented = true;
    }
}
