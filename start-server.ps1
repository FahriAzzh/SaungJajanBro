# SaungJajan Server Launcher
# Script ini akan menampilkan IP Address dan menjalankan aplikasi

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "     SaungJajan Server Launcher" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Get IP Address (WiFi atau Ethernet)
$networkInterfaces = Get-NetIPAddress -AddressFamily IPv4 | Where-Object { 
    $_.InterfaceAlias -notlike "*Loopback*" -and 
    $_.InterfaceAlias -notlike "*vEthernet*" -and
    $_.IPAddress -like "192.168.*" -or $_.IPAddress -like "10.*" 
}

if ($networkInterfaces) {
    $serverIP = ($networkInterfaces | Select-Object -First 1).IPAddress
} else {
    # Fallback: get any non-loopback IPv4
    $serverIP = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notlike "*Loopback*" } | Select-Object -First 1).IPAddress
}

if (-not $serverIP) {
    Write-Host "ERROR: Tidak bisa mendapatkan IP Address" -ForegroundColor Red
    Write-Host "Pastikan Anda terhubung ke jaringan WiFi/Ethernet" -ForegroundColor Yellow
    Write-Host ""
    pause
    exit
}

$localURL = "http://localhost:5276"
$networkURL = "http://${serverIP}:5276"

Write-Host "✓ Server IP: $serverIP" -ForegroundColor Green
Write-Host ""
Write-Host "=====================================" -ForegroundColor Yellow
Write-Host "  ACCESS URLS:" -ForegroundColor Yellow
Write-Host "=====================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Local (This PC):" -ForegroundColor White
Write-Host "  $localURL" -ForegroundColor Green
Write-Host ""
Write-Host "  Network (Other Devices):" -ForegroundColor White
Write-Host "  $networkURL" -ForegroundColor Yellow
Write-Host ""
Write-Host "=====================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "Share URL to:" -ForegroundColor White
Write-Host "  📱 Sellers (Penjual):  $networkURL/Auth/LoginToko" -ForegroundColor Cyan
Write-Host "  🛒 Buyers (Pembeli):   $networkURL/Auth/Login" -ForegroundColor Cyan
Write-Host "  📋 Kiosk Mode:         $networkURL" -ForegroundColor Cyan
Write-Host ""
Write-Host "=====================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "Starting ASP.NET Core Server..." -ForegroundColor Green
Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
Write-Host ""

# Run the application
dotnet run
