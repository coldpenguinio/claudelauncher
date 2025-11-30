@echo off
setlocal

echo ========================================
echo ClaudeLauncher Installer Build Script
echo ========================================
echo.

:: Check for Inno Setup
set ISCC=
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" (
    set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
) else if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" (
    set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
)

if "%ISCC%"=="" (
    echo ERROR: Inno Setup 6 not found!
    echo Please install from: https://jrsoftware.org/isdl.php
    exit /b 1
)

echo [1/2] Publishing ClaudeLauncher...
cd /d "%~dp0.."
dotnet publish ClaudeLauncher -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

if errorlevel 1 (
    echo ERROR: Build failed!
    exit /b 1
)

echo.
echo [2/2] Building installer...
cd /d "%~dp0"
"%ISCC%" ClaudeLauncher.iss

if errorlevel 1 (
    echo ERROR: Installer build failed!
    exit /b 1
)

echo.
echo ========================================
echo Build complete!
echo Installer: %~dp0Output\ClaudeLauncher-Setup-1.0.0.exe
echo ========================================
pause
