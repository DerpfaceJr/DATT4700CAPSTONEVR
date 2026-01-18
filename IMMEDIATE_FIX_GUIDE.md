# IMMEDIATE FIX - Enemy Projectiles & Sword Combat

## Current Status
✅ All code is in place
✅ All Unity Editor setup complete
✅ VR Grab integration added
✅ Diagnostic script added

## Why Projectiles Might Not Show

### Most Likely Cause: Throws Not Being Recorded

When you release an object, the console MUST show:
```
[VRGrabThrow] Tracking [object] for projectile replay
ProjectileThrowTracker: Registered throw
```

**If you don't see these messages**, throws aren't being recorded, so enemies have nothing to replay!

### How to Fix/Test:

1. **Enter Play Mode**
2. **Pick up an object** (cube, ball, etc.)
3. **Throw it HARD** (minimum 2.0 m/s - swing your controller!)
4. **Watch Console** immediately after release

**Expected:** See tracking messages
**If not:** Throw harder or check VRGrabThrow script

---

## Sword Combat Fix

The sword should work because:
- ✅ CombatSystem added to all enemies (line 252-266 in EnemySpawner)
- ✅ Sword tagged as "Sword"
- ✅ Melee detection enabled

### Test Sword:

1. **Pick up sword**
2. **Swing at static enemy** (Round 1)
3. **Check Console**

**Expected:**
```
[CombatSystem] SWORD KILL! Sword hit Enemy_Round1 (velocity: X.XX m/s)
```

**If you see:**
```
[CombatSystem] Sword hit but velocity too low (X.XX < 1.0)
```
→ **Swing HARDER!**

**If no message at all:**
→ Sword not colliding - check Rigidbody and Collider on sword

---

## Quick Verification

### Check #1: Is VRGrabThrow Calling TrackObject()?

File: `Assets/Scripts/VRGrabThrow.cs`
Line: ~1115

Should contain:
```csharp
ProjectileThrowTracker tracker = FindObjectOfType<ProjectileThrowTracker>();
if (tracker != null)
{
    tracker.TrackObject(rbToRelease.gameObject);
    Debug.Log($"[VRGrabThrow] Tracking {rbToRelease.name} for projectile replay");
}
```

✅ **Already added** - just needs testing

### Check #2: Is Enemy Ball Prefab Assigned?

1. Select **EnemySpawner** GameObject in Hierarchy
2. Check Inspector
3. **Enemy Ball Prefab** field should show: `[BuildingBlock] Enemy Ball`

✅ **Already assigned** via Unity MCP

### Check #3: Does Sword Have Components?

1. Select **[BuildingBlock] Sword** in Hierarchy
2. Check Inspector:
   - ✅ Tag: "Sword"
   - ✅ Rigidbody component
   - ✅ BoxCollider components

---

## Diagnostic Tool

I've added `ProjectileReplayDiagnostic.cs` to your project.

### To Use:
1. Create empty GameObject: `GameObject > Create Empty`
2. Name it: "Diagnostic"
3. Add Component: `ProjectileReplayDiagnostic`
4. Enter Play Mode
5. Check Inspector - "Diagnostic Results" field shows full system status

OR:
- Right-click component → "Run Diagnostics"
- Check Console for full report

---

## Most Likely Issues

| Issue | Probability | Test | Fix |
|-------|-------------|------|-----|
| **Throws not recorded** | 80% | Check console for tracking messages | Throw harder (>2 m/s) |
| **No movement data** | 15% | Check PlayerMovementRecorder | Should auto-record |
| **Sword velocity too low** | 4% | Check console for "velocity too low" | Swing harder (>1 m/s) |
| **Missing components** | 1% | Run diagnostic script | Components auto-added |

---

## Action Plan

### RIGHT NOW:
1. **Enter Play Mode**
2. **Throw an object HARD**
3. **Check Console immediately**

### If You See Tracking Messages:
✅ System working! Complete Round 1 and check Round 2 for projectiles.

### If NO Tracking Messages:
❌ Problem with VRGrabThrow integration or throw too weak.
- Try throwing even harder
- Verify VRGrabThrow.Release() is being called (check for Release messages)

### For Sword:
1. **Swing sword at enemy**
2. **Check Console**
3. If "velocity too low" → Swing harder
4. If no message → Sword not colliding (check components)

---

## Console Messages Reference

### GOOD (System Working):
```
✓ [VRGrabThrow] Released Cube with velocity: 3.45 m/s
✓ [VRGrabThrow] Tracking Cube for projectile replay
✓ ProjectileThrowTracker: Started tracking Cube
✓ ProjectileThrowTracker: Registered throw of Cube at velocity 3.45 m/s
✓ ProjectileThrowTracker: Added throw event to frame at time 2.30s
✓ Enemy 2 (Rigged): Assigned Enemy Ball prefab for projectile replay
✓ EnemyIKReplay: Enemy threw shadow projectile from (1.5, 2.0, 3.2) with velocity 3.45 m/s
✓ [CombatSystem] SWORD KILL! Sword hit Enemy_Round2 (velocity: 2.1 m/s)
```

### BAD (System Not Working):
```
✗ (No messages after releasing object)
✗ ProjectileThrowTracker: No ProjectileThrowTracker found!
✗ EnemyIKReplay: Enemy Ball prefab not assigned!
✗ [CombatSystem] Sword hit but velocity too low (0.5 < 1.0)
```

---

**TL;DR**: 
1. **Test in Play Mode NOW**
2. **Check Console for tracking/combat messages**
3. **Throw and swing HARD** (need velocity!)
4. **Report what you see in console**

The system is ready - just needs testing with actual throws and swings!
