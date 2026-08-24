@echo off
rem Double-click to run: restore custom files wiped by the WeChat conversion tool
rem (cloudfunctions + open-data + configs). See restore-custom.ps1 for details.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0restore-custom.ps1"
pause
