# Rigged Enemy Prefab Setup Guide

## Overview
This guide will help you set up the enemy prefab to use the rigged character model (Ch44_nonPBR) with full IK animations, matching the player character setup.

## Prerequisites
- Player tag already exists (✓ confirmed)
- Player character has IKTargetFollowVRRig component set up
- Ch44_nonPBR FBX model imported

## Step-by-Step Setup

### Step 1: Open Enemy Prefab
1. In Unity Project window, navigate to `Assets/Enemy.prefab`
2. Double-click to open in **Prefab Mode**
3. You should see a Cube GameObject as the root

### Step 2: Replace Cube with Rigged Character Model
1. **Delete** the Cube child object (if it exists)
2. In Project window, find `Assets/Ch44_nonPBR.fbx`
3. **Drag** the Ch44_nonPBR model into the prefab hierarchy as a child of the root GameObject
4. The model should appear in the prefab

### Step 3: Add IKTargetFollowVRRig Component
1. Select the **root GameObject** (Enemy) in the prefab
2. Click **Add Component**
3. Search for and add: **IK Target Follow VR Rig**
4. The component should appear in the Inspector

### Step 4: Set Up IK Targets
The IKTargetFollowVRRig component needs three IK target references. You need to find these in the character hierarchy:

#### Finding IK Targets:
1. **Expand** the Ch44_nonPBR model in the hierarchy
2. Look for these GameObjects (they should exist if the character was set up like the player):
   - **"Head IK"** → **"Head IK_target"** (for head tracking)
   - **"Left Arm IK"** → **"Left Arm IK_target"** (for left hand)
   - **"Right Arm IK"** → **"Right Arm IK_target"** (for right hand)

#### Assigning IK Targets:
1. In the **IK Target Follow VR Rig** component:
   - **Head → Ik Target**: Drag "Head IK_target" from hierarchy
   - **Left Hand → Ik Target**: Drag "Left Arm IK_target" from hierarchy  
   - **Right Hand → Ik Target**: Drag "Right Arm IK_target" from hierarchy
2. Leave **Vr Target** fields empty (enemies don't use VR tracking)
3. Set **Head Body Position Offset**: (0, -0.54, 0)
4. Set **Turn Smoothness**: 0.1

### Step 5: Verify Required Components
Ensure the root GameObject has these components:
- ✓ **Rigidbody** (should already exist)
- ✓ **EnemyController** (should already exist)
- ✓ **CombatSystem** (should already exist)
- ✓ **IKTargetFollowVRRig** (just added)

### Step 6: Set Up Hitboxes (Optional but Recommended)
For proper combat detection, add colliders to character bones:

1. Find the character's bone hierarchy (mixamorig:Head, mixamorig:LeftHand, mixamorig:RightHand, etc.)
2. Add **Capsule Colliders** or **Sphere Colliders** to:
   - Head bone
   - Torso/chest bone
   - Left hand bone
   - Right hand bone
3. Set colliders as **Triggers** if using trigger-based combat
4. Adjust collider sizes to match bone proportions

### Step 7: Save Prefab
1. Click **Open Prefab** button (top of Inspector) to exit Prefab Mode
2. Unity will automatically save changes
3. Or press **Ctrl+S** to save

### Step 8: Tag Player Character (If Not Done)
1. In your scene, find the player's rigged character GameObject
2. Select it
3. In Inspector, set **Tag** to **"Player"**
4. This allows PlayerMovementRecorder to find and record IK data

## Verification

### Test in Play Mode:
1. Enter Play Mode
2. Check Console for:
   - `"PlayerMovementRecorder: Found IK rig, will record IK target data"` ✓
   - `"Enemy X (Rigged): Assigned Y movement frames with IK data"` ✓
3. Spawn enemies and verify they replay your movements with full animations

### Troubleshooting

**Problem**: IK targets not found
- **Solution**: The character model may need IK targets created. Copy the IK setup from the player character in the scene.

**Problem**: Enemies still spawn as cubes
- **Solution**: Ensure EnemySpawner has the updated Enemy prefab assigned in its "Enemy Prefab" field.

**Problem**: No IK data being recorded
- **Solution**: Verify player character has "Player" tag and IKTargetFollowVRRig component is properly configured.

**Problem**: Enemies don't animate
- **Solution**: Check that EnemyIKReplay component is being added (should happen automatically if IKTargetFollowVRRig is detected).

## Using the Editor Tool

An editor tool is available to help automate some setup:

1. Go to **Tools → Setup Rigged Enemy Prefab**
2. Assign the Enemy prefab
3. Click **"Setup Enemy Prefab"**
4. The tool will add IKTargetFollowVRRig component
5. You still need to manually assign IK target references

## Notes

- The enemy prefab doesn't need VR targets (vrTarget fields) - those are only for the player
- Enemies will automatically use EnemyIKReplay instead of EnemyAI when IKTargetFollowVRRig is detected
- The system is backward compatible - enemies without rigged models will still work with basic movement replay

