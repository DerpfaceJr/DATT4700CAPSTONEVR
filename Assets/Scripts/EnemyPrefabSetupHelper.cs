using UnityEngine;

/// <summary>
/// Helper script to verify and complete enemy prefab setup for rigged characters
/// Attach this to the enemy prefab root GameObject
/// It will verify IK setup and log helpful messages
/// </summary>
public class EnemyPrefabSetupHelper : MonoBehaviour
{
    [Header("Setup Verification")]
    [Tooltip("Check this to verify setup on Start")]
    public bool verifyOnStart = true;
    
    void Start()
    {
        if (verifyOnStart)
        {
            VerifySetup();
        }
    }
    
    /// <summary>
    /// Verify that the enemy prefab is properly set up for rigged animations
    /// </summary>
    [ContextMenu("Verify Setup")]
    public void VerifySetup()
    {
        bool allGood = true;
        string issues = "";
        
        // Check for IKTargetFollowVRRig
        IKTargetFollowVRRig ikRig = GetComponent<IKTargetFollowVRRig>();
        if (ikRig == null)
        {
            ikRig = GetComponentInChildren<IKTargetFollowVRRig>();
        }
        
        if (ikRig == null)
        {
            allGood = false;
            issues += "❌ IKTargetFollowVRRig component not found. Add it to enable rigged animations.\n";
        }
        else
        {
            Debug.Log("✓ IKTargetFollowVRRig component found");
            
            // Check IK targets
            if (ikRig.head.ikTarget == null)
            {
                allGood = false;
                issues += "❌ Head IK Target not assigned in IKTargetFollowVRRig.\n";
            }
            else
            {
                Debug.Log($"✓ Head IK Target: {ikRig.head.ikTarget.name}");
            }
            
            if (ikRig.leftHand.ikTarget == null)
            {
                allGood = false;
                issues += "❌ Left Hand IK Target not assigned in IKTargetFollowVRRig.\n";
            }
            else
            {
                Debug.Log($"✓ Left Hand IK Target: {ikRig.leftHand.ikTarget.name}");
            }
            
            if (ikRig.rightHand.ikTarget == null)
            {
                allGood = false;
                issues += "❌ Right Hand IK Target not assigned in IKTargetFollowVRRig.\n";
            }
            else
            {
                Debug.Log($"✓ Right Hand IK Target: {ikRig.rightHand.ikTarget.name}");
            }
        }
        
        // Check for required components
        EnemyController enemyController = GetComponent<EnemyController>();
        if (enemyController == null)
        {
            allGood = false;
            issues += "❌ EnemyController component not found.\n";
        }
        else
        {
            Debug.Log($"✓ EnemyController found (Max Health: {enemyController.maxHealth})");
        }
        
        CombatSystem combatSystem = GetComponent<CombatSystem>();
        if (combatSystem == null)
        {
            allGood = false;
            issues += "❌ CombatSystem component not found.\n";
        }
        else
        {
            Debug.Log("✓ CombatSystem found");
        }
        
        // Check for rigged character model
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            issues += "⚠️ No Animator found in children. Make sure Ch44_nonPBR model is a child.\n";
        }
        else
        {
            Debug.Log($"✓ Animator found: {animator.name}");
        }
        
        // Summary
        if (allGood)
        {
            Debug.Log($"<color=green>✓ Enemy Prefab Setup Complete!</color> All required components are configured.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>Enemy Prefab Setup Issues:</color>\n{issues}");
            Debug.LogWarning("See RIGGED_ENEMY_SETUP_GUIDE.md for setup instructions.");
        }
    }
    
    /// <summary>
    /// Try to auto-find and assign IK targets (helper method)
    /// </summary>
    [ContextMenu("Auto-Find IK Targets")]
    public void AutoFindIKTargets()
    {
        IKTargetFollowVRRig ikRig = GetComponent<IKTargetFollowVRRig>();
        if (ikRig == null)
        {
            ikRig = GetComponentInChildren<IKTargetFollowVRRig>();
        }
        
        if (ikRig == null)
        {
            Debug.LogError("IKTargetFollowVRRig component not found! Add it first.");
            return;
        }
        
        // Search for IK targets in children
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        
        Transform headIK = null;
        Transform leftArmIK = null;
        Transform rightArmIK = null;
        
        foreach (Transform child in allChildren)
        {
            if (child.name.Contains("Head IK_target") || child.name.Contains("Head IK Target"))
            {
                headIK = child;
            }
            else if (child.name.Contains("Left Arm IK_target") || child.name.Contains("Left Arm IK Target"))
            {
                leftArmIK = child;
            }
            else if (child.name.Contains("Right Arm IK_target") || child.name.Contains("Right Arm IK Target"))
            {
                rightArmIK = child;
            }
        }
        
        if (headIK != null)
        {
            ikRig.head.ikTarget = headIK;
            Debug.Log($"✓ Auto-assigned Head IK Target: {headIK.name}");
        }
        else
        {
            Debug.LogWarning("Could not auto-find Head IK Target. Please assign manually.");
        }
        
        if (leftArmIK != null)
        {
            ikRig.leftHand.ikTarget = leftArmIK;
            Debug.Log($"✓ Auto-assigned Left Hand IK Target: {leftArmIK.name}");
        }
        else
        {
            Debug.LogWarning("Could not auto-find Left Hand IK Target. Please assign manually.");
        }
        
        if (rightArmIK != null)
        {
            ikRig.rightHand.ikTarget = rightArmIK;
            Debug.Log($"✓ Auto-assigned Right Hand IK Target: {rightArmIK.name}");
        }
        else
        {
            Debug.LogWarning("Could not auto-find Right Hand IK Target. Please assign manually.");
        }
        
        Debug.Log("Auto-find complete. Verify setup to check results.");
    }
}


