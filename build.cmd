@echo off
setlocal enabledelayedexpansion

REM =========================================
REM NUKE Build Script for Windows
REM =========================================

set SCRIPT_DIR=%~dp0
set BUILD_PROJECT=%SCRIPT_DIR%build\_build.csproj
set TEMP_DIR=%SCRIPT_DIR%.nuke\temp

REM Check if .NET is installed
where dotnet >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: .NET SDK is not installed or not in PATH
    exit /b 1
)

REM Check if Nuke.GlobalTool is installed
dotnet tool list --global | findstr /C:"nuke.globaltool" >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo Installing Nuke.GlobalTool...
    dotnet tool install Nuke.GlobalTool --global
)

REM Create temp directory if it doesn't exist
if not exist "%TEMP_DIR%" mkdir "%TEMP_DIR%"

REM Run Nuke with all passed arguments
dotnet run --project "%BUILD_PROJECT%" --no-launch-profile -- %*
exit /b %ERRORLEVEL%
