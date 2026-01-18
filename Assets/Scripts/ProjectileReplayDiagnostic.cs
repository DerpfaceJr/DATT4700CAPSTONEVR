using UnityEngine;

/// <summary>
/// Diagnostic script to help troubleshoot enemy projectile replay system
/// Attach to any GameObject and run in Play Mode
/// </summary>
public class ProjectileReplayDiagnostic : MonoBehaviour
{
    [Header("Diagnostic Settings")]
    [Tooltip("Run diagnostics automatically on start")]
    public bool runOnStart = true;
    
    [Tooltip("Run diagnostics every frame (for live monitoring)")]
    public bool runContinuously = false;
    
    [Header("Results")]
    [TextArea(10, 20)]
    public string diagnosticResults = "Not run yet";
    
    void Start()
    {
        if (runOnStart)
        {
            RunDiagnostics();
        }
    }
    
    void Update()
    {
        if (runContinuously && Time.frameCount % 60 == 0) // Once per second
        {
            RunDiagnostics();
        }
    }
    
    [ContextMenu("Run Diagnostics")]
    public void RunDiagnostics()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== PROJECTILE REPLAY DIAGNOSTIC ===\n");
        
        // 1. Check ProjectileThrowTracker
        ProjectileThrowTracker tracker = FindObjectOfType<ProjectileThrowTracker>();
        sb.AppendLine("1. ProjectileThrowTracker:");
        if (tracker != null)
        {
            sb.AppendLine($"   ✓ FOUND on: {tracker.gameObject.name}");
            sb.AppendLine($"   - Min Throw Velocity: {tracker.minThrowVelocity}");
            sb.AppendLine($"   - Tracking Duration: {tracker.trackingDuration}");
        }
        else
        {
            sb.AppendLine("   ✗ NOT FOUND! Add to scene.");
        }
        sb.AppendLine();
        
        // 2. Check PlayerMovementRecorder
        PlayerMovementRecorder recorder = FindObjectOfType<PlayerMovementRecorder>();
        sb.AppendLine("2. PlayerMovementRecorder:");
        if (recorder != null)
        {
            sb.AppendLine($"   ✓ FOUND on: {recorder.gameObject.name}");
            var movementData = recorder.GetMovementData();
            if (movementData != null)
            {
                sb.AppendLine($"   - Recorded Frames: {movementData.Count}");
                int throwCount = 0;
                foreach (var frame in movementData)
                {
                    if (frame.hasThrowEvent) throwCount++;
                }
                sb.AppendLine($"   - Throw Events: {throwCount}");
            }
        }
        else
        {
            sb.AppendLine("   ✗ NOT FOUND!");
        }
        sb.AppendLine();
        
        // 3. Check EnemySpawner
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        sb.AppendLine("3. EnemySpawner:");
        if (spawner != null)
        {
            sb.AppendLine($"   ✓ FOUND on: {spawner.gameObject.name}");
            sb.AppendLine($"   - Enemy Prefab: {(spawner.enemyPrefab != null ? spawner.enemyPrefab.name : "NULL")}");
            sb.AppendLine($"   - Rigged Enemy Prefab: {(spawner.riggedEnemyPrefab != null ? spawner.riggedEnemyPrefab.name : "NULL")}");
            sb.AppendLine($"   - Enemy Ball Prefab: {(spawner.enemyBallPrefab != null ? spawner.enemyBallPrefab.name : "NULL ✗")}");
        }
        else
        {
            sb.AppendLine("   ✗ NOT FOUND!");
        }
        sb.AppendLine();
        
        // 4. Check Rigged Enemies
        EnemyIKReplay[] riggedEnemies = FindObjectsOfType<EnemyIKReplay>();
        sb.AppendLine($"4. Rigged Enemies: {riggedEnemies.Length}");
        foreach (var enemy in riggedEnemies)
        {
            sb.AppendLine($"   - {enemy.gameObject.name}:");
            sb.AppendLine($"     - Enemy Ball Prefab: {(enemy.enemyBallPrefab != null ? "✓" : "✗ NULL")}");
            sb.AppendLine($"     - IK Rig: {(enemy.ikRig != null ? "✓" : "✗ NULL")}");
            
            // Check CombatSystem
            CombatSystem combat = enemy.GetComponent<CombatSystem>();
            sb.AppendLine($"     - CombatSystem: {(combat != null ? "✓" : "✗ MISSING")}");
            if (combat != null)
            {
                sb.AppendLine($"       - Melee Weapons Enabled: {combat.meleeWeaponsEnabled}");
                sb.AppendLine($"       - Sword Min Velocity: {combat.swordMinVelocity}");
            }
        }
        sb.AppendLine();
        
        // 5. Check Enemy Ball Prefab
        if (spawner != null && spawner.enemyBallPrefab != null)
        {
            sb.AppendLine("5. Enemy Ball Prefab Components:");
            Component[] components = spawner.enemyBallPrefab.GetComponents<Component>();
            foreach (var comp in components)
            {
                sb.AppendLine($"   - {comp.GetType().Name}");
            }
            
            // Check for required components
            bool hasRigidbody = spawner.enemyBallPrefab.GetComponent<Rigidbody>() != null;
            bool hasCollider = spawner.enemyBallPrefab.GetComponent<Collider>() != null;
            bool hasBuildingBlockProjectile = spawner.enemyBallPrefab.GetComponent<BuildingBlockProjectile>() != null;
            
            sb.AppendLine();
            sb.AppendLine("   Required Components:");
            sb.AppendLine($"   - Rigidbody: {(hasRigidbody ? "✓" : "✗ MISSING")}");
            sb.AppendLine($"   - Collider: {(hasCollider ? "✓" : "✗ MISSING")}");
            sb.AppendLine($"   - BuildingBlockProjectile: {(hasBuildingBlockProjectile ? "✓" : "✗ MISSING (will add at runtime)")}");
        }
        else
        {
            sb.AppendLine("5. Enemy Ball Prefab: NOT ASSIGNED ✗");
        }
        sb.AppendLine();
        
        // 6. Check Sword
        GameObject[] swords = GameObject.FindGameObjectsWithTag("Sword");
        sb.AppendLine($"6. Swords with 'Sword' tag: {swords.Length}");
        foreach (var sword in swords)
        {
            sb.AppendLine($"   - {sword.name}");
            Rigidbody rb = sword.GetComponent<Rigidbody>();
            sb.AppendLine($"     - Rigidbody: {(rb != null ? "✓" : "✗ MISSING")}");
            Collider col = sword.GetComponent<Collider>();
            sb.AppendLine($"     - Collider: {(col != null ? "✓" : "✗ MISSING")}");
        }
        sb.AppendLine();
        
        // 7. Check VRGrabThrow integration
        VRGrabThrow[] grabScripts = FindObjectsOfType<VRGrabThrow>();
        sb.AppendLine($"7. VRGrabThrow scripts: {grabScripts.Length}");
        if (grabScripts.Length > 0)
        {
            sb.AppendLine("   ✓ VR Grab system found");
            sb.AppendLine("   Note: Check if TrackObject() is being called on Release()");
        }
        else
        {
            sb.AppendLine("   ✗ No VR Grab scripts found!");
        }
        
        sb.AppendLine();
        sb.AppendLine("=== END DIAGNOSTIC ===");
        
        diagnosticResults = sb.ToString();
        Debug.Log(diagnosticResults);
    }
}
