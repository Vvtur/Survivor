@echo off
rem 双击运行：恢复 minigame/ 里被转换工具清掉的定制文件（云函数 + 开放数据域 + 配置）
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0restore-custom.ps1"
pause
