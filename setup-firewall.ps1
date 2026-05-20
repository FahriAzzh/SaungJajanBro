# Firewall Setup Script for SaungJajan
# Run this script as Administrator to allow network access

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  SaungJajan Firewall Setup" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This script will add firewall rules to allow network access." -ForegroundColor Yellow
Write-Host "You need to run this as Administrator." -ForegroundColor Yellow
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: Please run this script as Administrator!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Right-click PowerShell → Run as Administrator" -ForegroundColor Yellow
    Write-Host ""
    pause
    exit
}

Write-Host "Checking existing rules..." -ForegroundColor White
Write-Host ""

# Remove existing rules if any
Remove-NetFirewallRule -DisplayName "SaungJajan HTTP" -ErrorAction SilentlyContinue
Remove-NetFirewallRule -DisplayName "SaungJajan HTTPS" -ErrorAction SilentlyContinue

# Create new firewall rules
Write-Host "Creating firewall rules..." -ForegroundColor Yellow
Write-Host ""

try {
    # HTTP Rule (Port 5276)
    New-NetFirewallRule -DisplayName "SaungJajan HTTP" -Direction Inbound -LocalPort 5276 -Protocol TCP -Action Allow -Profile Any -Description "Allow inbound HTTP traffic for SaungJajan ASP.NET Core app"
    
    Write-Host "HTTP Rule (Port 5276) - Created" -ForegroundColor Green

    # HTTPS Rule (Port 7077)
    New-NetFirewallRule -DisplayName "SaungJajan HTTPS" -Direction Inbound -LocalPort 7077 -Protocol TCP -Action Allow -Profile Any -Description "Allow inbound HTTPS traffic for SaungJajan ASP.NET Core app"
    
    Write-Host "HTTPS Rule (Port 7077) - Created" -ForegroundColor Green
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host "  Firewall Setup Complete!" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Other devices on the same network can now access your server." -ForegroundColor White
    Write-Host ""
    Write-Host "Next step: Run start-server.ps1 to start the application" -ForegroundColor Yellow
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "ERROR: Failed to create firewall rules" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
}

pause
