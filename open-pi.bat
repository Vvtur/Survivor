@echo off
REM ============================================================
REM  一键打开 pi 对话框（恢复最近一次会话）
REM  行为：进入项目目录 + 启动 pi + 让你从历史会话里选
REM ============================================================

setlocal

set "PROJECT_DIR=%~dp0"
set "PI_EXE=pi"

echo.
echo ========================================
echo  打开 pi（恢复最近对话）
echo  项目: %PROJECT_DIR%
echo ========================================
echo.

cd /d "%PROJECT_DIR%"

REM --resume 会进入会话选择器，↑↓ 选，回车确认
"%PI_EXE%" --resume

if errorlevel 1 (
    echo.
    echo pi 启动失败，检查：
    echo   1. pi 是否在 PATH 里: where pi
    echo   2. 或者改成完整路径: C:\path\to\pi.exe
    echo.
    pause
)
