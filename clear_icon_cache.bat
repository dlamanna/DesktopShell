@echo off
echo Clearing Windows Icon Cache...
echo.

echo Stopping Windows Explorer...
taskkill /f /im explorer.exe

echo Clearing icon cache...
del /q "%localappdata%\IconCache.db" 2>nul
del /q "%localappdata%\Microsoft\Windows\Explorer\iconcache*" 2>nul
del /q "%localappdata%\Microsoft\Windows\Explorer\thumbcache*" 2>nul

echo Restarting Windows Explorer...
start explorer.exe

echo.
echo Icon cache cleared! You may need to restart DesktopShell to see the new icon.
echo.
pause

