@echo off
cd /d "%~dp0"
echo ================================================
echo Starting MQTT Chat System
echo ================================================
echo.
echo Starting Server...
start "MQTT Server" cmd /k "dotnet run --project src\ChatMQTT.Server.WPF\ChatMQTT.Server.WPF.csproj"

echo Waiting 3 seconds for server to start...
timeout /t 3 /nobreak > nul

echo Starting Client 1...
start "MQTT Client 1" cmd /k "dotnet run --project src\ChatMQTT.Client.WPF\ChatMQTT.Client.WPF.csproj"

echo Starting Client 2...
start "MQTT Client 2" cmd /k "dotnet run --project src\ChatMQTT.Client.WPF\ChatMQTT.Client.WPF.csproj"

echo.
echo ================================================
echo All applications started!
echo - 1 Server window
echo - 2 Client windows
echo ================================================
echo.
echo Press any key to exit this window...
pause > nul
