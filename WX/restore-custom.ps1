# ============================================================
# restore-custom.ps1 - Run once after EVERY WeChat conversion
#
# WHY: the WeChat Unity conversion tool deletes the whole
#      minigame/ directory and rebuilds it from template
#      (WXConvertCore.ConvertCode -> DelectDir(minigame)).
#      All manual changes (cloudfunctions, open-data, configs)
#      are lost. This script restores them from WX/custom/.
#
# To edit cloud functions / leaderboard rendering logic:
#   edit files under WX/custom/ -> run this script ->
#   redeploy cloud functions in WeChat DevTools
# ============================================================

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path   # script dir = WX/
$custom = Join-Path $root 'custom'
$dst = Join-Path $root 'minigame'

if (-not (Test-Path $dst)) {
    Write-Host '[ERROR] minigame/ not found. Run the WeChat conversion tool first.' -ForegroundColor Red
    exit 1
}

# 1. restore cloud functions (save / load / invite)
Copy-Item -Path (Join-Path $custom 'cloudfunctions') -Destination $dst -Recurse -Force
Write-Host '[1/4] cloudfunctions/ restored (save + load + invite)'

# 2. restore open-data (remove tool-generated official template, then copy ours)
$openDataDst = Join-Path $dst 'open-data'
if (Test-Path $openDataDst) { Remove-Item $openDataDst -Recurse -Force }
Copy-Item -Path (Join-Path $custom 'open-data') -Destination $dst -Recurse -Force
Write-Host '[2/4] open-data/ restored (custom friend leaderboard)'

# 2b. invite query stash patch:
#     the transform plugin loses wx.onShow res.query (Dictionary) when
#     dispatching to C#; invite-query.js stashes it via native wx APIs
#     at game.js import time, C# reads it from storage instead.
$iqSrc = Join-Path $custom 'invite-query.js'
if (Test-Path $iqSrc) {
    Copy-Item $iqSrc $dst -Force
    $gameJsPath = Join-Path $dst 'game.js'
    $gameJs = [System.IO.File]::ReadAllText($gameJsPath)
    if ($gameJs -notmatch 'invite-query') {
        [System.IO.File]::WriteAllText($gameJsPath, $gameJs + "`nimport './invite-query'; // restore-custom: invite query stash patch`n")
        Write-Host '       game.js: invite-query import appended'
    } else {
        Write-Host '       game.js: invite-query already present, skip'
    }
} else {
    Write-Host '       [WARN] custom/invite-query.js not found, skip patch' -ForegroundColor Yellow
}

# 3. patch configs (tool regenerates both files every conversion)
# 3a. project.config.json -> cloudfunctionRoot (for DevTools cloud function panel)
$projPath = Join-Path $dst 'project.config.json'
$proj = [System.IO.File]::ReadAllText($projPath)
if ($proj -notmatch '"cloudfunctionRoot"') {
    $proj = $proj -replace '("appid"\s*:)', ('"cloudfunctionRoot": "cloudfunctions/",' + "`n" + '  $1')
    [System.IO.File]::WriteAllText($projPath, $proj)
    Write-Host '[3/4] project.config.json: cloudfunctionRoot added'
}
else {
    Write-Host '[3/4] project.config.json: cloudfunctionRoot already present, skip'
}

# 3b. game.json patches:
#     - add openDataContext (tool removes it if the "friend relation"
#       checkbox was unchecked during conversion)
#     - strip Layout/MiniGameChat plugins (Layout is for the official
#       open-data template which we replaced with a pure-Canvas version;
#       MiniGameChat is unrelated to the leaderboard)
$gamePath = Join-Path $dst 'game.json'
$game = [System.IO.File]::ReadAllText($gamePath)
if ($game -notmatch '"openDataContext"') {
    $game = $game -replace '("deviceOrientation"\s*:\s*"[^"]+"\s*,)', ('$1' + "`n" + '  "openDataContext" : "open-data",')
    Write-Host '       game.json: openDataContext added'
}
$newGame = $game -replace ',\s*"Layout"\s*:\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}', ''
$newGame = $newGame -replace ',\s*"MiniGameChat"\s*:\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}', ''
if ($newGame -ne $game) {
    $game = $newGame
    Write-Host '       game.json: removed Layout/MiniGameChat plugins (custom open-data does not need them)'
}
[System.IO.File]::WriteAllText($gamePath, $game)

Write-Host '[4/4] configs patched'
Write-Host ''
Write-Host 'DONE. You can open WeChat DevTools now.' -ForegroundColor Green
Write-Host 'NOTE: if cloud function logic changed, right-click save/load/invite in DevTools -> upload & deploy'
