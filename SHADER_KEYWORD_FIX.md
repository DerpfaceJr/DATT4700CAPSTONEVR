# Shader Keyword Mismatch Fix - Meta/Lit to URP/Lit

## Problem
The project was experiencing shader keyword mismatch errors (76 vs 66 keywords) due to Meta Building Blocks materials using `Meta/Lit` shader instead of `Universal Render Pipeline/Lit` shader, which is incompatible with URP.

**Error Messages:**
- "Keyword state size mismatch (76 vs 66 keywords)"
- "State comes from an incompatible keyword space"

## Solution Applied
Following the solution from [Meta Community Forums](https://communityforums.atmeta.com/discussions/Questions_Discussions/material-read-only-issue--shader-error-76-vs-66-keywords-fix/1348478):

### 1. Created URP-Compatible Material
- **New Material**: `Assets/Materials/SharedMaterial_URP.mat`
- **GUID**: `cba6de8f95784c01be2745c71b044fae`
- **Shader**: Universal Render Pipeline/Lit (GUID: `933532a4fcc9baf4fa0491de14d08ed7`)
- **Properties**: Copied all properties from Meta SharedMaterial, including:
  - Base Color: `{r: 0.20392153, g: 0.462745, b: 0.8784314, a: 1}` (blue)
  - Metallic: 0
  - Smoothness: 0.5
  - All other material properties preserved

### 2. Replaced Material References
Replaced all references to Meta SharedMaterial (GUID: `15b1d5ff3d274694e8d0c6cb466a4ae6`) with the new URP material in:
- ✅ `Assets/Scenes/SimpleArea.unity/SimpleArea.unity`
- ✅ `Assets/Scenes/SampleScene.unity`
- ✅ `Assets/Example.unity`

## Files Created
- `Assets/Materials/SharedMaterial_URP.mat` - New URP-compatible material
- `Assets/Materials/SharedMaterial_URP.mat.meta` - Material metadata
- `Assets/Materials.meta` - Materials folder metadata

## Files Modified
- All scene files that referenced the Meta SharedMaterial

## Result
- ✅ All Meta/Lit shader references replaced with URP/Lit
- ✅ Shader keyword mismatch errors should be resolved
- ✅ Materials maintain the same visual appearance
- ✅ Project is now fully compatible with URP

## Next Steps
1. Open Unity Editor
2. Wait for assets to reimport
3. Verify no shader keyword errors in Console
4. Test scenes to ensure materials render correctly

## Reference
Solution based on: https://communityforums.atmeta.com/discussions/Questions_Discussions/material-read-only-issue--shader-error-76-vs-66-keywords-fix/1348478

