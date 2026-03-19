using UnityEngine;
using System.Collections;
using UnityEngine.UI;

#if UNITY_EDITOR || UNITY_ANDROID
using Oculus.Interaction;
#endif


public class ThrowablesIconControl : MonoBehaviour
{
    [Header("Check Grab")]
    private Grabbable grabbable;
    public bool isGrabbed;

    [Header("Mini Map Icons Control")]
    public SpriteRenderer iconRenderer;

    void Start()
    {
        grabbable = GetComponent<Grabbable>();
    }


    void Update()
    {
        if (grabbable == null) return;

        isGrabbed = grabbable.Points.Count > 0;

        //Grabbed
        if (isGrabbed)
        {
            //Hide minimap icon
            if(iconRenderer != null)
            {
                iconRenderer.enabled = false;
            }
        }

        // Not Grabbed
        else
        {
            //show mini map icon
            if(iconRenderer != null)
            {
                iconRenderer.enabled = true;
            }
        }
    }
}
