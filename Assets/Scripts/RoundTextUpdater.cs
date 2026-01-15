using UnityEngine;
using TMPro;

/// <summary>
/// Updates the round text UI to display the current round number
/// Subscribes to RoundManager events to update when rounds change
/// </summary>
public class RoundTextUpdater : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("TextMeshProUGUI component to display round number (auto-finds if not set)")]
    public TextMeshProUGUI roundText;
    
    [Tooltip("Text format (use {0} for round number)")]
    public string textFormat = "Round {0}";
    
    [Header("Settings")]
    [Tooltip("RoundManager reference (auto-finds if not set)")]
    public RoundManager roundManager;
    
    private void Start()
    {
        // Find RoundManager if not assigned
        if (roundManager == null)
        {
            roundManager = FindObjectOfType<RoundManager>();
        }
        
        // Find round text if not assigned
        if (roundText == null)
        {
            // Try to find by name
            GameObject roundTextObj = GameObject.Find("Round Text");
            if (roundTextObj != null)
            {
                roundText = roundTextObj.GetComponent<TextMeshProUGUI>();
            }
            
            // If still not found, try to find any TextMeshProUGUI on this GameObject
            if (roundText == null)
            {
                roundText = GetComponent<TextMeshProUGUI>();
            }
        }
        
        // Subscribe to RoundManager events
        if (roundManager != null)
        {
            roundManager.OnRoundStart += UpdateRoundText;
            roundManager.OnGameReset += OnGameReset;
            
            // Update text immediately with current round
            UpdateRoundText(roundManager.GetCurrentRound());
        }
        else
        {
            Debug.LogWarning("[RoundTextUpdater] RoundManager not found! Round text will not update.");
        }
        
        if (roundText == null)
        {
            Debug.LogWarning("[RoundTextUpdater] Round text UI not found! Please assign a TextMeshProUGUI component.");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (roundManager != null)
        {
            roundManager.OnRoundStart -= UpdateRoundText;
            roundManager.OnGameReset -= OnGameReset;
        }
    }
    
    /// <summary>
    /// Handle game reset event (no parameters)
    /// </summary>
    private void OnGameReset()
    {
        if (roundManager != null)
        {
            UpdateRoundText(roundManager.GetCurrentRound());
        }
    }
    
    /// <summary>
    /// Update the round text display
    /// </summary>
    private void UpdateRoundText(int roundNumber)
    {
        if (roundText != null)
        {
            roundText.text = string.Format(textFormat, roundNumber);
            Debug.Log($"[RoundTextUpdater] Updated round text to: {roundText.text}");
        }
    }
}
