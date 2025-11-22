# Enemy System Setup Guide

## Overview

This enemy system implements a round-based combat system where:
- Each round spawns one more enemy (Round 1 = 1 enemy, Round 2 = 2 enemies, etc.)
- Enemies replay your exact movements from the previous round (ghost replay)
- First enemy (Round 1) is static and rotates to look around
- Player respawns at random locations each round
- Enemies are defeated by throwing objects at them
- Game resets to Round 1 if player dies

## Scripts Created

### Core Components

1. **MovementFrame.cs** - Data structure for movement recording
2. **PlayerMovementRecorder.cs** - Records player position/rotation
3. **EnemyController.cs** - Health system and damage handling
4. **EnemyAI.cs** - Replays player movements (ghost replay)
5. **StaticEnemy.cs** - First enemy behavior (static with rotation)
6. **EnemySpawner.cs** - Spawns enemy GameObjects
7. **CombatSystem.cs** - Detects thrown object collisions
8. **PlayerRespawner.cs** - Handles random player respawn
9. **RoundManager.cs** - Manages rounds and game state

## Setup Instructions

### Step 1: Add PlayerMovementRecorder

1. Find the **VRMovementController** GameObject in your scene
2. Add Component → Scripts → **Player Movement Recorder**
3. Configure:
   - Record Interval: 0.1 (records every 0.1 seconds)
   - Max Frames: 0 (unlimited)

### Step 2: Create RoundManager GameObject

1. Right-click in Hierarchy → Create Empty
2. Name it: **RoundManager**
3. Add Component → Scripts → **Round Manager**
4. Configure references:
   - **Movement Recorder**: Drag PlayerMovementRecorder component here
   - **Enemy Spawner**: Will be set in next step
   - **Player Respawner**: Will be set in next step
   - **Player Object**: Drag XR Origin or Camera Rig here
   - **Death Y Threshold**: -10 (player dies if falls below this)

### Step 3: Create EnemySpawner GameObject

1. Right-click in Hierarchy → Create Empty
2. Name it: **EnemySpawner**
3. Add Component → Scripts → **Enemy Spawner**
4. Configure:
   - **Enemy Material**: Leave empty (will create red material automatically)
   - **Enemy Scale**: (1, 1, 1)
   - **Use Random Spawns**: ✓ (checked)
   - **Min Spawn Distance**: 2

### Step 4: Create PlayerRespawner GameObject

1. Right-click in Hierarchy → Create Empty
2. Name it: **PlayerRespawner**
3. Add Component → Scripts → **Player Respawner**
4. Configure:
   - **Wall Padding**: 1 (distance from walls)
   - **Ground Level**: 0
   - **Player Height Offset**: 1.6
   - **Max Spawn Attempts**: 50

### Step 5: Link Components in RoundManager

1. Select **RoundManager** GameObject
2. In Inspector, drag:
   - **Enemy Spawner**: Drag EnemySpawner GameObject
   - **Player Respawner**: Drag PlayerRespawner GameObject
   - **Player Object**: Drag XR Origin or Camera Rig

### Step 6: Configure Combat System

The CombatSystem is automatically added to enemies when they spawn. You can adjust damage settings in the EnemySpawner:
- Damage Multiplier: 1.0
- Min Damage Velocity: 2.0
- Max Damage: 50.0

## How It Works

### Round Progression

1. **Round 1 Starts**:
   - Player respawns at random location
   - 1 enemy spawns (static, rotates to look around)
   - Player movement recording begins

2. **Defeat All Enemies**:
   - Round completes
   - Player movements are saved
   - Round 2 starts

3. **Round 2+**:
   - Player respawns at new random location
   - N enemies spawn (N = round number)
   - Enemies replay player's movements from previous round
   - Player movement recording begins for next round

### Combat

- **Throw Objects**: Use VRGrabThrow to grab and throw objects
- **Hit Enemies**: Thrown objects collide with enemies
- **Damage Calculation**: Velocity × Mass × Multiplier
- **Enemy Health**: 100 HP by default
- **Enemy Death**: Enemy is destroyed when health reaches 0

### Player Death

- Player falls below Y threshold (-10 by default)
- Game Over triggered
- Game resets to Round 1 after 2 seconds

## Testing

1. **Enter Play Mode**
2. **Round 1**: 
   - Move around (movements are being recorded)
   - Throw objects at the static enemy
   - Defeat the enemy to advance

3. **Round 2**:
   - Player respawns at random location
   - 2 enemies spawn
   - Enemies replay your movements from Round 1
   - Defeat both enemies

4. **Continue**: Each round adds one more enemy

## Troubleshooting

### Enemies Not Spawning
- Check RoundManager has EnemySpawner reference
- Check console for errors
- Verify PlayerRespawner is set up

### Movements Not Recording
- Check PlayerMovementRecorder is on VRMovementController
- Verify it's linked in RoundManager
- Check record interval is > 0

### Enemies Not Replaying Movements
- Verify previous round completed successfully
- Check EnemyAI has movement data assigned
- Verify movement data was recorded (check console logs)

### Combat Not Working
- Ensure enemies have CombatSystem component (auto-added)
- Check thrown objects have Rigidbody
- Verify collision detection is enabled
- Check minimum damage velocity threshold

### Player Not Respawning
- Check PlayerRespawner is linked in RoundManager
- Verify XR Origin is assigned
- Check spawn bounds are calculated correctly
- Look for console warnings about spawn positions

## Customization

### Enemy Health
- Edit EnemyController.maxHealth in EnemySpawner or on individual enemies

### Damage Settings
- Edit CombatSystem settings on enemies
- Adjust damageMultiplier, minDamageVelocity, maxDamage

### Spawn Points
- Create Transform GameObjects as spawn points
- Assign to EnemySpawner.spawnPoints array
- Set useRandomSpawns to false

### Static Enemy Behavior
- Edit StaticEnemy.rotationSpeed
- Change RotationPattern (Continuous or Random)
- Adjust lookDuration for Random pattern

## Events

RoundManager provides events you can subscribe to:
- `OnRoundStart(int roundNumber)`
- `OnRoundComplete(int roundNumber)`
- `OnGameOver()`
- `OnGameReset()`

Example usage:
```csharp
RoundManager roundManager = FindObjectOfType<RoundManager>();
roundManager.OnRoundStart += (round) => Debug.Log($"Round {round} started!");
```

