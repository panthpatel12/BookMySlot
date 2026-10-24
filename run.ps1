# PowerShell Launch Script for BookMySlot
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "           BookMySlot - Appointment Booking System" -ForegroundColor White
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""

# Check dotnet
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "[ERROR] .NET SDK is not installed on this PC!" -ForegroundColor Red
    Write-Host "Please download and install .NET SDK (v10.0 or v8.0+) from:" -ForegroundColor Yellow
    Write-Host "https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    Read-Host "Press Enter to exit..."
    exit 1
}

Write-Host "[1/3] Checking .NET SDK..." -ForegroundColor Green
$version = dotnet --version
Write-Host "Found .NET SDK: $version" -ForegroundColor Gray
Write-Host ""

Write-Host "[2/3] Configuring Application..." -ForegroundColor Green
Write-Host "  Database: SQLite (bookmyslot.db - zero setup needed)" -ForegroundColor Gray
Write-Host "  URL:      http://localhost:5100" -ForegroundColor Gray
Write-Host ""

Write-Host "[3/3] Launching Web Server..." -ForegroundColor Green
Write-Host "Opening http://localhost:5100 in browser..." -ForegroundColor Yellow
Start-Process "http://localhost:5100"

Set-Location "$PSScriptRoot\src\BookMySlot"
dotnet run --urls "http://localhost:5100"
