using UnityEngine;

[System.Serializable]
public class VRMap
{
    public Transform vrTarget;
    public Transform ikTarget;
    public Vector3 trackingPositionOffset;
    public Vector3 trackingRotationOffset;
    public void Map()
    {
        ikTarget.position = vrTarget.TransformPoint(trackingPositionOffset);
        ikTarget.rotation = vrTarget.rotation * Quaternion.Euler(trackingRotationOffset);
    }
}

public class IKTargetFollowVRRig : MonoBehaviour
{
    [Range(0,1)]
    public float turnSmoothness = 0.1f;
    public VRMap head;
    public VRMap leftHand;
    public VRMap rightHand;

    public Vector3 headBodyPositionOffset;
    public float headBodyYawOffset;

    // Update is called once per frame
    void LateUpdate()
    {
        // Check if VR targets are null (enemy mode - IK can be driven by seek/attack script if needed)
        if (head.vrTarget == null || leftHand.vrTarget == null || rightHand.vrTarget == null)
        {
            // Enemy mode: Skip VR mapping
            // Only update body position from head IK target if it exists
            //if (head.ikTarget != null)
            //{
            //    transform.position = head.ikTarget.position + headBodyPositionOffset;
            //}
            return;
        }
        
        // Player mode: Use VR tracking to drive IK targets
        transform.position = head.ikTarget.position + headBodyPositionOffset;
        float yaw = head.vrTarget.eulerAngles.y;
        transform.rotation = Quaternion.Lerp(transform.rotation,Quaternion.Euler(transform.eulerAngles.x, yaw, transform.eulerAngles.z),turnSmoothness);

        head.Map();
        leftHand.Map();
        rightHand.Map();
    }
}
