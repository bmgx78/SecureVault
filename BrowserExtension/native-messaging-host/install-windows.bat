@echo off
REM Run as Administrator to register the SecureVault native messaging host for Chrome/Edge
REM Replace YOUR_EXTENSION_ID_HERE with your actual Chrome extension ID

set KEY=HKEY_CURRENT_USER\SOFTWARE\Google\Chrome\NativeMessagingHosts\com.securevault.host
set HOST_PATH=%~dp0com.securevault.host.json

reg add "%KEY%" /ve /t REG_SZ /d "%HOST_PATH%" /f
echo SecureVault native messaging host registered for Chrome.

set KEY_EDGE=HKEY_CURRENT_USER\SOFTWARE\Microsoft\Edge\NativeMessagingHosts\com.securevault.host
reg add "%KEY_EDGE%" /ve /t REG_SZ /d "%HOST_PATH%" /f
echo SecureVault native messaging host registered for Edge.

REM Firefox uses a different path:
set FIREFOX_DIR=%APPDATA%\Mozilla\NativeMessagingHosts
mkdir "%FIREFOX_DIR%" 2>nul
copy /y "%HOST_PATH%" "%FIREFOX_DIR%\com.securevault.host.json"
echo SecureVault native messaging host registered for Firefox.

pause
