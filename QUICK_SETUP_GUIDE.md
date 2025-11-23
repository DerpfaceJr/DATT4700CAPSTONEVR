# Quick Setup Guide - Enemy System

## Automated Setup (Recommended)

I've created an automated setup script! Here's how to use it:

### Step 1: Add EnemySystemSetup Script
1. Select the **EnemySystemSetup** GameObject in the Hierarchy (already created)
2. In Inspector, click **Add Component**
3. Search for **Enemy System Setup**
4. Add the component

### Step 2: Run Setup
1. With EnemySystemSetup GameObject selected
2. In Inspector, find the **Enemy System Setup** component
3. Click the **three dots (⋮)** menu in the component header
4. Select **Setup Enemy System** from the context menu
5. Check Console for "Enemy System Setup Complete!" message

### Step 3: Remove Setup Script (Optional)
- After setup is complete, you can remove the EnemySystemSetup component
- The EnemySystemSetup GameObject can also be deleted

## Manual Setup (Alternative)

If the automated setup doesn't work, follow these steps:

### 1. VRMovementController Setup
1. Select **VRMovementController** GameObject
2. Add Component → **Character Controller**
   - Center: (0, 1, 0)
   - Radius: 0.3
   - Height: 2
3. Add Component → **VR Movement Controller**
4. Add Component → **Player Movement Recorder**
   - Record Interval: 0.1
   - Max Frames: 0

### 2. RoundManager Setup
1. Select **RoundManager** GameObject
2. Add Component → **Round Manager**
3. Configure:
   - Movement Recorder: Drag VRMovementController's PlayerMovementRecorder
   - Enemy Spawner: Drag EnemySpawner GameObject
   - Player Respawner: Drag PlayerRespawner GameObject
   - Player Object: Drag "[BuildingBlock] Camera Rig"
   - Death Y Threshold: -10
   - Debug Mode: ✓

### 3. EnemySpawner Setup
1. Select **EnemySpawner** GameObject
2. Add Component → **Enemy Spawner**
3. Configure:
   - Enemy Scale: (1, 1, 1)
   - Use Random Spawns: ✓
   - Min Spawn Distance: 2

### 4. PlayerRespawner Setup
1. Select **PlayerRespawner** GameObject
2. Add Component → **Player Respawner**
3. Configure:
   - Wall Padding: 1
   - Ground Level: 0
   - Player Height Offset: 1.6
   - Max Spawn Attempts: 50

### 5. Link VRMovementController to Camera Rig
1. Select **VRMovementController** GameObject
2. In VR Movement Controller component:
   - XR Origin: Drag "[BuildingBlock] Camera Rig" here

## Verification

After setup, verify:
- ✅ RoundManager has all references assigned
- ✅ VRMovementController has CharacterController, VRMovementController, and PlayerMovementRecorder
- ✅ EnemySpawner has EnemySpawner script
- ✅ PlayerRespawner has PlayerRespawner script
- ✅ No errors in Console

## Testing

1. Enter Play Mode
2. Round 1 should start automatically
3. Player should respawn at random location
4. One static enemy should spawn
5. Move around - movements are being recorded
6. Throw objects at enemy to defeat it
7. Round 2 should start with 2 enemies replaying your movements


