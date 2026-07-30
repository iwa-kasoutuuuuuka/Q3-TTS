# Q3-TTS Model Download PowerShell Script
param (
    [string]$Size = "1.7B"
)

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "     Q3-TTS Qwen3-TTS Model Downloader           " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

$ModelsDir = Join-Path $PSScriptRoot "models"
if (!(Test-Path $ModelsDir)) {
    New-Item -ItemType Directory -Path $ModelsDir -Force | Out-Null
}

$RepoId = if ($Size -eq "1.7B") { "Qwen/Qwen3-TTS-12Hz-1.7B-CustomVoice" } else { "Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice" }
$SubFolder = if ($Size -eq "1.7B") { "qwen3-1.7b" } else { "qwen3-0.6b" }
$TargetPath = Join-Path $ModelsDir $SubFolder

Write-Host "Target Model : $RepoId" -ForegroundColor Yellow
Write-Host "Save Path    : $TargetPath" -ForegroundColor Yellow

# Try running python download script via uv or python if available
if (Get-Command "uv" -ErrorAction SilentlyContinue) {
    Write-Host "Using uv to run python downloader..." -ForegroundColor Green
    uv run python download_models.py --size $Size
} elseif (Get-Command "python" -ErrorAction SilentlyContinue) {
    Write-Host "Using system python to run downloader..." -ForegroundColor Green
    python download_models.py --size $Size
} else {
    Write-Host "Python environment not detected. Creating placeholder structure..." -ForegroundColor Magenta
    New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null
}

Write-Host "Download script execution finished." -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Cyan
