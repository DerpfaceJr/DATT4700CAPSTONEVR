using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Plays a cutscene when a round is completed
/// Disables player controls during the cutscene, displays UI message, then re-enables them
/// </summary>
public class RoundCompleteCutscene : MonoBehaviour
{
    [Header("Cutscene Settings")]
    [Tooltip("Duration of the cutscene in seconds")]
    public float cutsceneDuration = 3f;
    
    [Tooltip("Enable debug logging")]
    public bool debugMode = false;
    
    [Header("References")]
    [Tooltip("Round Manager (auto-found if not assigned)")]
    public RoundManager roundManager;
    
    [Tooltip("VR Movement Controller (auto-found if not assigned)")]
    public VRMovementController movementController;
    
    [Tooltip("VR Grab Throw scripts (auto-found if not assigned)")]
    public VRGrabThrow[] grabThrowScripts;
    
    [Header("UI Settings")]
    [Tooltip("Round Complete Panel (auto-found if not assigned)")]
    public GameObject roundCompletePanel;
    
    [Tooltip("TextMeshProUGUI component for round text (auto-found if not assigned)")]
    public TextMeshProUGUI roundCompleteText;
    
    [Tooltip("Message format (use {0} for round number)")]
    public string messageFormat = "Round {0} Complete!";
    
    private bool isCutscenePlaying = false;
    private bool movementWasEnabled = true;
    private bool[] grabThrowWasEnabled;


    void Start()
    {
        // Find RoundManager if not assigned
        if (roundManager == null)
        {
            roundManager = FindObjectOfType<RoundManager>();
        }
        
        // Subscribe to round complete event
        if (roundManager != null)
        {
            roundManager.OnRoundComplete += OnRoundComplete;
        }
        else
        {
            Debug.LogError("RoundCompleteCutscene: RoundManager not found!");
        }
        
        // Find VRMovementController if not assigned
        if (movementController == null)
        {
            movementController = FindObjectOfType<VRMovementController>();
        }
        
        // Find VRGrabThrow scripts if not assigned
        if (grabThrowScripts == null || grabThrowScripts.Length == 0)
        {
            grabThrowScripts = FindObjectsOfType<VRGrabThrow>();
        }
        
        // Find RoundCompletePanel if not assigned
        if (roundCompletePanel == null)
        {
            roundCompletePanel = GameObject.Find("RoundCompletePanel");
            
            if (roundCompletePanel == null)
            {
                // Search in all canvases
                Canvas[] canvases = FindObjectsOfType<Canvas>(true);
                foreach (Canvas canvas in canvases)
                {
                    Transform panelTransform = canvas.transform.Find("RoundCompletePanel");
                    if (panelTransform != null)
                    {
                        roundCompletePanel = panelTransform.gameObject;
                        break;
                    }
                }
            }
            
            if (roundCompletePanel != null && debugMode)
            {
                Debug.Log($"RoundCompleteCutscene: Auto-found RoundCompletePanel");
            }
        }
        
        // Find TextMeshProUGUI component if not assigned
        if (roundCompleteText == null && roundCompletePanel != null)
        {
            // Search for "Round Complete Text" child
            roundCompleteText = roundCompletePanel.GetComponentInChildren<TextMeshProUGUI>(true);
            
            if (roundCompleteText != null && debugMode)
            {
                Debug.Log($"RoundCompleteCutscene: Auto-found TextMeshProUGUI component on {roundCompleteText.gameObject.name}");
            }
        }
        
        // Hide panel initially
        if (roundCompletePanel != null)
        {
            roundCompletePanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("RoundCompleteCutscene: RoundCompletePanel not found! Assign 'roundCompletePanel' in Inspector.");
        }
        
        if (roundCompleteText == null)
        {
            Debug.LogWarning("RoundCompleteCutscene: TextMeshProUGUI component not found! Assign 'roundCompleteText' in Inspector.");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from event
        if (roundManager != null)
        {
            roundManager.OnRoundComplete -= OnRoundComplete;
        }
    }
    
    /// <summary>
    /// Called when a round is completed
    /// </summary>
    private void OnRoundComplete(int roundNumber)
    {
        if (isCutscenePlaying) return; // Prevent multiple cutscenes
        
        if (debugMode)
        {
            Debug.Log($"RoundCompleteCutscene: Round {roundNumber} complete! Starting cutscene...");
        }
        
        StartCoroutine(PlayCutscene(roundNumber));
    }
    
    /// <summary>
    /// Play the cutscene
    /// </summary>
    private IEnumerator PlayCutscene(int roundNumber)
    {
        isCutscenePlaying = true;
        
        // Disable player controls
        DisablePlayerControls();
        
        // Show UI message
        ShowRoundCompleteMessage(roundNumber);
        
        // Wait for cutscene duration
        yield return new WaitForSeconds(cutsceneDuration);
        
        // Hide UI message
        HideRoundCompleteMessage();
        
        // Re-enable player controls
        EnablePlayerControls();
        
        // Tell RoundManager to continue to next round
        if (roundManager != null)
        {
            roundManager.ContinueToNextRound();
        }
        
        isCutscenePlaying = false;
        
        if (debugMode)
        {
            Debug.Log("RoundCompleteCutscene: Cutscene finished!");
        }
    }
    
    /// <summary>
    /// Show the round complete message
    /// </summary>
    private void ShowRoundCompleteMessage(int roundNumber)
    {
        // Update text content
        if (roundCompleteText != null)
        {
            roundCompleteText.text = string.Format(messageFormat, roundNumber);
        }

        // Show panel
        if (roundCompletePanel != null)
        {
            roundCompletePanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hide the round complete message
    /// </summary>
    private void HideRoundCompleteMessage()
    {
        if (roundCompletePanel != null)
        {
            roundCompletePanel.SetActive(false);
        }
    }

    
    /// <summary>
    /// Disable player controls
    /// </summary>
    private void DisablePlayerControls()
    {
        // Disable movement
        if (movementController != null)
        {
            movementWasEnabled = movementController.enableSmoothMovement;
            movementController.enableSmoothMovement = false;
        }
        
        // Disable grab/throw
        if (grabThrowScripts != null)
        {
            grabThrowWasEnabled = new bool[grabThrowScripts.Length];
            for (int i = 0; i < grabThrowScripts.Length; i++)
            {
                if (grabThrowScripts[i] != null)
                {
                    grabThrowWasEnabled[i] = grabThrowScripts[i].enabled;
                    grabThrowScripts[i].enabled = false;
                }
            }
        }
    }
    
    /// <summary>
    /// Re-enable player controls
    /// </summary>
    private void EnablePlayerControls()
    {
        // Re-enable movement
        if (movementController != null)
        {
            movementController.enableSmoothMovement = movementWasEnabled;
        }
        
        // Re-enable grab/throw
        if (grabThrowScripts != null && grabThrowWasEnabled != null)
        {
            for (int i = 0; i < grabThrowScripts.Length; i++)
            {
                if (grabThrowScripts[i] != null && i < grabThrowWasEnabled.Length)
                {
                    grabThrowScripts[i].enabled = grabThrowWasEnabled[i];
                }
            }
        }
    }
}
