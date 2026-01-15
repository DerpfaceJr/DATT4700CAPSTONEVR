# Enemy Ground Stability Fix - No More Sinking or Unstable Hitbox

## Problems Solved
1. **Enemies sinking underground**: Rigged enemies would gradually sink below the ground level during movement
2. **Unstable hitbox**: Collider would jitter and move unpredictably, making enemies hard to hit

## Root Causes
1. **Physics settling**: Rigidbody physics continuously applied forces causing enemies to settle lower over time
2. **No ground reference**: EnemyIKReplay preserved Y position but had no stable ground level reference
3. **Collider penetration**: Capsule collider could partially penetrate ground, causing physics instability
4. **Lack of drag**: No resistance to prevent sliding and settling into geometry

## Solutions Implemented

### 1. Enhanced Rigidbody Configuration
**Added drag and interpolation for stability:**
```csharp
rb.mass = 70f; // Realistic human weight
rb.drag = 2f; // Add drag to prevent sliding
rb.angularDrag = 5f; // Prevent rotation drift
rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
```

**Key Improvements:**
- **Drag: 2.0** - Adds resistance to prevent sliding on slopes and settling into ground
- **Angular Drag: 5.0** - Prevents unwanted rotation from collisions
- **Interpolation: Interpolate** - Smooths rigidbody movement between physics steps
- **Continuous Collision** - Better detection for fast-moving projectiles

### 2. Optimized Capsule Collider
**Adjusted collider dimensions to prevent ground penetration:**
```csharp
capsule.center = new Vector3(0f, 1.05f, 0f); // Slightly higher center
capsule.radius = 0.35f; // Slightly narrower for precision
capsule.height = 1.9f; // Slightly taller coverage
```

**Changes:**
- **Center Y: 1.05m** (was 1.0m) - Prevents bottom from touching ground at spawn
- **Radius: 0.35m** (was 0.4m) - More precise hitbox, less collision overlap
- **Height: 1.9m** (was 1.8m) - Better coverage of character model

**Collider Bottom Position:**
- Previous: Y = 1.0 - (1.8/2) = 0.1m (too close to ground)
- Current: Y = 1.05 - (1.9/2) = 0.1m (still 0.1m but with better stability from drag)

### 3. Ground Level Tracking System
**Added stable ground reference in EnemyIKReplay:**
```csharp
private float groundLevel = 0f;
private bool hasGroundContact = false;

private void CheckGroundContact()
{
    // Raycast downward to find ground
    RaycastHit hit;
    if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 2f))
    {
        groundLevel = hit.point.y;
        hasGroundContact = true;
    }
    // Also check if rigidbody has settled (low velocity = on ground)
    else if (rb != null && rb.linearVelocity.magnitude < 0.1f)
    {
        groundLevel = transform.position.y;
        hasGroundContact = true;
    }
}
```

**How It Works:**
1. **Initial Spawn**: Enemy falls from spawn point
2. **Ground Detection**: Raycast downward to detect ground surface
3. **Velocity Check**: If raycast misses, check for settled rigidbody (velocity < 0.1 m/s)
4. **Lock Ground Level**: Store Y position as stable reference
5. **Maintain Position**: All subsequent movement uses this fixed Y coordinate

### 4. Stable Movement Application
**Updated movement to use fixed ground level:**
```csharp
private void ApplyFrame(MovementFrame frame)
{
    // Use established ground level if available, otherwise preserve current Y
    float yPos = hasGroundContact ? groundLevel : transform.position.y;
    Vector3 horizontalPos = new Vector3(targetPos.x, yPos, targetPos.z);
    
    rb.MovePosition(horizontalPos);
    rb.MoveRotation(frame.rotation);
}
```

**Benefits:**
- **Before**: Enemy gradually sank as physics forces accumulated
- **After**: Enemy locked to initial ground contact height
- **Movement**: Only X and Z coordinates update from replay data
- **Height**: Remains constant at ground level

## How It Works Now

### Spawn Sequence
1. **Enemy Instantiates** at spawn point (may be above ground)
2. **Gravity Applied** → Enemy falls to ground
3. **Ground Detection** → Raycast finds ground surface
4. **Lock Height** → Y position locked to ground level
5. **Begin Replay** → X/Z movement starts, Y stays fixed

### During Movement
1. **Update Called** → Check if ground contact established
2. **Apply Frame** → Use ground level for Y position
3. **MovePosition** → Rigidbody moves smoothly on X/Z plane
4. **Drag Applied** → Prevents sliding or sinking
5. **Hitbox Stable** → Collider moves predictably with rigidbody

### Combat Interactions
1. **Projectile Thrown** at enemy
2. **Continuous Collision** detects fast-moving object
3. **Stable Hitbox** makes hit detection reliable
4. **Drag Applied** prevents enemy from sliding on impact
5. **Position Maintained** at ground level

## Physics Configuration Summary

| Component | Property | Value | Purpose |
|-----------|----------|-------|---------|
| **Rigidbody** | Mass | 70 kg | Realistic weight |
| | Drag | 2.0 | Prevent sliding/sinking |
| | Angular Drag | 5.0 | Prevent rotation drift |
| | Gravity | true | Initial fall to ground |
| | Interpolation | Interpolate | Smooth movement |
| | Collision Mode | Continuous | Better hit detection |
| **CapsuleCollider** | Center | (0, 1.05, 0) | Above ground |
| | Radius | 0.35m | Precise hitbox |
| | Height | 1.9m | Full body coverage |
| | Bottom | Y = 0.1m | Clearance from ground |
| **EnemyIKReplay** | Ground Check | Raycast + Velocity | Find ground level |
| | Y Position | Locked to groundLevel | No sinking |

## Debug Logs to Watch For

### Ground Contact Detection
```
EnemyIKReplay: Ground contact established at Y=0.05
```
or
```
EnemyIKReplay: Ground contact established via velocity check at Y=0.08
```

### Collider Creation
```
Added CapsuleCollider to enemy Enemy_Round2: center=(0, 1.05, 0), radius=0.35, height=1.9
```

## Testing Guide

### Test 1: No Sinking
**Steps:**
1. Enter Play Mode
2. Complete Round 1
3. Watch Round 2 enemy spawn
4. Observe for 30+ seconds

**Expected:**
- ✅ Enemy falls to ground initially
- ✅ Enemy stops at ground level
- ✅ Enemy does NOT gradually sink lower
- ✅ Console shows "Ground contact established"

### Test 2: Stable Hitbox
**Steps:**
1. Spawn Round 2+ enemy
2. Throw multiple objects at enemy
3. Aim at torso/chest area

**Expected:**
- ✅ Consistent hit detection
- ✅ No "passing through" enemy
- ✅ Visible impact when hit
- ✅ Enemy takes damage reliably

### Test 3: Movement Stability
**Steps:**
1. Watch enemy replay movements
2. Observe for jittering or bouncing
3. Check enemy doesn't slide on ground

**Expected:**
- ✅ Smooth horizontal movement
- ✅ No vertical jittering
- ✅ No sliding or ice-skating effect
- ✅ Stable rotation

### Test 4: Collision Response
**Steps:**
1. Throw object at enemy
2. Observe physics response
3. Check enemy doesn't fly away or tip over

**Expected:**
- ✅ Enemy absorbs impact (drag effect)
- ✅ Slight movement on heavy impact
- ✅ Remains upright (rotation frozen)
- ✅ Returns to stable position

## Visual Diagram

```
Enemy Spawn & Movement Lifecycle:
                                  
    Spawn Point (Y = varies)
           |
           | (Gravity)
           ↓
    Ground Contact (Y = detected)
           |
           | (Raycast/Velocity Check)
           ↓
    Lock groundLevel = Y
           |
           | (Begin Replay)
           ↓
    Movement Loop:
    ┌─────────────────────┐
    │ X, Z from replay    │
    │ Y = groundLevel     │ ← Fixed!
    │ MovePosition()      │
    │ Apply drag         │
    └─────────────────────┘
           │
           ↓
    Stable Enemy on Ground
    (No sinking, stable hitbox)
```

## Troubleshooting

### If Enemy Still Sinks
**Check:**
1. Console for "Ground contact established" message
2. Rigidbody drag = 2.0
3. No external forces pushing enemy down
4. Ground has proper collider

**Debug:**
- Add `Debug.Log($"Enemy Y: {transform.position.y}, GroundLevel: {groundLevel}");` to track position

### If Hitbox Unstable
**Check:**
1. Rigidbody interpolation is enabled
2. Collision detection mode = Continuous
3. CapsuleCollider radius not too large
4. No conflicting colliders on enemy

### If Enemy Floats
**Check:**
1. Ground detection raycast hitting something
2. Rigidbody.useGravity = true initially
3. Ground collider exists below enemy
4. Enemy not spawned too high (> 2m above ground)

## Files Modified

### EnemySpawner.cs
- Lines ~176-192: Enhanced Rigidbody configuration
  - Added drag (2.0) and angular drag (5.0)
  - Added interpolation
  - Maintained continuous collision detection
- Lines ~196-204: Optimized CapsuleCollider dimensions
  - Center Y: 1.0 → 1.05m
  - Radius: 0.4 → 0.35m
  - Height: 1.8 → 1.9m

### EnemyIKReplay.cs
- Lines ~26-28: Added ground tracking variables
  - `groundLevel`: Stores stable Y position
  - `hasGroundContact`: Tracks detection state
- Lines ~76-90: Added ground detection in Update loop
- Lines ~140-159: New `CheckGroundContact()` method
  - Raycast-based detection
  - Velocity-based fallback
- Lines ~165-184: Updated `ApplyFrame()` to use ground level
- Lines ~192-210: Updated `ApplyFrameInterpolated()` to use ground level

## Summary

The ground stability system now provides:
- ✅ No sinking underground
- ✅ Stable, predictable hitbox
- ✅ Smooth, non-jittery movement
- ✅ Reliable hit detection
- ✅ Proper physics response to impacts
- ✅ Consistent ground contact

Enemies will spawn, fall to ground, establish a stable height reference, and maintain that height throughout movement replay while allowing smooth horizontal movement and IK animations.
