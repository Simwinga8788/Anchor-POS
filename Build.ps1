dotnet clean --configuration Release
dotnet restore
dotnet build --configuration Release --no-restore
dotnet publish src\AnchorPOS.Desktop\AnchorPOS.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true --output "src\AnchorPOS.Desktop\bin\Release\net10.0-windows\win-x64\publish" /p:PublishSingleFile=true /p:PublishTrimmed=false /p:IncludeNativeLibrariesForSelfExtract=true

$iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) {
    $iscc = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
}

if (Test-Path $iscc) {
    & $iscc "AnchorPOS_Installer.iss"
}
else {
    Write-Error "Inno Setup Compiler (ISCC.exe) not found. Please install Inno Setup."
    exit 1
}
