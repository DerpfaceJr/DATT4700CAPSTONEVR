# SimpleArea Maze Scene Setup

## Changes Made

### ✅ Removed Elevated Objects
- **CenterPlatform**: Disabled (was at y: -0.11, now inactive)
- **Walls**: Lowered from y: 1.25 to y: 0 (ground level)
  - WestWall: y: 1.25 → y: 0
  - EastWall: y: 1.25 → y: 0  
  - NorthWall: y: 1.25 → y: 0
  - SouthWall: y: 1.25 → y: 0

### ✅ Lowered Grabbable Objects
- **GrabCube1**: Lowered from y: 1 → y: 0.5
- **GrabCube2**: Lowered from y: 1 → y: 0.5
- **GrabCube3**: Lowered from y: 1 → y: 0.5
- **InteractiveCube1**: Lowered from y: 1.5 → y: 0.5
- **InteractiveCube2**: Lowered from y: 1.5 → y: 0.5

## Scripts Created

### 1. VRMovementController.cs
- **Location**: `Assets/Scripts/VRMovementController.cs`
- **Purpose**: Enables smooth locomotion in VR
- **Controls**:
  - Left thumbstick: Move forward/backward and strafe
  - Right thumbstick: Snap turning (45° increments)

### 2. VRGrabThrow.cs
- **Location**: `Assets/Scripts/VRGrabThrow.cs`
- **Purpose**: Enables grabbing and throwing objects
- **Controls**:
  - Grip button or Trigger: Grab/release objects
  - Throws objects with physics when released

## Next Steps in Unity Editor

### 1. Set Up VR Movement
1. Find or create the Camera Rig GameObject (`[BuildingBlock] Camera Rig`)
2. Create an empty GameObject named "VRMovementController"
3. Add Character Controller component
4. Add VRMovementController script
5. Set Character Controller:
   - Center: (0, 1, 0)
   - Radius: 0.3
   - Height: 2
6. Assign Camera Rig to "XR Origin" field in VRMovementController

### 2. Set Up Grab/Throw
1. Find controller GameObjects (left and right)
2. Add VRGrabThrow script to each controller
3. Configure:
   - Grab Distance: 0.5
   - Throw Force: 10
   - Grabbable Layer: Default (or create a "Grabbable" layer)

### 3. Make Objects Grabbable
1. Select GrabCube1, GrabCube2, GrabCube3, InteractiveCube1, InteractiveCube2
2. Ensure they have:
   - Rigidbody component (not kinematic)
   - Box Collider
   - Tag them as "Grabbable" (optional, for filtering)

### 4. Create Maze Layout (Optional)
To create a more complex maze, you can:
1. Duplicate walls
2. Reposition them to create corridors
3. Add more walls at different positions
4. Create dead ends and pathways

Example maze positions:
- Wall at (0, 0, 1) - creates a corridor
- Wall at (1, 0, 0) - creates another path
- Wall at (-1, 0, -1) - creates a dead end

## Current Scene State

- ✅ All objects at ground level (no jumping required)
- ✅ Walls lowered to ground level
- ✅ CenterPlatform disabled
- ✅ Grabbable objects accessible
- ⚠️ VR Movement: Needs to be set up in Unity Editor
- ⚠️ Grab/Throw: Needs to be set up in Unity Editor

## Testing
1. Enter Play Mode
2. Put on VR headset
3. Test movement with left thumbstick
4. Test grabbing objects with grip/trigger
5. Test throwing by releasing grip/trigger

