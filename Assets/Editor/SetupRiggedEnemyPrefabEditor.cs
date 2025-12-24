using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script to help set up the enemy prefab with rigged character model
/// </summary>
public class SetupRiggedEnemyPrefabEditor : EditorWindow
{
    private GameObject enemyPrefab;
    private GameObject riggedCharacterPrefab;
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Setup Rigged Enemy Prefab")]
    public static void ShowWindow()
    {
        GetWindow<SetupRiggedEnemyPrefabEditor>("Setup Rigged Enemy");
    }
    
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.LabelField("Rigged Enemy Prefab Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox("This tool helps set up the enemy prefab to use the rigged character model with IK animations.", MessageType.Info);
        EditorGUILayout.Space();
        
        enemyPrefab = (GameObject)EditorGUILayout.ObjectField("Enemy Prefab", enemyPrefab, typeof(GameObject), false);
        riggedCharacterPrefab = (GameObject)EditorGUILayout.ObjectField("Rigged Character Model (Ch44_nonPBR)", riggedCharacterPrefab, typeof(GameObject), false);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Setup Enemy Prefab", GUILayout.Height(30)))
        {
            if (enemyPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign the Enemy Prefab!", "OK");
                return;
            }
            
            SetupEnemyPrefab();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Manual Setup Instructions:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. Open Enemy.prefab in Prefab mode\n" +
            "2. Delete the Cube child object\n" +
            "3. Drag Ch44_nonPBR model as a child\n" +
            "4. Add IKTargetFollowVRRig component to root\n" +
            "5. Find and assign IK targets:\n" +
            "   - Head IK Target (from Head IK hierarchy)\n" +
            "   - Left Arm IK_target (from Left Arm IK hierarchy)\n" +
            "   - Right Arm IK_target (from Right Arm IK hierarchy)\n" +
            "6. Ensure EnemyController and CombatSystem exist\n" +
            "7. Add colliders to character bones for hitboxes",
            MessageType.Info
        );
        
        EditorGUILayout.EndScrollView();
    }
    
    void SetupEnemyPrefab()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy prefab not assigned!");
            return;
        }
        
        // Open prefab in prefab mode
        string prefabPath = AssetDatabase.GetAssetPath(enemyPrefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            EditorUtility.DisplayDialog("Error", "Could not find prefab path. Make sure the prefab is in the Assets folder.", "OK");
            return;
        }
        
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
        
        // Check if it already has IKTargetFollowVRRig
        IKTargetFollowVRRig existingIKRig = prefabInstance.GetComponent<IKTargetFollowVRRig>();
        if (existingIKRig != null)
        {
            EditorUtility.DisplayDialog("Info", "Enemy prefab already has IKTargetFollowVRRig component. Please configure IK targets manually in Prefab mode.", "OK");
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            return;
        }
        
        // Add IKTargetFollowVRRig component
        IKTargetFollowVRRig ikRig = prefabInstance.AddComponent<IKTargetFollowVRRig>();
        ikRig.turnSmoothness = 0.1f;
        ikRig.headBodyPositionOffset = new Vector3(0, -0.54f, 0);
        
        // Try to find IK targets in children
        Transform headIKTarget = FindChildByName(prefabInstance.transform, "Head IK_target");
        Transform leftArmIKTarget = FindChildByName(prefabInstance.transform, "Left Arm IK_target");
        Transform rightArmIKTarget = FindChildByName(prefabInstance.transform, "Right Arm IK_target");
        
        if (headIKTarget != null)
        {
            ikRig.head.ikTarget = headIKTarget;
            Debug.Log("Found Head IK Target");
        }
        else
        {
            Debug.LogWarning("Head IK Target not found. Please assign manually.");
        }
        
        if (leftArmIKTarget != null)
        {
            ikRig.leftHand.ikTarget = leftArmIKTarget;
            Debug.Log("Found Left Arm IK Target");
        }
        else
        {
            Debug.LogWarning("Left Arm IK Target not found. Please assign manually.");
        }
        
        if (rightArmIKTarget != null)
        {
            ikRig.rightHand.ikTarget = rightArmIKTarget;
            Debug.Log("Found Right Arm IK Target");
        }
        else
        {
            Debug.LogWarning("Right Arm IK Target not found. Please assign manually.");
        }
        
        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabInstance);
        
        EditorUtility.DisplayDialog("Success", "IKTargetFollowVRRig component added to enemy prefab. Please verify IK target assignments in Prefab mode.", "OK");
        
        // Select the prefab
        Selection.activeObject = enemyPrefab;
        EditorGUIUtility.PingObject(enemyPrefab);
    }
    
    Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            
            Transform found = FindChildByName(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
}

