# Enemy Spawn Fix - Round 2 Spawning Too Many Enemies

## Problem
Round 2 was sometimes spawning 4 enemies instead of 2. The issue was that `currentEnemies` in `RoundManager` could be out of sync with actual enemies in the scene.

## Root Cause
- `RoundManager` maintains its own `currentEnemies` list
- `EnemySpawner` also maintains an `activeEnemies` list
- These lists could become desynchronized, causing incorrect spawn counts
- When Round 2 started, if `currentEnemies.Count` was 0 (but actual enemies existed), it would spawn `targetEnemyCount - 0 = 2` NEW enemies, plus any existing enemies

## Solution
Added synchronization logic in `RoundManager.StartRound()` for Round 2+:

```csharp
else
{
    // SYNC FIX: For Round 2+, sync our enemy list with EnemySpawner's actual list
    // This prevents spawning too many enemies if lists are out of sync
    if (enemySpawner != null)
    {
        List<GameObject> actualEnemies = enemySpawner.GetActiveEnemies();
        currentEnemies = actualEnemies;
        
        if (debugMode)
        {
            Debug.Log($"Round {currentRound}: Synced with EnemySpawner. Actual enemies in scene: {currentEnemies.Count}");
        }
    }
}
```

## How It Works Now

### Round 1
1. Clears all enemies
2. Sets `currentEnemies` to empty
3. Spawns 1 static enemy
4. Total: 1 enemy

### Round 2
1. **SYNCS** `currentEnemies` with `EnemySpawner.GetActiveEnemies()`
2. Checks actual enemy count (should be 1 if Round 1 enemy survived)
3. Calculates: `enemiesToSpawn = 2 - 1 = 1`
4. Spawns exactly 1 new enemy
5. Total: 2 enemies

### Round 3
1. **SYNCS** `currentEnemies` with `EnemySpawner.GetActiveEnemies()`
2. Checks actual enemy count (should be 2 if previous enemies survived)
3. Calculates: `enemiesToSpawn = 3 - 2 = 1`
4. Spawns exactly 1 new enemy
5. Total: 3 enemies

## Testing
1. **Round 1**: Should spawn exactly 1 static enemy
2. **Round 2**: Should have exactly 2 enemies total (1 from Round 1 + 1 new)
3. **Round 3**: Should have exactly 3 enemies total (2 previous + 1 new)
4. **Round N**: Should have exactly N enemies total

## Debug Logs
The fix adds a debug log for Round 2+:
```
Round {N}: Synced with EnemySpawner. Actual enemies in scene: {count}
```

This helps verify the synchronization is working correctly.

## Files Modified
- `Assets/Scripts/RoundManager.cs` - Added synchronization logic in `StartRound()` method

## Verification
- ✅ No compilation errors
- ✅ RoundManager GameObject properly configured in scene
- ✅ EnemySpawner references valid
- ✅ All component references assigned
