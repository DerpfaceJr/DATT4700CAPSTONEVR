# Enemy Projectile Troubleshooting Guide

## Issues Reported
1. ❌ No enemy projectiles shown at all
2. ❌ Sword doesn't kill rigged enemies

## Quick Diagnostic Steps

### Step 1: Add Diagnostic Script
I've created `ProjectileReplayDiagnostic.cs` - add it to any GameObject in scene:

1. Create empty GameObject: `GameObject > Create Empty`
2. Name it: "Diagnostic"
3. Add component: `ProjectileReplayDiagnostic`
4. Check Inspector for results

OR use Console Menu:
- Right-click ProjectileReplayDiagnostic component
- Select "Run Diagnostics"

### Step 2: Check Console During Play

**When you throw objects (Round 1), you should see:**
```
[VRGrabThrow] Released [object] with velocity: X.XX m/s
[VRGrabThrow] Tracking [object] for projectile replay
ProjectileThrowTracker: Started tracking [object]
ProjectileThrowTracker: Registered throw of [object] at velocity X.XX m/s
ProjectileThrowTracker: Added throw event to frame at time X.XX s
```

**If you DON'T see these messages**, throws aren't being recorded!

**When enemy spawns (Round 2), you should see:**
```
Enemy 2 (Rigged): Assigned Enemy Ball prefab for projectile replay
```

**When enemy replays throw:**
```
EnemyIKReplay: Enemy threw shadow projectile from (x, y, z) with velocity X.XX m/s
DisablePlayerGrabbing: Removed grab components from Enemy Ball (Shadow)
```

---

## Common Issues & Fixes

### Issue 1: Throws Not Being Recorded

**Symptoms:**
- No `[VRGrabThrow] Tracking` messages
- No `ProjectileThrowTracker: Registered throw` messages

**Causes:**
- ProjectileThrowTracker not in scene
- VRGrabThrow.Release() not calling TrackObject()
- Throw velocity < 2.0 m/s

**Fix:**
1. Verify ProjectileThrowTracker is on RoundManager ✅
2. Verify VRGrabThrow.cs has TrackObject() call ✅ (just added)
3. **Throw harder** - minimum 2.0 m/s required
4. Check: Is VRGrabThrow.Release() actually being called?

### Issue 2: Enemy Ball Prefab Not Assigned

**Symptoms:**
- Console error: "Enemy Ball prefab not assigned!"
- No projectiles spawn

**Causes:**
- EnemySpawner.enemyBallPrefab is null
- Assignment lost after scene reload

**Fix:**
```
Via Unity MCP (already done):
✅ EnemySpawner.enemyBallPrefab = "Assets/[BuildingBlock] Enemy Ball.prefab"
```

Verify in Inspector:
1. Select EnemySpawner GameObject
2. Check "Enemy Ball Prefab" field
3. Should show: [BuildingBlock] Enemy Ball

### Issue 3: No Movement Data Recorded

**Symptoms:**
- Enemies spawn but don't replay any movements
- No throw events because no movement data

**Causes:**
- PlayerMovementRecorder not recording
- RoundManager not saving movement data between rounds

**Fix:**
Check PlayerMovementRecorder is:
- Attached to Player GameObject
- Recording enabled
- Movement data being captured

### Issue 4: Sword Not Killing Rigged Enemies

**Symptoms:**
- Sword passes through enemies
- No damage/death on sword hit

**Causes:**
- CombatSystem not on rigged enemy ❓
- Sword not tagged properly ✅ (fixed)
- Sword velocity too low

**Fix:**
The EnemySpawner already adds CombatSystem to all enemies (line 252-266).

**Verify:**
1. Hit rigged enemy with sword
2. Check console for:
   - `[CombatSystem] SWORD KILL!` ← should see this
   - OR `[CombatSystem] Sword hit but velocity too low` ← swing harder!

**If no messages at all:**
- Rigged enemy doesn't have CombatSystem component
- Collider not working
- Sword not actually colliding

---

## Manual Verification Checklist

### Before Play Mode:
- [ ] ProjectileThrowTracker exists (on RoundManager) ✅
- [ ] PlayerHealth exists (on Player) ✅
- [ ] Sword tagged as "Sword" ✅
- [ ] EnemySpawner.enemyBallPrefab assigned ✅
- [ ] VRGrabThrow.cs has TrackObject() call ✅

### During Round 1:
- [ ] Pick up object successfully
- [ ] Release object
- [ ] See "[VRGrabThrow] Tracking..." in console
- [ ] See "ProjectileThrowTracker: Registered throw" in console
- [ ] Swing sword at static enemy
- [ ] See "[CombatSystem] SWORD KILL!" OR "velocity too low"

### During Round 2:
- [ ] Rigged enemy spawns
- [ ] See "Enemy 2 (Rigged): Assigned Enemy Ball prefab"
- [ ] Enemy replays your movements
- [ ] At throw moment: See "EnemyIKReplay: Enemy threw shadow projectile"
- [ ] Shadow ball appears visually
- [ ] Shadow ball cannot be grabbed
- [ ] Swing sword at rigged enemy
- [ ] See "[CombatSystem] SWORD KILL!"

---

## Debug Commands

### Check if throws are being recorded:
```csharp
// In console during/after Round 1:
PlayerMovementRecorder recorder = FindObjectOfType<PlayerMovementRecorder>();
if (recorder != null) {
    var data = recorder.GetMovementData();
    Debug.Log($"Recorded {data.Count} frames");
    int throws = 0;
    foreach (var frame in data) {
        if (frame.hasThrowEvent) throws++;
    }
    Debug.Log($"Throw events: {throws}");
}
```

### Check if Enemy Ball prefab is assigned:
```csharp
EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
Debug.Log($"Enemy Ball: {(spawner.enemyBallPrefab != null ? "Assigned" : "NULL")}");
```

### Check rigged enemy components:
```csharp
// Select rigged enemy in Hierarchy, then in console:
EnemyIKReplay replay = Selection.activeGameObject.GetComponent<EnemyIKReplay>();
Debug.Log($"Enemy Ball Prefab: {(replay.enemyBallPrefab != null ? "✓" : "✗")}");
CombatSystem combat = Selection.activeGameObject.GetComponent<CombatSystem>();
Debug.Log($"CombatSystem: {(combat != null ? "✓" : "✗")}");
```

---

## Most Likely Issues

### 1. Throws Not Being Recorded (80% probability)
**Test:** Check console for tracking messages when you release objects.
**Fix:** Already done - VRGrabThrow integration added. Need to test in Play Mode.

### 2. Low Throw Velocity (15% probability)
**Test:** Throw HARDER - minimum 2.0 m/s required.
**Fix:** Increase throw force or throw with more motion.

### 3. Sword Not Colliding with Rigged Enemies (5% probability)
**Test:** Swing sword, check for any combat messages in console.
**Fix:** Ensure rigged enemies have colliders and CombatSystem (should be auto-added).

---

## Next Steps

1. **Enter Play Mode**
2. **Watch Console Carefully**
3. **Throw objects** - look for tracking messages
4. **Swing sword** - look for combat messages
5. **Report which messages you see/don't see**

The diagnostic output will tell us exactly what's missing!
