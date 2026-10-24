@echo off
title BookMySlot - Automated Tests
color 0A

echo ===================================================================
echo             BookMySlot - Running xUnit Test Suite
echo ===================================================================
echo.

where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    color 0C
    echo [ERROR] .NET SDK was not found!
    pause
    exit /b 1
)

cd /d "%~dp0"
echo Running tests against SQLite integration test database...
echo.

dotnet test BookMySlot.slnx

echo.
echo ===================================================================
echo Test Execution Finished.
echo ===================================================================
pause
