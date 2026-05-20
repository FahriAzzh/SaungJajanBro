@echo off
echo.
echo =====================================
echo      SaungJajan Server Launcher
echo =====================================
echo.

REM Get IP Address
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr /c:"IPv4"') do (
    set "IP=%%a"
    goto :found
)
:found

REM Clean up IP (remove leading space)
set "IP=%IP:~1%"

echo Server IP: %IP%
echo.
echo =====================================
echo   ACCESS URLS:
echo =====================================
echo.
echo   Local (This PC):
echo   http://localhost:5276
echo.
echo   Network (Other Devices):
echo   http://%IP%:5276
echo.
echo =====================================
echo.
echo Share URL to:
echo   Sellers (Penjual):  http://%IP%:5276/Auth/LoginToko
echo   Buyers (Pembeli):   http://%IP%:5276/Auth/Login
echo   Kiosk Mode:         http://%IP%:5276
echo.
echo =====================================
echo.
echo Starting ASP.NET Core Server...
echo Press Ctrl+C to stop
echo.

dotnet run
