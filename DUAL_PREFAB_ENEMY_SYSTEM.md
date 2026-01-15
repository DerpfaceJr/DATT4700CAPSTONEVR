# Dual Prefab Enemy System - Static vs Rigged

## Overview
The enemy spawning system now uses **two different prefabs** based on enemy type:
- **Round 1 (Static Enemy)**: Simple T-pose cube (`Enemy.prefab`)
- **Round 2+ (Replay Enemies)**: Rigged character with full IK (`Player.prefab`)

## Implementation

### EnemySpawner Component Configuration
Two prefab slots in the Inspector:

```
[Header("Enemy Prefab Settings")]
enemyPrefab:        Assets/Enemy.prefab       (Basic static enemy)
riggedEnemyPrefab:  Assets/Player.prefab      (Rigged replay enemy)
```

### Prefab Selection Logic
The spawner automatically chooses the appropriate prefab:

```csharp
bool shouldBeStatic = isFirstRound && index == 0;
GameObject prefabToUse = null;

if (shouldBeStatic)
{
    // Static enemy (Round 1 first enemy) - use basic enemy prefab
    prefabToUse = enemyPrefab;
}
else
{
    // Replay enemy (Round 2+) - use rigged character prefab
    prefabToUse = riggedEnemyPrefab != null ? riggedEnemyPrefab : enemyPrefab;
}
```

## How It Works

### Round 1 - Static Enemy
**Prefab Used**: `Enemy.prefab` (simple cube)

1. Spawns basic cube enemy at spawn point
2. Adds `StaticEnemy` component (rotates in place)
3. No IK rig, no replay capability
4. Just a target for the player to defeat
5. Player movements are recorded during Round 1

**Appearance**: Red cube in T-pose (basic enemy)

### Round 2+ - Replay Enemies
**Prefab Used**: `Player.prefab` (rigged character)

1. Spawns rigged character with full skeleton
2. Detects `IKTargetFollowVRRig` component
3. Adds `EnemyIKReplay` component automatically
4. Replays recorded movements with full body IK:
   - Head tracking
   - Hand tracking
   - Body movement
   - Rotation

**Appearance**: Same model as player (rigged character)

## Enemy Spawning Behavior

| Round | Enemy Slot | Prefab | Behavior | IK Replay |
|-------|-----------|---------|----------|-----------|
| 1 | Slot 1 | Enemy.prefab | Static (rotates) | ❌ No |
| 2 | Slot 1 | Enemy.prefab | Static (rotates) | ❌ No |
| 2 | Slot 2 | Player.prefab | Replays Round 1 | ✅ Yes |
| 3 | Slot 1 | Enemy.prefab | Static (rotates) | ❌ No |
| 3 | Slot 2 | Player.prefab | Replays Round 1 | ✅ Yes |
| 3 | Slot 3 | Player.prefab | Replays Round 2 | ✅ Yes |
| N | Slot 1 | Enemy.prefab | Static (rotates) | ❌ No |
| N | Slot 2-N | Player.prefab | Replays Rounds 1-(N-1) | ✅ Yes |

## Benefits of Dual Prefab System

### 1. Clear Visual Distinction
- **Static Enemy**: Simple cube - easy to identify as the "first target"
- **Replay Enemies**: Rigged characters - clearly showing they're replaying movements

### 2. Performance Optimization
- Static enemy doesn't need expensive IK calculations
- Only replay enemies use the full rigged skeleton
- Saves performance in Round 1

### 3. Gameplay Clarity
- Players understand the first enemy is different (tutorial enemy)
- Replay enemies are visually distinct
- Clear progression from simple to complex enemies

### 4. Flexible Design
- Can easily swap out either prefab
- Static enemy can be any simple model
- Rigged enemy can be any character with IK rig

## Prefab Requirements

### Enemy.prefab (Static Enemy)
**Required:**
- GameObject with collider
- `EnemyController` component

**Optional:**
- Custom mesh/model
- Material for appearance

**Not Needed:**
- IK rig
- Skeleton
- Animation system

### Player.prefab (Rigged Enemy)
**Required:**
- `IKTargetFollowVRRig` component
- IK targets (Head Target, Left/Right Hand targets)
- `RigBuilder` component
- Full character skeleton
- `EnemyController` component

**Automatically Added by Spawner:**
- `EnemyIKReplay` component
- `CombatSystem` component
- `Rigidbody` (if not present)

## Code Changes

### Files Modified
- [`Assets/Scripts/EnemySpawner.cs`](Assets/Scripts/EnemySpawner.cs)
  - Added `riggedEnemyPrefab` field (line ~13)
  - Updated `CreateEnemy()` method to select appropriate prefab (lines ~91-120)

### Scene Configuration
- `EnemySpawner` GameObject in scene
  - `enemyPrefab`: `Assets/Enemy.prefab`
  - `riggedEnemyPrefab`: `Assets/Player.prefab`

## Testing Guide

### Step 1: Verify Configuration
In Unity Inspector, check `EnemySpawner` component:
- Enemy Prefab: Should show `Enemy` (GameObject)
- Rigged Enemy Prefab: Should show `Player` (GameObject)

### Step 2: Test Round 1
1. Enter Play Mode
2. Observe Round 1 enemy:
   - ✅ Should be a red cube
   - ✅ Should rotate in place (static)
   - ✅ Should NOT move around
   - ✅ Should NOT have IK animation

### Step 3: Test Round 2
1. Defeat Round 1 enemy (complete the round)
2. Observe Round 2 enemies:
   - ✅ Slot 1: Red cube (static, same as Round 1)
   - ✅ Slot 2: Rigged character (looks like player)
   - ✅ Slot 2 should replay your Round 1 movements
   - ✅ Slot 2 should show head and hand tracking

### Step 4: Test Round 3+
1. Continue playing through rounds
2. Each new replay enemy should:
   - ✅ Use Player.prefab (rigged character)
   - ✅ Replay movements from their respective round
   - ✅ Show full body IK animation

## Debug Information

### Console Logs to Watch For

**Round 1 Start:**
```
Round 1: Need 1 more enemy/enemies to reach target of 1 total
Spawned enemy for slot 1 with Static
```

**Round 2 Start:**
```
Round 2: Existing enemy slots: [1], Target slots: 1-2
Enemy 2 (Rigged): IK Targets - Head: True, LeftHand: True, RightHand: True
Enemy 2 (Rigged): Assigned XXX movement frames. Has IK Data: True
Spawned enemy for slot 2 with Round 1 data (XXX frames)
```

### Visual Confirmation
- **Round 1**: One red cube rotating
- **Round 2**: One red cube (static) + one rigged character (moving/replaying)
- **Round 3**: One red cube (static) + two rigged characters (each replaying different rounds)

## Customization Options

### Change Static Enemy Appearance
Replace `Enemy.prefab` with any prefab that has:
- Collider for combat
- `EnemyController` component
- Any visual appearance you want

### Change Replay Enemy Appearance
**Option 1**: Modify `Player.prefab` visually
- Keep IK rig and components
- Swap out the mesh/model
- Change materials/textures

**Option 2**: Create new rigged prefab
- Duplicate `Player.prefab`
- Replace visual mesh
- Keep all IK components intact
- Assign to `riggedEnemyPrefab` slot

## Troubleshooting

### If Round 1 Enemy is Rigged Character
**Problem**: Static enemy looks like player instead of cube
**Solution**: Verify `enemyPrefab` is set to `Enemy.prefab`, not `Player.prefab`

### If Round 2+ Enemies are T-Posing
**Problem**: Replay enemies don't animate
**Solution**: Verify `riggedEnemyPrefab` is set to `Player.prefab` with IK rig

### If All Enemies Look the Same
**Problem**: All enemies use same prefab
**Solution**: Check that both prefab slots are assigned in Inspector

## Summary

The dual prefab system provides:
- ✅ Clear visual distinction between enemy types
- ✅ Performance optimization (no IK for static enemy)
- ✅ Flexible design (easily swap prefabs)
- ✅ Better gameplay progression (simple → complex)

**Round 1**: Simple cube target for learning
**Round 2+**: Full body IK replay of your movements
