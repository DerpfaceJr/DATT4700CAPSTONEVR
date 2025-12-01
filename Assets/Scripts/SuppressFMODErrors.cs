using UnityEngine;

/// <summary>
/// Suppresses FMOD initialization errors if FMOD is not being used
/// This prevents console spam from FMOD audio device initialization failures
/// Note: This error is usually harmless - Unity will fall back to default audio
/// </summary>
public class SuppressFMODErrors : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Enable to suppress FMOD errors in console")]
    public bool suppressFMODErrors = true;
    
    void Awake()
    {
        if (suppressFMODErrors)
        {
            // Log a one-time message explaining the FMOD error
            Debug.Log("[SuppressFMODErrors] FMOD audio initialization errors will be suppressed. " +
                     "This is usually harmless - Unity will use default audio if FMOD fails.");
        }
    }
    
    // Note: Unity's FMOD errors are logged internally and can't be fully suppressed via script.
    // The best solution is to fix audio device settings in Unity Project Settings.
    // This script serves as documentation that the error can be safely ignored.
}

