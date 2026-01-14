@echo off
echo Starting Anchor POS...
cd /d "%~dp0"
dotnet run --project src\AnchorPOS.Desktop\AnchorPOS.Desktop.csproj
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Execution failed!
    pause
)
