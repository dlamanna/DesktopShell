@echo off
echo ========================================
echo Comprehensive Windows Icon Cache Clearer
echo ========================================
echo.
echo This script will clear all Windows icon caches
echo and restart necessary services to refresh icons.
echo.
echo WARNING: This will temporarily stop Windows Explorer
echo and may take a few moments to complete.
echo.
pause

echo.
echo Step 1: Stopping Windows Explorer...
taskkill /f /im explorer.exe 2>nul
timeout /t 2 /nobreak >nul

echo Step 2: Clearing icon cache files...
del /q "%localappdata%\IconCache.db" 2>nul
del /q "%localappdata%\Microsoft\Windows\Explorer\iconcache*" 2>nul
del /q "%localappdata%\Microsoft\Windows\Explorer\thumbcache*" 2>nul
del /q "%localappdata%\Microsoft\Windows\Explorer\iconcache*" 2>nul

echo Step 3: Clearing system icon cache...
del /q "%ProgramData%\Microsoft\Windows\Explorer\iconcache*" 2>nul
del /q "%ProgramData%\Microsoft\Windows\Explorer\thumbcache*" 2>nul

echo Step 4: Clearing task manager icon cache...
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3" /f 2>nul
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StreamMRU" /f 2>nul

echo Step 5: Restarting Windows Explorer...
start explorer.exe

echo.
echo Step 6: Waiting for Explorer to fully restart...
timeout /t 5 /nobreak >nul

echo.
echo ========================================
echo Icon cache clearing completed!
echo ========================================
echo.
echo Now try the following:
echo 1. Close DesktopShell completely
echo 2. Run the new DesktopShell.exe from the publish folder
echo 3. Check Task Manager - the icon should now appear correctly
echo.
echo If the icon still doesn't show, you may need to:
echo - Restart your computer
echo - Or run this script as Administrator
echo.
pause
