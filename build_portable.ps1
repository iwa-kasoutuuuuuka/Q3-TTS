# Q3-TTS Portable Package Build Script
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "   Q3-TTS (Qwen3-TTS US Edition) Portable Build  " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

$PublishDir = Join-Path $PSScriptRoot "Release_Portable_Q3TTS_CUDA"

if (Test-Path $PublishDir) {
    Remove-Item -Path $PublishDir -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Publishing portable WPF Application (win-x64)..." -ForegroundColor Yellow
dotnet publish "Q3TTS.Native.csproj" -c Release -r win-x64 --self-contained false -o $PublishDir

Write-Host "Copying assets, user dictionary, and scripts..." -ForegroundColor Yellow
Copy-Item (Join-Path $PSScriptRoot "user_dict_en.txt") $PublishDir -Force
Copy-Item (Join-Path $PSScriptRoot "sample_sentences_en.txt") $PublishDir -Force
Copy-Item (Join-Path $PSScriptRoot "download_models.py") $PublishDir -Force
Copy-Item (Join-Path $PSScriptRoot "download_models.ps1") $PublishDir -Force

$AssetsDest = Join-Path $PublishDir "assets"
if (!(Test-Path $AssetsDest)) {
    New-Item -ItemType Directory -Path $AssetsDest -Force | Out-Null
}
Copy-Item (Join-Path $PSScriptRoot "assets\*") $AssetsDest -Force

Write-Host "=================================================" -ForegroundColor Green
Write-Host "Portable package created at: $PublishDir" -ForegroundColor Green
Write-Host "Main Executable: Q3TTS.Native.exe" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
