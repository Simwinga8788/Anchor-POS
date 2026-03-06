# Anchor POS - Build and Package Script
# This script builds the application and creates the installer

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Anchor POS - Build & Package Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean previous builds
Write-Host "[1/5] Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --configuration Release
if (Test-Path "src\AnchorPOS.Desktop\bin\Release") {
    Remove-Item "src\AnchorPOS.Desktop\bin\Release" -Recurse -Force
}
if (Test-Path "installer_output") {
    Remove-Item "installer_output" -Recurse -Force
}
Write-Host "✓ Clean complete" -ForegroundColor Green
Write-Host ""

# Step 2: Restore NuGet packages
Write-Host "[2/5] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
Write-Host "✓ Restore complete" -ForegroundColor Green
Write-Host ""

# Step 3: Build the application
Write-Host "[3/5] Building application (Release mode)..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build complete" -ForegroundColor Green
Write-Host ""

# Step 4: Publish self-contained executable
Write-Host "[4/5] Publishing self-contained application..." -ForegroundColor Yellow
dotnet publish src\AnchorPOS.Desktop\AnchorPOS.Desktop.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output "src\AnchorPOS.Desktop\bin\Release\net8.0-windows\win-x64\publish" `
    /p:PublishSingleFile=false `
    /p:PublishTrimmed=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Publish failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Publish complete" -ForegroundColor Green
Write-Host ""

# Step 5: Create installer (if Inno Setup is installed)
Write-Host "[5/5] Creating installer..." -ForegroundColor Yellow

$innoSetupPath = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
if (Test-Path $innoSetupPath) {
    & $innoSetupPath "AnchorPOS_Installer.iss"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Installer created successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "  BUILD COMPLETE!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Installer location:" -ForegroundColor Yellow
        Write-Host "  installer_output\AnchorPOS_Setup_v3.5.0.exe" -ForegroundColor White
        Write-Host ""
    }
    else {
        Write-Host "✗ Installer creation failed!" -ForegroundColor Red
    }
}
else {
    Write-Host "⚠ Inno Setup not found at: $innoSetupPath" -ForegroundColor Yellow
}
