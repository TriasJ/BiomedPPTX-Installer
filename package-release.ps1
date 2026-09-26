# BiomedPPTX Release Packaging Script
# Creates a distributable release package with all required files
#
# Usage: .\package-release.ps1 [-OutputDir <path>]
#
# Prerequisites:
# 1. Build BiomedPPTX in Visual Studio (Debug or Release)
# 2. SMART-Library assets at ..\SMART-Library\ and ..\SMART-Lib\
# 3. BioArt index at ~\.claude\skills\fetch-media\bioart_index.json
# 4. Build this installer project in Visual Studio

param(
    [string]$OutputDir = ".\Release"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ScratchDir = Split-Path -Parent $ScriptDir

Write-Host "=== BiomedPPTX Release Packager ===" -ForegroundColor Cyan
Write-Host ""

# Create output directory
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDir | Out-Null
Write-Host "Output: $OutputDir"

# --- 1. Copy installer executable ---
$installerExe = Join-Path $ScriptDir "PowerPointLabsInstaller\PowerPointLabsInstallerUi\bin\Release\BiomedPPTXInstaller.exe"
if (-not (Test-Path $installerExe)) {
    $installerExe = Join-Path $ScriptDir "PowerPointLabsInstaller\PowerPointLabsInstallerUi\bin\Debug\BiomedPPTXInstaller.exe"
}
if (Test-Path $installerExe) {
    Copy-Item $installerExe (Join-Path $OutputDir "BiomedPPTXInstaller.exe")
    Write-Host "[OK] Installer executable" -ForegroundColor Green
} else {
    Write-Host "[SKIP] Installer exe not found - build the installer project first" -ForegroundColor Yellow
}

# --- 2. Create smart-assets.zip ---
$smartLibrary = Join-Path $ScratchDir "SMART-Library"
$smartLib = Join-Path $ScratchDir "SMART-Lib"
$smartAssetsZip = Join-Path $OutputDir "smart-assets.zip"

if ((Test-Path $smartLibrary) -and (Test-Path $smartLib)) {
    Write-Host "Packaging SMART-Library assets (this may take a while)..."

    # Create temp staging directory
    $staging = Join-Path $env:TEMP "BiomedPPTX-staging"
    if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
    New-Item -ItemType Directory -Path "$staging\SMART-Library" | Out-Null
    New-Item -ItemType Directory -Path "$staging\SMART-Lib" | Out-Null

    # Copy database
    Copy-Item (Join-Path $smartLibrary "illustrations.db") "$staging\SMART-Library\" -Force
    Write-Host "  - illustrations.db" -ForegroundColor Gray

    # Copy PNG thumbnails
    if (Test-Path (Join-Path $smartLibrary "png")) {
        Write-Host "  - PNG thumbnails (copying)..." -ForegroundColor Gray
        Copy-Item (Join-Path $smartLibrary "png") "$staging\SMART-Library\png" -Recurse -Force
    }

    # Copy source PPTX files
    Write-Host "  - Source PPTX files (49 files)..." -ForegroundColor Gray
    Get-ChildItem $smartLib -Filter "*.pptx" | ForEach-Object {
        Copy-Item $_.FullName "$staging\SMART-Lib\" -Force
    }

    # Create zip
    Write-Host "  - Compressing..." -ForegroundColor Gray
    if (Test-Path $smartAssetsZip) { Remove-Item $smartAssetsZip -Force }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($staging, $smartAssetsZip, [System.IO.Compression.CompressionLevel]::Optimal, $false)

    $sizeMB = [math]::Round((Get-Item $smartAssetsZip).Length / 1MB, 1)
    Write-Host "[OK] smart-assets.zip ($sizeMB MB)" -ForegroundColor Green

    # Cleanup staging
    Remove-Item $staging -Recurse -Force
} else {
    Write-Host "[SKIP] SMART-Library not found at $smartLibrary" -ForegroundColor Yellow
}

# --- 3. Copy bioart_index.json ---
$bioartPaths = @(
    (Join-Path $env:USERPROFILE ".claude\skills\fetch-media\bioart_index.json"),
    (Join-Path $ScratchDir "SMART-Library\bioart_index.json")
)
$bioartCopied = $false
foreach ($bioartPath in $bioartPaths) {
    if (Test-Path $bioartPath) {
        Copy-Item $bioartPath (Join-Path $OutputDir "bioart_index.json") -Force
        Write-Host "[OK] bioart_index.json" -ForegroundColor Green
        $bioartCopied = $true
        break
    }
}
if (-not $bioartCopied) {
    Write-Host "[SKIP] bioart_index.json not found" -ForegroundColor Yellow
}

# --- 4. Copy Tutorial.pptx ---
$tutorialPath = Join-Path $ScratchDir "BiomedPPTX\doc\Tutorial.pptx"
if (Test-Path $tutorialPath) {
    Copy-Item $tutorialPath (Join-Path $OutputDir "Tutorial.pptx") -Force
    Write-Host "[OK] Tutorial.pptx" -ForegroundColor Green
} else {
    Write-Host "[SKIP] Tutorial.pptx not found at $tutorialPath" -ForegroundColor Yellow
}

# --- 5. Summary ---
Write-Host ""
Write-Host "=== Package Contents ===" -ForegroundColor Cyan
Get-ChildItem $OutputDir | ForEach-Object {
    $sizeMB = [math]::Round($_.Length / 1MB, 1)
    Write-Host ("  {0,-30} {1,8} MB" -f $_.Name, $sizeMB)
}

$totalMB = [math]::Round((Get-ChildItem $OutputDir | Measure-Object -Property Length -Sum).Sum / 1MB, 1)
Write-Host ""
Write-Host "Total: $totalMB MB" -ForegroundColor Cyan
Write-Host ""
Write-Host "To distribute: zip the contents of $OutputDir and upload to GitHub Releases" -ForegroundColor Yellow
