$RootPath = Resolve-Path "$PSScriptRoot\.."
$DeploymentPath = Join-Path $RootPath "Deployment"
$SquashPublishPath = Join-Path $RootPath "Squash\bin\Publish"
$ExtraPath = Join-Path $DeploymentPath "extra"
$OutPath = Join-Path $DeploymentPath "out"
$ZipFile = Join-Path $OutPath "squash-payload.zip"

Write-Host "--- Starting Deployment Process ---" -ForegroundColor Cyan

Write-Host "[1/3] Creating squash-payload.zip..." -ForegroundColor Yellow

if (-not (Test-Path $OutPath)) {
    New-Item -ItemType Directory -Path $OutPath | Out-Null
}

if (Test-Path $ZipFile) {
    Remove-Item $ZipFile -Force
}

$TempPayload = Join-Path $env:TEMP "SquashPayload_$(Get-Random)"
New-Item -ItemType Directory -Path $TempPayload | Out-Null

Write-Host "  Copying files to temporary staging area..." -ForegroundColor Gray
Copy-Item -Path "$SquashPublishPath\*" -Destination $TempPayload -Recurse
if (Test-Path $ExtraPath) {
    Copy-Item -Path "$ExtraPath\*" -Destination $TempPayload -Recurse
}

Write-Host "  Compressing files..." -ForegroundColor Gray
Compress-Archive -Path "$TempPayload\*" -DestinationPath $ZipFile

Remove-Item $TempPayload -Recurse -Force

Write-Host "[2/3] Running stamper..." -ForegroundColor Yellow
Push-Location $DeploymentPath
try {
    .\stamper.exe stamp --input updater.exe --config config.json
} catch {
    Write-Error "Stamper failed: $_"
    Pop-Location
    exit 1
}
Pop-Location

Write-Host "[3/3] Compiling setup.iss with ISCC..." -ForegroundColor Yellow
try {
    $SquashDll = Join-Path $SquashPublishPath "Squash.dll"
    $Version = (Get-Item $SquashDll).VersionInfo.FileVersion
    Write-Host "  Detected Version: $Version" -ForegroundColor Gray
    iscc.exe "/dMyAppVersion=$Version" "$DeploymentPath\setup.iss"
} catch {
    Write-Error "ISCC failed: $_"
    exit 1
}

Write-Host "--- Deployment Process Complete ---" -ForegroundColor Green
