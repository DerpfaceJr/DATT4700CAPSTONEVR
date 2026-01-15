# Throw Trajectory Calculation Analysis

## Overview
The project uses **three different systems** for calculating throw trajectories, depending on which grabbing system is used.

---

## System 1: VRGrabThrow.cs (Custom Controller-Based Throwing)

### Location
`Assets/Scripts/VRGrabThrow.cs`

### How It Calculates Trajectory

#### Step 1: Velocity Tracking (Lines 213-227)
```csharp
// Every frame, calculate frame velocity
Vector3 frameVelocity = (transform.position - previousPosition) / Time.deltaTime;

// Store in circular buffer (5 frames)
velocityHistory[velocityIndex] = frameVelocity;
velocityIndex = (velocityIndex + 1) % velocityHistory.Length;

// Calculate smoothed average velocity
Vector3 avgVelocity = Vector3.zero;
for (int i = 0; i < velocityHistory.Length; i++) {
    avgVelocity += velocityHistory[i];
}
currentVelocity = avgVelocity / velocityHistory.Length;
```

**Key Points:**
- Tracks **controller movement** (not object movement)
- Uses **5-frame rolling average** for smoothness
- Calculates velocity as: `(currentPos - previousPos) / deltaTime`

#### Step 2: Throw Velocity Calculation (Line 908)
```csharp
Vector3 throwVelocity = currentVelocity * throwForce;
```

**Formula:**
```
throwVelocity = (averaged controller velocity) × throwForce
```

#### Step 3: Apply to Rigidbody (Lines 911-915)
```csharp
#if UNITY_2023_1_OR_NEWER
    rbToRelease.linearVelocity = throwVelocity;
#else
    rbToRelease.velocity = throwVelocity;
#endif
```

#### Step 4: Angular Velocity (Spin) (Lines 917-927)
```csharp
// Calculate spin axis perpendicular to throw direction
Vector3 spinAxis = Vector3.Cross(transform.forward, throwVelocity.normalized);
if (spinAxis.magnitude > 0.1f) {
    rbToRelease.angularVelocity = spinAxis.normalized * throwVelocity.magnitude * 0.5f;
} else {
    // Random spin if no clear axis
    rbToRelease.angularVelocity = Random.insideUnitSphere * Mathf.Min(throwVelocity.magnitude * 0.3f, 10f);
}
```

### Physics Setup
- **Gravity**: Enabled on Rigidbody (`useGravity = true`)
- **Kinematic**: Object is kinematic while grabbed, becomes dynamic on release
- **Result**: **Parabolic trajectory** due to gravity

### Trajectory Formula
```
Position(t) = initialPosition + (throwVelocity × t) + (0.5 × gravity × t²)
```

Where:
- `throwVelocity` = averaged controller velocity × throwForce
- `gravity` = Unity's Physics.gravity (typically -9.81 m/s² on Y-axis)
- `t` = time since release

---

## System 2: ConfigureBallLikeCube.cs (Oculus Interaction Compatible)

### Location
`Assets/Scripts/ConfigureBallLikeCube.cs`

### How It Calculates Trajectory

#### Step 1: Velocity Tracking While Grabbed (Lines 175-180)
```csharp
// Track velocity while object is kinematic (being grabbed)
if (rb.isKinematic) {
    throwVelocity = (transform.position - lastPosition) / Time.deltaTime;
}
lastPosition = transform.position;
```

**Key Points:**
- Tracks **object movement** (not controller movement)
- Only tracks when object is kinematic (being grabbed by Oculus system)
- Calculates velocity as: `(currentPos - lastPos) / deltaTime`

#### Step 2: Detect Release (Lines 167-170)
```csharp
// When released, apply throw velocity
if (wasGrabbed && !isGrabbed) {
    ApplyThrowVelocity();
}
```

#### Step 3: Apply Throw Velocity (Lines 185-191)
```csharp
void ApplyThrowVelocity() {
    if (rb == null || rb.isKinematic) return;
    
    // Apply throw velocity with multiplier
    Vector3 finalVelocity = throwVelocity * throwVelocityMultiplier;
    rb.linearVelocity = finalVelocity;
    
    // Add random angular velocity
    rb.angularVelocity = new Vector3(
        Random.Range(-5f, 5f),
        Random.Range(-5f, 5f),
        Random.Range(-5f, 5f)
    );
}
```

**Formula:**
```
finalVelocity = (object movement velocity while grabbed) × throwVelocityMultiplier
```

### Physics Setup
- **Gravity**: Enabled (`rb.useGravity = true`)
- **Kinematic**: Object becomes kinematic when grabbed, dynamic when released
- **Collision Detection**: Continuous mode for fast-moving objects
- **Result**: **Parabolic trajectory** due to gravity

### Trajectory Formula
```
Position(t) = initialPosition + (finalVelocity × t) + (0.5 × gravity × t²)
```

Where:
- `finalVelocity` = object movement velocity × throwVelocityMultiplier (default 1.5)
- `gravity` = Unity's Physics.gravity

---

## System 3: Oculus Interaction Grabbable (Built-in)

### Location
Oculus Interaction SDK (external library)

### How It Calculates Trajectory

This system is **handled internally by Oculus Interaction SDK**. The project configures it via reflection:

```csharp
// Enable throw when unselected (ConfigureBallLikeCube.cs line 121-125)
var throwField = grabbable.GetType().GetField("_throwWhenUnselected", 
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
if (throwField != null) {
    throwField.SetValue(grabbable, true);
}
```

**How It Works:**
- Oculus SDK tracks hand/controller movement
- Calculates velocity based on hand tracking data
- Applies velocity automatically when object is released
- Uses internal algorithms for smooth, natural throwing

**Note:** The exact calculation is proprietary to Oculus SDK, but it typically:
- Tracks hand position/velocity
- Applies smoothing/filtering
- Calculates throw velocity based on release motion
- Applies to Rigidbody automatically

---

## Comparison Table

| System | Velocity Source | Calculation Method | Multiplier | Smoothing |
|--------|---------------|-------------------|------------|-----------|
| **VRGrabThrow** | Controller movement | 5-frame average | `throwForce` (default 1.0) | Rolling average |
| **ConfigureBallLikeCube** | Object movement | Single frame delta | `throwVelocityMultiplier` (default 1.5) | None |
| **Oculus Grabbable** | Hand tracking | Internal SDK | Internal | Internal smoothing |

---

## Trajectory Physics

All three systems result in the **same physics trajectory** because:

1. **Initial Velocity**: Applied via `Rigidbody.linearVelocity`
2. **Gravity**: Enabled on all Rigidbodies (`useGravity = true`)
3. **Physics Engine**: Unity's physics engine handles the parabolic arc

### Trajectory Equation
```
y(t) = y₀ + v₀y·t - ½·g·t²    (vertical)
x(t) = x₀ + v₀x·t              (horizontal)
z(t) = z₀ + v₀z·t              (depth)
```

Where:
- `v₀` = initial velocity vector (from throw calculation)
- `g` = gravity (9.81 m/s² downward)
- `t` = time since release

---

## Key Differences

### VRGrabThrow
- ✅ **Smoother**: 5-frame averaging reduces jitter
- ✅ **Controller-based**: Uses controller movement directly
- ✅ **Customizable**: Easy to modify throw force
- ❌ **No filtering**: All controller movements affect throw (including side movements)

### ConfigureBallLikeCube
- ✅ **Simple**: Direct velocity calculation
- ✅ **Oculus compatible**: Works with Oculus Interaction system
- ✅ **Multiplier**: Built-in velocity boost (1.5x)
- ❌ **No smoothing**: Single-frame calculation can be jittery
- ❌ **Object-based**: Tracks object, not controller (less accurate)

### Oculus Grabbable
- ✅ **Professional**: Industry-standard hand tracking
- ✅ **Optimized**: Internal smoothing and filtering
- ✅ **Natural feel**: Designed for VR hand interactions
- ❌ **Black box**: Can't easily customize calculation
- ❌ **SDK dependent**: Requires Oculus SDK

---

## Current Implementation Status

Based on code analysis:

1. **VRGrabThrow.cs** (Line 908): Uses simple `currentVelocity * throwForce`
   - **No forward filtering** (user's previous request was reverted)
   - All controller movements affect throw direction

2. **ConfigureBallLikeCube.cs** (Line 190): Uses `throwVelocity * throwVelocityMultiplier`
   - Tracks object movement, not controller
   - Applies 1.5x multiplier

3. **Oculus Grabbable**: Handled by SDK
   - Automatic when `_throwWhenUnselected = true`

---

## Recommendations

### For Forward-Only Throwing
The current implementation does **NOT** filter side movements. To achieve forward-only throwing:

1. **Option A**: Re-implement forward throw filtering in `VRGrabThrow.cs` Release() method
2. **Option B**: Use Oculus Grabbable system (handles this better)
3. **Option C**: Modify `ConfigureBallLikeCube.cs` to project velocity onto forward direction

### For Better Trajectory Control
- Increase `throwForce` in VRGrabThrow for stronger throws
- Adjust `throwVelocityMultiplier` in ConfigureBallLikeCube (default 1.5)
- Use Oculus Grabbable for most natural feel

---

## Debugging

### VRGrabThrow Debug Logs
```csharp
Debug.Log($"[VRGrabThrow] Released {rbToRelease.name} with velocity: {throwVelocity.magnitude:F2} m/s");
```

### ConfigureBallLikeCube Debug Logs
```csharp
Debug.Log($"[ConfigureBallLikeCube] Applied throw velocity to {gameObject.name}: {finalVelocity.magnitude:F2} m/s");
```

### Enable Debug Mode
Set `debugMode = true` on VRGrabThrow component to see detailed velocity information.
