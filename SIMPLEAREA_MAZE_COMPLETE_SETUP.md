# SimpleArea Maze Scene - Complete Setup Guide

## ✅ Changes Already Applied

### 1. Removed Elevated Objects
- **CenterPlatform**: Disabled (no longer accessible)
- **All Walls**: Lowered from y: 1.25 to y: 0 (ground level)
- **All Cubes**: Lowered from y: 1-1.5 to y: 0.5 (ground level, half height)

### 2. Scripts Created
- ✅ `VRMovementController.cs` - Smooth locomotion
- ✅ `VRGrabThrow.cs` - Grab and throw functionality

## 🎮 Setup Instructions

### Step 1: Open Scene in Unity
1. Open Unity Editor
2. Load scene: `Assets/Scenes/SimpleArea.unity/SimpleArea.unity`

### Step 2: Set Up VR Movement

1. **Find or Create Camera Rig**:
   - Look for `[BuildingBlock] Camera Rig` in Hierarchy
   - If not present, add via: Window → Meta → Building Blocks → Camera Rig

2. **Create VR Movement Controller**:
   - Right-click in Hierarchy → Create Empty
   - Name: "VRMovementController"
   - Position: (0, 0, 0)
   - Add Component → Character Controller
   - Add Component → Scripts → VR Movement Controller

3. **Configure Character Controller**:
   - Center: (0, 1, 0)
   - Radius: 0.3
   - Height: 2

4. **Configure VR Movement Controller**:
   - Move Speed: 2
   - Enable Smooth Movement: ✓
   - Enable Snap Turn: ✓
   - Snap Turn Angle: 45
   - XR Origin: Drag `[BuildingBlock] Camera Rig` here

### Step 3: Set Up Grab/Throw

1. **Find Controllers**:
   - Look for left and right controller GameObjects in the Camera Rig hierarchy
   - They might be named like "left_oculus_controller_world" or similar

2. **Add VRGrabThrow Script**:
   - Select left controller GameObject
   - Add Component → Scripts → VR Grab Throw
   - Repeat for right controller

3. **Configure VRGrabThrow**:
   - Grab Distance: 0.5
   - Throw Force: 10
   - Grabbable Layer: Default (or create "Grabbable" layer)

### Step 4: Create Maze Layout

#### Option A: Using Unity Editor (Recommended)

1. **Duplicate Walls for Maze**:
   - Select WestWall
   - Duplicate (Ctrl+D) 3-4 times
   - Reposition to create corridors:
     - Wall1: Position (0, 0, 1), Scale (0.1, 2.5, 2) - creates corridor
     - Wall2: Position (1, 0, 0), Scale (2, 2.5, 0.1) - creates another path
     - Wall3: Position (-1, 0, -1), Scale (1, 2.5, 0.1) - creates dead end
     - Wall4: Position (0, 0, -1), Scale (0.1, 2.5, 1.5) - creates maze path

2. **Add More Grabbable Objects**:
   - Duplicate GrabCube1, GrabCube2, GrabCube3
   - Position them throughout the maze at y: 0.5
   - Ensure they have Rigidbody (not kinematic) and Box Collider

#### Option B: Using Unity MCP

Use the `manage_gameobject` tool to create walls:
```python
# Example: Create internal maze wall
mcp_unityMCP_manage_gameobject(
    action="create",
    name="MazeWall1",
    primitive_type="Cube",
    position=[0, 0, 1],
    scale=[0.1, 2.5, 2]
)
```

### Step 5: Ensure Objects Are Grabbable

1. **Select all grabbable objects**:
   - GrabCube1, GrabCube2, GrabCube3
   - InteractiveCube1, InteractiveCube2
   - PhysicsBall1, PhysicsBall2, PhysicsBall3

2. **Verify Components**:
   - ✅ Rigidbody (not kinematic)
   - ✅ Collider (Box or Sphere)
   - ✅ Material assigned (for visibility)

3. **Optional: Tag as "Grabbable"**:
   - Select objects
   - In Inspector, set Tag to "Grabbable" (create tag if needed)
   - Update VRGrabThrow layer mask to use this tag

## 🎯 Maze Layout Suggestions

### Simple Maze Pattern:
```
[West]  [Wall]  [Wall]  [East]
[Wall]  [Open]  [Wall]  [Wall]
[Open]  [Wall]  [Open]  [Wall]
[Wall]  [Open]  [Wall]  [Wall]
[South] [Open]  [Open]  [North]
```

### Wall Positions for Maze:
- Internal Wall 1: (0, 0, 1) - Scale (0.1, 2.5, 2)
- Internal Wall 2: (1, 0, 0) - Scale (2, 2.5, 0.1)
- Internal Wall 3: (-1, 0, -1) - Scale (1, 2.5, 0.1)
- Internal Wall 4: (0, 0, -1) - Scale (0.1, 2.5, 1.5)
- Internal Wall 5: (1.5, 0, 1) - Scale (1, 2.5, 0.1)

### Grabbable Object Positions:
- Cube at (0, 0.5, 0) - center
- Cube at (-1.5, 0.5, 1) - in corridor
- Cube at (1.5, 0.5, -1) - in another area
- Ball at (0, 0.5, 1.5) - near wall
- Ball at (-1, 0.5, -1.5) - in corner

## 🎮 Controls

### Movement
- **Left Thumbstick**: Move forward/backward and strafe
- **Right Thumbstick Left/Right**: Snap turn 45 degrees

### Grab/Throw
- **Grip Button or Trigger**: Grab object (hold)
- **Release**: Throw object with physics

## ✅ Verification Checklist

- [ ] All walls at y: 0 (ground level)
- [ ] CenterPlatform disabled
- [ ] All grabbable objects at y: 0.5
- [ ] VR Movement Controller set up
- [ ] Grab/Throw scripts on controllers
- [ ] Maze walls created
- [ ] Objects have Rigidbody and Colliders
- [ ] Test movement in Play Mode
- [ ] Test grabbing objects
- [ ] Test throwing objects

## 🐛 Troubleshooting

### Movement Not Working
- Check Character Controller is added
- Verify XR Origin is assigned
- Ensure controllers are connected

### Can't Grab Objects
- Verify objects have Rigidbody (not kinematic)
- Check grab distance is sufficient
- Ensure layer mask includes object layers

### Objects Fall Through Floor
- Verify floor has a collider
- Check object colliders are enabled
- Ensure Rigidbody is not kinematic

## 📝 Notes

- All objects are now at ground level - no jumping required
- Maze can be expanded by adding more walls
- Grabbable objects can be positioned throughout the maze
- Use Unity MCP or Editor to add more maze complexity

