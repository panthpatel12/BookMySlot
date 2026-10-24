@echo off
title BookMySlot - Appointment Booking System
color 0B

echo ===================================================================
echo             BookMySlot - Appointment Booking System
echo ===================================================================
echo.

:: Check if dotnet is installed
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    color 0C
    echo [ERROR] .NET SDK was not found on this computer!
    echo.
    echo Please install .NET SDK (version 10.0 or 8.0+) from:
    echo https://dotnet.microsoft.com/download
    echo.
    echo After installing, reopen this script.
    echo.
    pause
    exit /b 1
)

echo [1/3] Checking .NET SDK version...
dotnet --version
echo.

echo [2/3] Preparing application...
echo   - Framework: ASP.NET Core MVC
echo   - Database:  SQLite (Self-contained, zero external setup needed)
echo   - Port:      http://localhost:5100
echo.

echo [3/3] Starting web server...
echo.
echo ===================================================================
echo Web Application is starting!
echo Access the site at: http://localhost:5100
echo.
echo Demo Accounts:
echo   - Admin:    admin@bookmyslot.com    / Admin123!
echo   - Provider: provider@bookmyslot.com / Provider123!
echo   - Customer: customer@bookmyslot.com / Customer123!
echo.
echo Press Ctrl+C in this window anytime to stop the server.
echo ===================================================================
echo.

:: Open browser automatically after a short delay
start "" timeout /t 3 /nobreak >nul & start http://localhost:5100

:: Run the ASP.NET Core application
cd /d "%~dp0src\BookMySlot"
dotnet run --urls "http://localhost:5100"

pause
