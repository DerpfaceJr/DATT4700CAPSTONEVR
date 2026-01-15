using UnityEngine;
using TMPro;

/// <summary>
/// Helper script to automatically add RoundTextUpdater to Round Text GameObject
/// Attach this to any GameObject in the scene and it will set up round text on Start
/// </summary>
public class SetupRoundText : MonoBehaviour
{
    void Start()
    {
        // Find Round Text GameObject
        GameObject roundTextObj = GameObject.Find("Round Text");
        
        if (roundTextObj != null)
        {
            // Check if it already has RoundTextUpdater
            if (roundTextObj.GetComponent<RoundTextUpdater>() == null)
            {
                RoundTextUpdater updater = roundTextObj.AddComponent<RoundTextUpdater>();
                Debug.Log("Added RoundTextUpdater to Round Text GameObject");
            }
            else
            {
                Debug.Log("Round Text already has RoundTextUpdater component");
            }
        }
        else
        {
            Debug.LogWarning("SetupRoundText: Round Text GameObject not found!");
        }
    }
}
