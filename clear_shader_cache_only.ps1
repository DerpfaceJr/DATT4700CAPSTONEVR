# Script to clear only Unity's shader cache
# This is safer than clearing the entire Library folder
# Make sure Unity Editor is CLOSED before running this script!

Write-Host "This script will clear Unity's shader cache to fix shader compilation errors." -ForegroundColor Yellow
Write-Host "Make sure Unity Editor is CLOSED before running this script!" -ForegroundColor Red
Write-Host ""

$projectPath = $PSScriptRoot

# Check if Unity is running
$unityProcesses = Get-Process -Name "Unity*" -ErrorAction SilentlyContinue
if ($unityProcesses) {
    Write-Host "ERROR: Unity Editor is still running!" -ForegroundColor Red
    Write-Host "Please close Unity Editor completely before running this script." -ForegroundColor Red
    Write-Host ""
    Write-Host "Running Unity processes:" -ForegroundColor Yellow
    $unityProcesses | ForEach-Object { Write-Host "  - $($_.ProcessName) (PID: $($_.Id))" -ForegroundColor Gray }
    exit 1
}

Write-Host "Unity Editor is not running. Safe to proceed." -ForegroundColor Green
Write-Host ""

$confirm = Read-Host "Press Enter to clear shader cache (or Ctrl+C to cancel)"

# Clear shader cache
$shaderCachePath = Join-Path $projectPath "Library\ShaderCache"
$shaderCacheDbPath = Join-Path $projectPath "Library\ShaderCache.db"

Write-Host "`nClearing shader cache..." -ForegroundColor Cyan

if (Test-Path $shaderCachePath) {
    Write-Host "  Deleting: Library\ShaderCache" -ForegroundColor Gray
    Remove-Item -Path $shaderCachePath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  ✓ Deleted: Library\ShaderCache" -ForegroundColor Green
} else {
    Write-Host "  ⊗ Not found: Library\ShaderCache" -ForegroundColor Gray
}

if (Test-Path $shaderCacheDbPath) {
    Write-Host "  Deleting: Library\ShaderCache.db" -ForegroundColor Gray
    Remove-Item -Path $shaderCacheDbPath -Force -ErrorAction SilentlyContinue
    Write-Host "  ✓ Deleted: Library\ShaderCache.db" -ForegroundColor Green
} else {
    Write-Host "  ⊗ Not found: Library\ShaderCache.db" -ForegroundColor Gray
}

Write-Host "`n✓ Shader cache cleared successfully!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Open Unity Editor" -ForegroundColor White
Write-Host "2. Unity will automatically rebuild the shader cache" -ForegroundColor White
Write-Host "3. Wait for Unity to finish compiling shaders (may take a few minutes)" -ForegroundColor White
Write-Host "4. Check the Console for any remaining shader errors" -ForegroundColor White

