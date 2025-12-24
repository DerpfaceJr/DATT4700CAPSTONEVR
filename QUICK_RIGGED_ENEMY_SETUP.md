# Quick Setup: Rigged Enemy Prefab

## What's Been Done
✅ Code implementation complete - enemies can now use rigged animations
✅ Player tag exists
✅ Helper scripts created to assist with setup

## What You Need to Do

### Option 1: Manual Setup (Recommended for First Time)

1. **Open Enemy Prefab**
   - In Project window: `Assets/Enemy.prefab`
   - Double-click to open in Prefab Mode

2. **Add Rigged Character Model**
   - Delete the Cube child (if present)
   - Drag `Assets/Ch44_nonPBR.fbx` into the prefab hierarchy

3. **Add IKTargetFollowVRRig Component**
   - Select root GameObject (Enemy)
   - Add Component → Search "IK Target Follow VR Rig"
   - Add it

4. **Assign IK Targets**
   - In IKTargetFollowVRRig component:
     - **Head → Ik Target**: Find "Head IK_target" in hierarchy, drag it here
     - **Left Hand → Ik Target**: Find "Left Arm IK_target", drag it here
     - **Right Hand → Ik Target**: Find "Right Arm IK_target", drag it here
   - Set **Head Body Position Offset**: (0, -0.54, 0)

5. **Add Helper Script (Optional)**
   - Add Component → "Enemy Prefab Setup Helper"
   - Right-click component → "Verify Setup" to check everything

6. **Save Prefab**
   - Exit Prefab Mode or press Ctrl+S

### Option 2: Using Helper Scripts

1. **Add Helper to Prefab**
   - Open `Assets/Enemy.prefab` in Prefab Mode
   - Add Component → "Enemy Prefab Setup Helper"

2. **Use Auto-Find (If IK Targets Exist)**
   - Right-click "Enemy Prefab Setup Helper" component
   - Select "Auto-Find IK Targets"
   - This will try to find and assign IK targets automatically

3. **Verify Setup**
   - Right-click component → "Verify Setup"
   - Check Console for setup status

### Option 3: Using Editor Tool

1. **Open Editor Tool**
   - Menu: **Tools → Setup Rigged Enemy Prefab**

2. **Assign References**
   - Drag Enemy prefab to "Enemy Prefab" field
   - (Optional) Drag Ch44_nonPBR model

3. **Click "Setup Enemy Prefab"**
   - Tool will add IKTargetFollowVRRig component
   - You still need to assign IK target references manually

## Tag Player Character

1. In your scene, find the player's rigged character GameObject
2. Select it
3. In Inspector, set **Tag** to **"Player"**
4. This enables IK data recording

## Verification Checklist

After setup, verify:
- [ ] Enemy prefab has IKTargetFollowVRRig component
- [ ] All three IK targets are assigned (Head, Left Hand, Right Hand)
- [ ] EnemyController component exists
- [ ] CombatSystem component exists
- [ ] Player character has "Player" tag
- [ ] Player character has IKTargetFollowVRRig configured

## Testing

1. Enter Play Mode
2. Check Console:
   - Should see: `"PlayerMovementRecorder: Found IK rig, will record IK target data"`
   - Should see: `"Enemy X (Rigged): Assigned Y movement frames with IK data"`
3. Enemies should now replay your movements with full rigged animations!

## Troubleshooting

**"No IK targets found"**
- The character model needs IK targets set up
- Copy the IK setup from player character in scene
- Or create IK targets manually (see full guide)

**"Enemies still spawn as cubes"**
- Check EnemySpawner has Enemy prefab assigned
- Verify prefab was saved after modifications

**"No IK data recorded"**
- Verify player has "Player" tag
- Check player has IKTargetFollowVRRig component
- Check Console for warnings

## Files Created

- `Assets/Scripts/EnemyPrefabSetupHelper.cs` - Runtime verification helper
- `Assets/Editor/SetupRiggedEnemyPrefabEditor.cs` - Editor tool
- `Assets/Scripts/SetupRiggedEnemyPrefab.cs` - Setup script
- `RIGGED_ENEMY_SETUP_GUIDE.md` - Detailed guide
- `QUICK_RIGGED_ENEMY_SETUP.md` - This file

## Need More Help?

See `RIGGED_ENEMY_SETUP_GUIDE.md` for detailed step-by-step instructions with screenshots guidance.

