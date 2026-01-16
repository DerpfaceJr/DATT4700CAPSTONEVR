# Rigged Enemy Character Height Fix

## Problem
Rigged enemies were appearing with their Y position too low, causing their character models to be folded together and clipping through the ground. The characters were not standing properly on the ground surface.

## Root Cause
The `EnemyIKReplay.cs` script was setting the enemy's root transform position directly at ground level (Y=0), without accounting for the character's height offset. This caused the character's pivot point to be positioned at ground level, making the character appear to sink or clip through the ground.

### Technical Details
- **CapsuleCollider Configuration**: Center at `(0, 1.05, 0)` with height `1.9`
  - Collider bottom: Y = 1.05 - 0.95 = 0.1
  - Collider top: Y = 1.05 + 0.95 = 2.0
- **Issue**: Root transform was being set to Y=0 (ground level), but needed to be at Y=0.1 for character's feet to touch the ground properly

## Solution
Modified `EnemyIKReplay.cs` to calculate and apply a proper character height offset based on the CapsuleCollider dimensions.

### Changes Made

#### 1. Added Character Height Offset Tracking
**File**: `Assets/Scripts/EnemyIKReplay.cs`

**Added field**:
```csharp
private float characterHeightOffset = 0f; // Height offset for character above ground (calculated from collider)
```

#### 2. Calculate Height Offset in Start()
**Added logic in `Start()` method**:
```csharp
// Calculate character height offset from collider
CapsuleCollider capsule = GetComponent<CapsuleCollider>();
if (capsule != null)
{
    // Character should be positioned so bottom of collider touches ground
    // CapsuleCollider center is relative to transform, height is total height
    characterHeightOffset = capsule.center.y - (capsule.height / 2f);
    Debug.Log($"EnemyIKReplay: Calculated character height offset = {characterHeightOffset} (collider center.y={capsule.center.y}, height={capsule.height})");
}
else
{
    // Fallback: assume character pivot is at feet
    characterHeightOffset = 0.1f; // Small offset to prevent ground clipping
    Debug.LogWarning($"EnemyIKReplay: No CapsuleCollider found, using default height offset = {characterHeightOffset}");
}
```

**Calculation**:
- `characterHeightOffset = capsule.center.y - (capsule.height / 2f)`
- With center = `1.05` and height = `1.9`:
  - `characterHeightOffset = 1.05 - 0.95 = 0.1`
- This ensures the bottom of the collider (and character's feet) touch the ground

#### 3. Apply Height Offset to Y Position
**Modified `ApplyFrame()` method**:
```csharp
// OLD CODE:
float yPos = hasGroundContact ? groundLevel : transform.position.y;

// NEW CODE:
float yPos = hasGroundContact ? (groundLevel + characterHeightOffset) : transform.position.y;
```

**Modified `ApplyFrameInterpolated()` method**:
```csharp
// OLD CODE:
float yPos = hasGroundContact ? groundLevel : transform.position.y;

// NEW CODE:
float yPos = hasGroundContact ? (groundLevel + characterHeightOffset) : transform.position.y;
```

## How It Works

### Before Fix
1. Enemy spawns above ground
2. Gravity pulls enemy down
3. Ground contact detected at Y=0
4. Enemy's root transform set to Y=0
5. **Result**: Character sinks through ground, appears folded/clipped

### After Fix
1. Enemy spawns above ground
2. Gravity pulls enemy down
3. Ground contact detected at Y=0
4. **Character height offset calculated** from CapsuleCollider (0.1)
5. Enemy's root transform set to Y=0.1
6. **Result**: Character stands properly on ground, no clipping

### Visual Explanation
```
Ground Level: Y=0
    |
    |____ [Ground Surface]
    |
    +0.1  <--- characterHeightOffset (bottom of collider)
    |
    |     [Character Model]
    |     [    Body     ]
    |     [    Legs     ]
    |     [   Feet    ] <--- at Y=0.1 (just above ground)
    |
    +1.05 <--- Character Root Transform (collider center)
    |
    +2.0  <--- Top of collider (head)
```

## Expected Behavior

### Round 1
- Static enemy (red cube) spawns at correct height
- No height issues (simple cube primitive)

### Round 2+
- Rigged character enemies spawn at correct height
- Characters land on ground with physics
- **Ground contact established** → height offset applied
- Characters stand upright on ground surface
- No clipping through ground
- No folded/compressed character models
- Characters move horizontally while maintaining proper standing height

## Testing
1. **Enter Play Mode**
2. **Complete Round 1**
3. **Watch Round 2 enemies spawn**:
   - Enemies should fall and land on ground
   - Characters should stand upright (not folded)
   - Feet should be on ground surface (not sunk in)
   - Body should be fully visible and not clipped

### Debug Logs
Check console for height calculation:
```
EnemyIKReplay: Calculated character height offset = 0.1 (collider center.y=1.05, height=1.9)
```

## Configuration Summary

### CapsuleCollider (EnemySpawner.cs)
- **Center**: `(0, 1.05, 0)`
- **Radius**: `0.35`
- **Height**: `1.9`
- **Direction**: Y-axis (1)

### Height Offset Calculation
- **Formula**: `characterHeightOffset = center.y - (height / 2)`
- **Result**: `0.1` (for standard rigged character)
- **Meaning**: Character root must be 0.1 units above ground for feet to touch properly

### Physics Settings (Rigidbody)
- **Mass**: 70kg
- **Drag**: 2.0 (prevents sinking)
- **Angular Drag**: 5.0 (prevents rotation drift)
- **Gravity**: Enabled
- **Interpolation**: Enabled (smooth movement)
- **Collision Detection**: Continuous

## Benefits
✅ Characters stand properly on ground
✅ No clipping through ground surface
✅ No folded/compressed character models
✅ Proper vertical positioning maintained during replay
✅ Automatic calculation based on collider dimensions
✅ Fallback handling if collider not found

## Files Modified
- `Assets/Scripts/EnemyIKReplay.cs`
  - Added `characterHeightOffset` field
  - Added height offset calculation in `Start()`
  - Updated `ApplyFrame()` to use offset
  - Updated `ApplyFrameInterpolated()` to use offset

## Related Systems
- **EnemySpawner.cs**: Configures CapsuleCollider dimensions
- **Ground Detection**: Raycast system detects ground level
- **Physics System**: Rigidbody with gravity for initial landing
- **Replay System**: Maintains horizontal movement with stable height
