# Rigged Enemy Physics Fix - Ground Contact & Hittability

## Problems Solved
1. **Floating Enemies**: Rigged enemies were floating mid-air instead of staying on the ground
2. **Unhittable**: Player couldn't hit enemies with throwable objects

## Root Cause
The Player prefab (used for rigged enemies) didn't have:
- Rigidbody component with gravity
- Collider component for physics interactions and combat detection

When instantiated as enemies, they had no physics and couldn't be hit.

## Solution Implemented

### 1. Added Rigidbody with Proper Physics
```csharp
Rigidbody rb = enemy.AddComponent<Rigidbody>();
rb.mass = 70f; // Typical human weight
rb.useGravity = true; // Enable gravity
rb.isKinematic = false; // Dynamic physics
rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // Prevent tipping over
rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better collision detection
```

**Key Settings:**
- **Mass: 70kg** - Realistic human weight, provides proper physics interactions
- **Gravity: Enabled** - Enemy falls to ground and stays there
- **Non-kinematic** - Allows physics forces to affect the enemy
- **Rotation Constraints** - Prevents enemy from falling over (keeps upright)
- **Continuous Collision** - Ensures fast-moving projectiles don't pass through

### 2. Added CapsuleCollider for Body
```csharp
CapsuleCollider capsule = enemy.AddComponent<CapsuleCollider>();
capsule.center = new Vector3(0f, 1f, 0f); // Center at body height
capsule.radius = 0.4f; // Wide enough for body
capsule.height = 1.8f; // Typical human height
capsule.direction = 1; // Y-axis (vertical)
```

**Collider Configuration:**
- **Type**: CapsuleCollider (good for humanoid characters)
- **Center**: (0, 1, 0) - Middle of character body
- **Radius**: 0.4m - Encompasses torso and arms
- **Height**: 1.8m - Full character height
- **Direction**: Vertical (Y-axis)

### 3. Updated Combat Threshold
```csharp
combatSystem.instakillMinVelocity = 1.5f; // Prevent death from touching static objects
```

Ensures enemies only die from thrown objects, not from touching objects on the ground.

## How It Works Now

### Physics Behavior
1. **Spawn**: Enemy instantiates at spawn point
2. **Gravity**: Enemy falls to ground immediately
3. **Ground Contact**: Rigidbody collider keeps enemy on ground
4. **Stability**: Rotation constraints prevent enemy from tipping over
5. **Movement**: EnemyIKReplay uses `MovePosition()` for physics-compatible movement

### Combat Detection
1. **Throwable Object**: Player throws object at high velocity
2. **Collision**: Object hits enemy's CapsuleCollider
3. **Velocity Check**: CombatSystem checks object velocity ≥ 1.5 m/s
4. **Damage/Instakill**: Enemy takes damage based on impact

## Visual Diagram

```
Enemy GameObject (Rigged Character)
├─ Transform (position, rotation)
├─ Rigidbody (mass=70kg, gravity=true) ← ADDED
│  └─ Collision Detection: Continuous
├─ CapsuleCollider (height=1.8m, radius=0.4m) ← ADDED
│  └─ Center: (0, 1, 0)
├─ EnemyController (health, death)
├─ CombatSystem (damage detection) ← UPDATED threshold
├─ EnemyIKReplay (movement replay)
├─ IKTargetFollowVRRig (IK system)
├─ Animator (skeleton animation)
└─ RigBuilder (IK constraints)
```

## Testing

### Test 1: Ground Contact
**Steps:**
1. Enter Play Mode
2. Complete Round 1
3. Observe Round 2 enemies spawn

**Expected:**
- ✅ Enemies fall to ground immediately
- ✅ Enemies stay on ground (don't float)
- ✅ Enemies move along ground during replay

**Debug Log:**
```
Added CapsuleCollider to enemy Enemy_Round2: center=(0, 1, 0), radius=0.4, height=1.8
```

### Test 2: Throwable Object Hit Detection
**Steps:**
1. Pick up a throwable cube
2. Throw it at an enemy
3. Ensure throw velocity is high

**Expected:**
- ✅ Object collides with enemy (visible impact)
- ✅ Enemy takes damage or dies instantly
- ✅ Console shows combat detection logs

**Debug Log:**
```
CombatSystem: Collision with Throwable at velocity 3.2 m/s (threshold: 1.5)
Enemy defeated! Remaining: X
```

### Test 3: Static Object Interaction
**Steps:**
1. Enemy walks near objects on ground
2. Enemy touches static/slow-moving objects

**Expected:**
- ✅ Enemy does NOT die from touching ground objects
- ✅ Enemy pushes objects slightly (physics interaction)
- ✅ No combat detection for slow collisions (< 1.5 m/s)

## Component Breakdown

### Rigidbody Configuration
| Property | Value | Purpose |
|----------|-------|---------|
| Mass | 70 kg | Realistic human weight for physics |
| Use Gravity | true | Enemy falls to ground |
| Is Kinematic | false | Allows physics forces |
| Constraints | FreezeRotation X,Z | Keeps enemy upright |
| Collision Mode | Continuous | Better projectile detection |

### CapsuleCollider Configuration
| Property | Value | Purpose |
|----------|-------|---------|
| Center | (0, 1, 0) | Middle of body |
| Radius | 0.4 m | Body width (torso + arms) |
| Height | 1.8 m | Full character height |
| Direction | Y-axis | Vertical orientation |

### CombatSystem Configuration
| Property | Value | Purpose |
|----------|-------|---------|
| Instakill Enabled | true | Thrown objects can instantly kill |
| Instakill Min Velocity | 1.5 m/s | Prevents death from static objects |
| Damage Multiplier | 1.0 | Standard damage |
| Min Damage Velocity | 2.0 m/s | Minimum for damage (non-instakill) |

## Code Changes

### Files Modified
- [`Assets/Scripts/EnemySpawner.cs`](Assets/Scripts/EnemySpawner.cs)
  - Lines ~173-202: Added CapsuleCollider creation
  - Lines ~174-189: Updated Rigidbody configuration
  - Lines ~237-252: Updated CombatSystem threshold

### Key Additions
1. **Rigidbody mass increased** to 70kg (was 1kg)
2. **Continuous collision detection** added
3. **CapsuleCollider creation** for rigged characters
4. **InstakillMinVelocity increased** to 1.5 m/s (was 0.5 m/s)

## Benefits

### Realistic Physics
- Enemies have proper weight and gravity
- Natural ground contact and movement
- Stable upright posture

### Combat Functionality
- Enemies can be hit by thrown objects
- Proper collision detection with projectiles
- No false positives from touching ground objects

### Performance
- Single collider per enemy (efficient)
- Continuous collision detection (accurate)
- Proper physics integration with IK replay

## Troubleshooting

### If Enemy Still Floats
**Possible Causes:**
1. Rigidbody.useGravity = false
2. Rigidbody.isKinematic = true
3. No collider to detect ground

**Solution:** Check console for "Added CapsuleCollider" log

### If Can't Hit Enemy
**Possible Causes:**
1. No collider on enemy
2. Collider is trigger (should be solid)
3. Throw velocity too low (< 1.5 m/s)

**Solution:** Throw harder, check collider exists

### If Enemy Dies from Static Objects
**Possible Causes:**
1. instakillMinVelocity too low
2. Objects on ground have high velocity

**Solution:** Verify instakillMinVelocity = 1.5 m/s

## Summary

The rigged enemy physics system now provides:
- ✅ Proper ground contact with gravity
- ✅ Full hittability with thrown objects
- ✅ Realistic physics interactions
- ✅ Stable upright posture
- ✅ Prevention of false combat triggers

Rigged enemies will spawn, fall to the ground, replay your movements while staying grounded, and can be defeated by throwing objects at them with sufficient force.
