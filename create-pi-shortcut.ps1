# ============================================================
#  在桌面创建 pi 快捷方式（双击继续最近会话）
#  右键 → "使用 PowerShell 运行"  或在 pwsh 里执行
# ============================================================

$ErrorActionPreference = "Stop"

# --- 配置区（按需修改）---
$piExe       = "E:\PI\pi.exe"
$projectDir  = "E:\UN\Survivor"
$shortcutName = "继续和 pi 聊天.lnk"
# ---------------------------

if (-not (Test-Path $piExe)) {
    Write-Host "✗ 找不到 pi.exe: $piExe" -ForegroundColor Red
    Write-Host "  请修改脚本顶部的 `$piExe 变量" -ForegroundColor Yellow
    pause
    exit 1
}

if (-not (Test-Path $projectDir)) {
    Write-Host "✗ 找不到项目目录: $projectDir" -ForegroundColor Red
    pause
    exit 1
}

# 桌面路径
$desktop = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktop $shortcutName

# 如果已存在，先删
if (Test-Path $shortcutPath) {
    Remove-Item $shortcutPath -Force
    Write-Host "  已删除旧的快捷方式" -ForegroundColor Yellow
}

# 创建快捷方式
$ws = New-Object -ComObject WScript.Shell
$sc = $ws.CreateShortcut($shortcutPath)
$sc.TargetPath       = $piExe
$sc.Arguments        = "--continue"
$sc.WorkingDirectory = $projectDir
$sc.WindowStyle      = 1  # 1=正常窗口，3=最大化，7=最小化
$sc.IconLocation     = $piExe + ",0"
$sc.Description      = "启动 pi 并继续最近一次会话（项目：$projectDir）"
$sc.Save()

# 验证
if (Test-Path $shortcutPath) {
    Write-Host ""
    Write-Host "✓ 已创建桌面快捷方式" -ForegroundColor Green
    Write-Host "  位置: $shortcutPath" -ForegroundColor Cyan
    Write-Host "  目标: $piExe --continue" -ForegroundColor Cyan
    Write-Host "  工作目录: $projectDir" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "现在双击桌面上的 '$shortcutName' 就能继续最近的 pi 会话" -ForegroundColor Green
} else {
    Write-Host "✗ 创建失败" -ForegroundColor Red
}

# 防止窗口闪退
Write-Host ""
Write-Host "按任意键退出..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
