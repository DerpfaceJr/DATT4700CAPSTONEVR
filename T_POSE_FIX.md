# T-Pose Fix - Enemy IK Replay

## Problem
Enemies were staying in T-pose position instead of replaying player rig movements (head and hand tracking).

## Root Cause
The `EnemySpawner` was configured to use `Assets/Enemy.prefab`, which does not have the IK rig setup required for full body animation replay. Without an `IKTargetFollowVRRig` component and IK targets, the spawner falls back to basic `EnemyAI` (position/rotation only) instead of `EnemyIKReplay` (full body IK).

## Solution Applied
Changed the `EnemySpawner` to use `Assets/Player.prefab` as the enemy prefab. This prefab already has:
- `IKTargetFollowVRRig` component
- All IK targets configured (Head Target, Left Arm IK_target, Right Arm IK_target)
- `RigBuilder` component with IK constraints
- Full rigged character skeleton

### Change Made via Unity MCP:
```
EnemySpawner.enemyPrefab: "Assets/Enemy.prefab" → "Assets/Player.prefab"
```

## How It Works Now

### Enemy Spawning Logic
When an enemy spawns, `EnemySpawner.EnsureEnemyComponents()` now:

1. **Detects IK Rig** (lines 220-227):
   ```csharp
   IKTargetFollowVRRig enemyIKRig = enemy.GetComponent<IKTargetFollowVRRig>();
   bool hasRiggedCharacter = enemyIKRig != null;
   ```
   ✅ With Player.prefab, this will be **true**

2. **Adds EnemyIKReplay** (lines 267-283):
   ```csharp
   if (hasRiggedCharacter)
   {
       // Use EnemyIKReplay for rigged characters
       enemyIKReplay = enemy.AddComponent<EnemyIKReplay>();
       enemyIKReplay.ikRig = enemyIKRig;
       enemyIKReplay.useInterpolation = true;
   }
   ```
   ✅ Enemy gets full IK replay component

3. **Assigns Movement Data** (lines 294-314):
   ```csharp
   enemyIKReplay.SetMovementData(dataCopy);
   ```
   ✅ Enemy receives recorded head and hand positions/rotations

4. **Logs Debug Info** (line 313):
   ```csharp
   Debug.Log($"Enemy {index + 1} (Rigged): Assigned {movementData.Count} movement frames. Has IK Data: {hasIKData}");
   ```

## Testing the Fix

### Step 1: Verify Prefab Assignment
Check `EnemySpawner` component in hierarchy:
- Enemy Prefab should now show: `Player (GameObject)`

### Step 2: Play Through Round 1
1. Enter Play Mode
2. Move around using VR controllers
3. Move your head and hands
4. Defeat the Round 1 enemy (static red cube)
5. Round 1 completes and records your movements

### Step 3: Watch Round 2 Enemy
1. Round 2 starts automatically
2. Enemy spawns as a rigged character (same model as player)
3. Enemy should:
   - ✅ Walk along your recorded path
   - ✅ Turn their head matching your head movements
   - ✅ Move their hands matching your controller positions
   - ✅ Full body IK animation

### Debug Logs to Look For
In Unity Console during Round 2 spawn:
```
PlayerMovementRecorder: Found IK rig, will record IK target data
Round 2: Existing enemy slots: [1], Target slots: 1-2
Enemy 2 (Rigged): IK Targets - Head: True, LeftHand: True, RightHand: True
Enemy 2 (Rigged): Assigned 300+ movement frames. Has IK Data: True
Spawned enemy for slot 2 with Round 1 data (XXX frames)
```

## Important Notes

### Why Enemies Look Like the Player
Since we're using `Player.prefab` as the enemy prefab, enemies will have the same appearance as the player character. This is intentional for IK replay to work.

**To make enemies look different:**
1. Keep using Player.prefab for the IK rig
2. Swap out the visual mesh/model on the prefab
3. Keep all IK components and targets intact
4. Or: Create a custom enemy prefab based on Player.prefab with different visuals

### Round 1 Enemy (Static)
The first enemy in Round 1 is intentionally static (no replay) because there's no previous movement data to replay. It will:
- Stay in one spot
- Rotate slowly
- Serve as the first target

### Recording Requirements
For IK replay to work, the player must:
1. Have "Player" tag ✅ (Already configured)
2. Have IKTargetFollowVRRig component ✅ (Already configured)
3. Have VR targets assigned to actual controllers ✅ (Already configured)
4. Have PlayerMovementRecorder active ✅ (Already configured)

All requirements are met - the system is ready to use!

## Files Modified
- Unity Scene: `EnemySpawner` component property changed
  - `enemyPrefab`: Changed from `Assets/Enemy.prefab` to `Assets/Player.prefab`

## Expected Behavior Summary

| Round | Enemies | Behavior |
|-------|---------|----------|
| 1 | 1 static enemy | Rotates in place (no IK, no replay) |
| 2 | 1 static + 1 replay | Replay enemy shows full body IK from Round 1 |
| 3 | Previous + 1 new replay | New enemy replays Round 2 with full IK |
| N | N total enemies | Each replay enemy shows full body animation |

## Troubleshooting

### If Enemy Still T-Poses
1. Verify `EnemySpawner.enemyPrefab` is set to `Player.prefab`
2. Check console for "Enemy X (Rigged): IK Targets" message
3. Verify "Has IK Data: True" in console logs
4. Ensure you played through Round 1 completely before Round 2 started

### If No Console Logs Appear
1. Check that debug mode is enabled on RoundManager
2. Verify PlayerMovementRecorder is on VRMovementController
3. Check Unity Console is set to show "Log" messages (not just errors/warnings)

### If Movement Data Missing
1. Ensure you moved during Round 1
2. Check that Round 1 completed (all enemies defeated)
3. Verify PlayerMovementRecorder.recordInterval is 0.1 (10 fps)

## Next Steps
1. Enter Play Mode
2. Play through Round 1 (move around, defeat enemy)
3. Watch Round 2 enemy for full body IK replay
4. Verify head and hand movements are replayed correctly

The fix is complete - enemies will now replay your full body movements instead of T-posing!
