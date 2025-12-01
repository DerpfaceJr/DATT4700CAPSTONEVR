using UnityEngine;

/// <summary>
/// Setup script to automatically configure the enemy system
/// Attach this to a GameObject and run it once, then remove it
/// </summary>
public class EnemySystemSetup : MonoBehaviour
{
    [ContextMenu("Setup Enemy System")]
    void SetupEnemySystem()
    {
        // Find or create VRMovementController
        GameObject vrMovement = GameObject.Find("VRMovementController");
        if (vrMovement == null)
        {
            vrMovement = new GameObject("VRMovementController");
        }
        
        // Add CharacterController if missing
        if (vrMovement.GetComponent<CharacterController>() == null)
        {
            CharacterController cc = vrMovement.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 1, 0);
            cc.radius = 0.3f;
            cc.height = 2f;
        }
        
        // Add VRMovementController script if missing
        if (vrMovement.GetComponent<VRMovementController>() == null)
        {
            vrMovement.AddComponent<VRMovementController>();
        }
        
        // Add PlayerMovementRecorder if missing
        if (vrMovement.GetComponent<PlayerMovementRecorder>() == null)
        {
            PlayerMovementRecorder recorder = vrMovement.AddComponent<PlayerMovementRecorder>();
            recorder.recordInterval = 0.1f;
            recorder.maxFrames = 0;
        }
        
        // Find or create RoundManager
        GameObject roundManager = GameObject.Find("RoundManager");
        if (roundManager == null)
        {
            roundManager = new GameObject("RoundManager");
        }
        
        // Add RoundManager script if missing
        if (roundManager.GetComponent<RoundManager>() == null)
        {
            RoundManager rm = roundManager.AddComponent<RoundManager>();
            rm.movementRecorder = vrMovement.GetComponent<PlayerMovementRecorder>();
            rm.debugMode = true;
            rm.deathYThreshold = -10f;
        }
        
        // Find or create EnemySpawner
        GameObject enemySpawner = GameObject.Find("EnemySpawner");
        if (enemySpawner == null)
        {
            enemySpawner = new GameObject("EnemySpawner");
        }
        
        // Add EnemySpawner script if missing
        if (enemySpawner.GetComponent<EnemySpawner>() == null)
        {
            EnemySpawner es = enemySpawner.AddComponent<EnemySpawner>();
            es.enemyScale = Vector3.one;
            es.useRandomSpawns = true;
            es.minSpawnDistance = 2f;
        }
        
        // Find or create PlayerRespawner
        GameObject playerRespawner = GameObject.Find("PlayerRespawner");
        if (playerRespawner == null)
        {
            playerRespawner = new GameObject("PlayerRespawner");
        }
        
        // Add PlayerRespawner script if missing
        if (playerRespawner.GetComponent<PlayerRespawner>() == null)
        {
            PlayerRespawner pr = playerRespawner.AddComponent<PlayerRespawner>();
            pr.wallPadding = 1f;
            pr.groundLevel = 0f;
            pr.playerHeightOffset = 1.6f;
            pr.maxSpawnAttempts = 50;
        }
        
        // Link everything in RoundManager
        RoundManager rmComponent = roundManager.GetComponent<RoundManager>();
        if (rmComponent != null)
        {
            rmComponent.movementRecorder = vrMovement.GetComponent<PlayerMovementRecorder>();
            rmComponent.enemySpawner = enemySpawner.GetComponent<EnemySpawner>();
            rmComponent.playerRespawner = playerRespawner.GetComponent<PlayerRespawner>();
            
            // Find player object
            GameObject playerObj = GameObject.Find("XR Origin");
            if (playerObj == null)
            {
                playerObj = GameObject.Find("[BuildingBlock] Camera Rig");
            }
            if (playerObj != null)
            {
                rmComponent.playerObject = playerObj;
            }
        }
        
        // Configure VRMovementController to reference Camera Rig
        VRMovementController vrmc = vrMovement.GetComponent<VRMovementController>();
        if (vrmc != null)
        {
            GameObject cameraRig = GameObject.Find("[BuildingBlock] Camera Rig");
            if (cameraRig != null && vrmc.xrOrigin == null)
            {
                vrmc.xrOrigin = cameraRig.transform;
            }
        }
        
        Debug.Log("Enemy System Setup Complete!");
        Debug.Log("All components have been added and linked.");
        Debug.Log("You can now remove the EnemySystemSetup component.");
    }
}



