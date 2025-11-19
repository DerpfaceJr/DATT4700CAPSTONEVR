# Script to fix Unity shader keyword mismatch error
# This clears corrupted shader cache that causes keyword mismatches

Write-Host "This script will clear Unity's cache to fix shader keyword errors." -ForegroundColor Yellow
Write-Host "Make sure Unity Editor is CLOSED before running this script!" -ForegroundColor Red
Write-Host ""
$confirm = Read-Host "Press Enter to continue (or Ctrl+C to cancel)"

$projectPath = $PSScriptRoot

# Folders to delete
$foldersToDelete = @(
    "Library",
    "Temp",
    "Logs"
)

Write-Host "`nClearing Unity cache folders..." -ForegroundColor Cyan

foreach ($folder in $foldersToDelete) {
    $fullPath = Join-Path $projectPath $folder
    if (Test-Path $fullPath) {
        Write-Host "  Deleting: $folder" -ForegroundColor Gray
        Remove-Item -Path $fullPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  ✓ Deleted: $folder" -ForegroundColor Green
    } else {
        Write-Host "  ⊗ Not found: $folder" -ForegroundColor Gray
    }
}

Write-Host "`n✓ Cache cleared successfully!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Open Unity Editor" -ForegroundColor White
Write-Host "2. Unity will automatically rebuild the Library folder" -ForegroundColor White
Write-Host "3. Wait for Unity to finish importing assets (may take a few minutes)" -ForegroundColor White
Write-Host "4. The shader keyword error should be resolved" -ForegroundColor White

