using UnityEngine;

#if UNITY_EDITOR || UNITY_ANDROID
using Oculus.Interaction;
#endif

public class SwordPhysicsFix : MonoBehaviour
{
    private Rigidbody rb;
    private Grabbable grabbable;
    private bool wasGrabbed;
    public bool isGrabbed;

    [Header("Damage")]
    [Tooltip("Damage per melee hit. Default 34 ≈ 3 hits to kill a 100 HP enemy. Editable per weapon in the Inspector.")]
    public float damagePerHit = 34f;

    [Header("Blade Colliders (Triggers when held)")]
    [Tooltip("ONLY colliders in this list will be triggers when the sword is held")]
    public Collider[] bladeColliders;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();

        rb.useGravity = true;
        rb.isKinematic = false;

        SetBladeTriggers(false);

    }

    void Update()
    {
        if (grabbable == null) return;

        isGrabbed = grabbable.Points.Count > 0;

        if (isGrabbed && !wasGrabbed)
        {
            // Grabbed - play pickup sound
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayWoodSmallPickup();
            rb.isKinematic = true;
            SetBladeTriggers(true);
        }
        else if (!isGrabbed && wasGrabbed)
        {
            // Released
            rb.isKinematic = false;
            rb.useGravity = true;
            SetBladeTriggers(false);

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // VERY IMPORTANT
            Physics.SyncTransforms();

        }

        wasGrabbed = isGrabbed;
    }

    private void SetBladeTriggers(bool value)
    {
        if (bladeColliders == null) return;

        foreach (Collider col in bladeColliders)
        {
            if (col != null)
                col.isTrigger = value;
        }
    }

    public bool IsBladeCollider(Collider col)
    {
        if (bladeColliders == null) return false;

        foreach (Collider blade in bladeColliders)
        {
            if (blade == col)
                return true;
        }
        return false;
    }
}
