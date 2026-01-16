using UnityEngine;

#if UNITY_EDITOR || UNITY_ANDROID
using Oculus.Interaction;
#endif

public class SwordPhysicsFix : MonoBehaviour
{
    private Rigidbody rb;
    private Grabbable grabbable;
    private bool wasGrabbed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();

        rb.useGravity = true;
        rb.isKinematic = false;
    }

    void Update()
    {
        if (grabbable == null) return;

        bool isGrabbed = grabbable.Points.Count > 0;

        if (isGrabbed && !wasGrabbed)
        {
            // Grabbed
            rb.isKinematic = true;
        }
        else if (!isGrabbed && wasGrabbed)
        {
            // Released
            rb.isKinematic = false;
            rb.useGravity = true;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // VERY IMPORTANT
            Physics.SyncTransforms();

        }

        wasGrabbed = isGrabbed;
    }
}
