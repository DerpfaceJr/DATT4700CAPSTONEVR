# Fixing Unity Shader Keyword Mismatch Error

## Problem
You're seeing this error:
```
Keyword state size mismatch (76 vs 66 keywords)
KeywordSpace: From shader: 'Meta/Lit'
```

This happens when Unity's shader cache becomes corrupted or out of sync.

## Solution

### Option 1: Quick Fix (Recommended)
1. **Close Unity Editor completely**
2. Run the PowerShell script: `.\fix_shader_cache.ps1`
3. Reopen Unity - it will rebuild the cache automatically

### Option 2: Manual Fix
1. **Close Unity Editor completely**
2. Delete these folders manually:
   - `Library/`
   - `Temp/`
   - `Logs/`
3. Reopen Unity Editor
4. Wait for Unity to reimport all assets (this may take several minutes)

### Option 3: Reimport Shaders Only
If you want to keep other cached data:
1. In Unity: **Assets > Reimport All**
2. Or delete only: `Library/ShaderCache/`

## Why This Happens
- Unity version updates
- URP package updates
- Corrupted shader compilation cache
- Switching between different Unity/URP versions

## Prevention
- Keep Unity and packages updated consistently
- Don't manually edit files in the `Library/` folder
- Use version control (git) to track project changes

## Additional Notes
- The `Library/` folder is already in `.gitignore` (as it should be)
- Rebuilding the cache will take time but is necessary
- Your project assets are safe - only cache is being cleared

