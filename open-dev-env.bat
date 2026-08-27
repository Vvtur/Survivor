@echo off
REM ============================================================
REM  Survivor 开发环境一键启动
REM  用法：双击运行，或命令行 open-dev-env.bat
REM  按需编辑下面"配置区"的路径
REM ============================================================

setlocal enabledelayedexpansion

REM ==================== 配置区 ====================
REM 项目根目录（当前脚本所在目录）
set "PROJECT_DIR=%~dp0"

REM VSCode 安装路径（留空则用 PATH 里的 code）
set "VSCODE_EXE="

REM Unity Hub 可执行文件路径（留空则不启动 Unity）
set "UNITY_HUB_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe"
set "UNITY_PROJECT=%PROJECT_DIR%"

REM 微信开发者工具可执行文件路径（留空则不启动）
set "WECHAT_DEV_EXE=C:\Program Files (x86)\Tencent\微信开发者工具\微信开发者工具.exe"
set "WECHAT_PROJECT=%PROJECT_DIR%\WX\minigame"

REM 资源管理器打开项目目录（一般开一下方便看文件）
set "OPEN_EXPLORER=1"
REM ============================================================

echo.
echo ========================================
echo  Survivor 开发环境启动
echo  项目: %PROJECT_DIR%
echo ========================================
echo.

REM 1. 资源管理器
if "%OPEN_EXPLORER%"=="1" (
    echo [1/4] 打开项目目录...
    start "" explorer "%PROJECT_DIR%"
    timeout /t 1 /nobreak >nul
)

REM 2. VSCode
echo [2/4] 启动 VSCode...
if defined VSCODE_EXE (
    if exist "%VSCODE_EXE%" (
        start "" "%VSCODE_EXE%" "%PROJECT_DIR%"
    ) else (
        echo   路径不存在，回退到 PATH: %VSCODE_EXE%
        start "" code "%PROJECT_DIR%"
    )
) else (
    start "" code "%PROJECT_DIR%"
)
timeout /t 1 /nobreak >nul

REM 3. Unity Editor（可选）
echo [3/4] 启动 Unity Editor...
if defined UNITY_HUB_EXE (
    if exist "%UNITY_HUB_EXE%" (
        start "" "%UNITY_HUB_EXE%" -projectPath "%UNITY_PROJECT%"
    ) else (
        echo   Unity 未安装或路径不对，跳过: %UNITY_HUB_EXE%
    )
) else (
    echo   UNITY_HUB_EXE 未配置，跳过
)
timeout /t 1 /nobreak >nul

REM 4. 微信开发者工具（可选）
echo [4/4] 启动微信开发者工具...
if defined WECHAT_DEV_EXE (
    if exist "%WECHAT_DEV_EXE%" (
        if exist "%WECHAT_PROJECT%" (
            start "" "%WECHAT_DEV_EXE%" --project "%WECHAT_PROJECT%"
        ) else (
            echo   微信项目路径不存在，跳过: %WECHAT_PROJECT%
        )
    ) else (
        echo   微信开发者工具未安装或路径不对，跳过: %WECHAT_DEV_EXE%
    )
) else (
    echo   WECHAT_DEV_EXE 未配置，跳过
)

echo.
echo ========================================
echo  启动完成！
echo ========================================
echo.
echo  下一步建议：
echo    - 在 Unity 里等编译完成（首次会久一些）
echo    - 在微信开发者工具里点"编译"预览
echo    - 回到 pi：cd E:\UN\Survivor ^&^& pi
echo.
pause
