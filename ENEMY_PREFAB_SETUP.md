# Enemy Prefab Setup Guide

## Creating the Enemy Prefab

### Step 1: Create the Prefab

1. In Unity Editor, right-click in Hierarchy → **3D Object → Cube**
2. Name it: **Enemy**
3. Set Transform:
   - Position: (0, 0, 0)
   - Scale: (1, 1, 1)

### Step 2: Add Required Components

1. **Rigidbody**:
   - Mass: 1
   - Use Gravity: ✓ (checked - enemies will fall with gravity)
   - Constraints: Freeze Rotation X and Z (keep Y free for rotation)

2. **EnemyController**:
   - Max Health: **100** (you can edit this value!)
   - Current Health: (auto-set at runtime)
   - Damage Material: (optional - for visual feedback)

3. **CombatSystem**:
   - Damage Multiplier: 1.0
   - Min Damage Velocity: 2.0
   - Max Damage: 50.0

4. **Box Collider** (already on Cube):
   - Ensure it's enabled
   - Size: (1, 1, 1)

### Step 3: Add Visual Material (Red Color)

1. Create or assign a Material:
   - Right-click in Project → Create → Material
   - Name it: **EnemyMaterial**
   - Set color to **Red** (RGB: 255, 0, 0)
   - Drag it to the Enemy's MeshRenderer component
   - Note: Enemies are automatically set to red if no material is assigned

### Step 4: Save as Prefab

1. Create a **Prefabs** folder in Assets (if it doesn't exist)
2. Drag the **Enemy** GameObject from Hierarchy to the Prefabs folder
3. Delete the Enemy from the scene (we only need the prefab)
4. The prefab is now saved at: `Assets/Prefabs/Enemy.prefab`

### Step 5: Assign Prefab to EnemySpawner

1. Select **EnemySpawner** GameObject in Hierarchy
2. In Inspector, find **Enemy Spawner** component
3. Drag the **Enemy** prefab to the **Enemy Prefab** field
4. The system will now use your prefab instead of creating enemies procedurally

## Editing Enemy HP

### Method 1: Edit on Prefab (Recommended)

1. Open the **Enemy** prefab (double-click in Project window)
2. Select the root GameObject
3. In Inspector, find **Enemy Controller** component
4. Change **Max Health** value (e.g., 50, 100, 150, etc.)
5. Save the prefab (Ctrl+S or File → Save)
6. All enemies spawned will use this HP value

### Method 2: Edit Individual Enemy (Runtime)

1. In Play Mode, select an enemy in Hierarchy
2. In Inspector, find **Enemy Controller** component
3. Change **Max Health** value
4. Note: This only affects that specific enemy instance

## Prefab Benefits

✅ **Easy HP Editing**: Change HP once on prefab, affects all enemies
✅ **Visual Customization**: Add models, materials, particles easily
✅ **Better Performance**: Components pre-configured, faster spawning
✅ **Maintainability**: One place to configure all enemy properties

## Notes

- The **EnemyAI** or **StaticEnemy** components are added automatically at runtime based on round
- You don't need to add these to the prefab
- The prefab should have: Rigidbody, EnemyController, CombatSystem, and a Collider
- HP is fully editable on the prefab - change it anytime!

## Troubleshooting

### Enemies Not Spawning
- Check EnemySpawner has the prefab assigned
- Verify prefab has EnemyController component
- Check console for errors

### HP Not Changing
- Make sure you're editing the prefab, not a scene instance
- Save the prefab after making changes
- Check EnemyController component is on the prefab

### Components Missing
- Ensure prefab has: Rigidbody, EnemyController, CombatSystem
- EnemyAI/StaticEnemy are added automatically - don't add them to prefab

