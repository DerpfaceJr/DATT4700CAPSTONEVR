using UnityEngine;

/// <summary>
/// Helper script to automatically add VRGrabThrow to hand anchors
/// Attach this to any GameObject in the scene and it will set up grab/throw on Start
/// </summary>
public class SetupVRGrabThrow : MonoBehaviour
{
    void Start()
    {
        // Find hand anchors
        GameObject leftHand = GameObject.Find("LeftHandAnchor");
        GameObject rightHand = GameObject.Find("RightHandAnchor");
        
        if (leftHand != null)
        {
            if (leftHand.GetComponent<VRGrabThrow>() == null)
            {
                VRGrabThrow grabThrow = leftHand.AddComponent<VRGrabThrow>();
                grabThrow.grabDistance = 2.0f;
                grabThrow.throwForce = 1.0f;
                Debug.Log("Added VRGrabThrow to LeftHandAnchor");
            }
            else
            {
                Debug.Log("LeftHandAnchor already has VRGrabThrow component");
            }
        }
        else
        {
            Debug.LogWarning("LeftHandAnchor not found!");
        }
        
        if (rightHand != null)
        {
            if (rightHand.GetComponent<VRGrabThrow>() == null)
            {
                VRGrabThrow grabThrow = rightHand.AddComponent<VRGrabThrow>();
                grabThrow.grabDistance = 2.0f;
                grabThrow.throwForce = 1.0f;
                Debug.Log("Added VRGrabThrow to RightHandAnchor");
            }
            else
            {
                Debug.Log("RightHandAnchor already has VRGrabThrow component");
            }
        }
        else
        {
            Debug.LogWarning("RightHandAnchor not found!");
        }
    }
}
