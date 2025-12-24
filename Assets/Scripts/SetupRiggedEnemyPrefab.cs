using UnityEngine;

/// <summary>
/// Helper script to set up the enemy prefab with rigged character model
/// Run this once to configure the enemy prefab properly
/// </summary>
public class SetupRiggedEnemyPrefab : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The enemy prefab to update")]
    public GameObject enemyPrefab;
    
    [Tooltip("The rigged character model prefab (Ch44_nonPBR)")]
    public GameObject riggedCharacterModel;
    
    [Tooltip("Player character in scene (to copy IK setup from)")]
    public GameObject playerCharacter;
    
    void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("SetupRiggedEnemyPrefab: Enemy prefab not assigned!");
            return;
        }
        
        Debug.Log("SetupRiggedEnemyPrefab: Use this script in Editor mode to set up enemy prefab.");
        Debug.Log("Instructions:");
        Debug.Log("1. Assign enemy prefab reference");
        Debug.Log("2. Assign rigged character model (Ch44_nonPBR)");
        Debug.Log("3. Run SetupEnemyPrefab() method");
    }
    
    /// <summary>
    /// Set up the enemy prefab with rigged character (call from Editor)
    /// </summary>
    [ContextMenu("Setup Enemy Prefab")]
    public void SetupEnemyPrefab()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("SetupRiggedEnemyPrefab: Enemy prefab not assigned!");
            return;
        }
        
        // This should be run in Editor mode
        #if UNITY_EDITOR
        Debug.Log("SetupRiggedEnemyPrefab: Setting up enemy prefab...");
        
        // Note: Prefab modification should be done manually in Unity Editor
        // This script provides guidance
        Debug.Log("Manual steps required:");
        Debug.Log("1. Open Enemy.prefab in Prefab mode");
        Debug.Log("2. Replace Cube with Ch44_nonPBR model");
        Debug.Log("3. Add IKTargetFollowVRRig component");
        Debug.Log("4. Configure IK targets (Head, Left Hand, Right Hand)");
        Debug.Log("5. Ensure EnemyController and CombatSystem components exist");
        #else
        Debug.LogWarning("SetupRiggedEnemyPrefab: This should be run in Editor mode");
        #endif
    }
}

