# Unity MCP bridge helper for DSH agent.
# Talks to the MCP for Unity HTTP endpoint (streamable HTTP / JSON-RPC over POST).
#
# Usage:
#   .\Tools\unity-mcp.ps1 -ListTools
#   .\Tools\unity-mcp.ps1 -Resource "mcpforunity://instances"
#   .\Tools\unity-mcp.ps1 -CommandsJson '[{"tool":"manage_scene","params":{"action":"get_active"}}]'
#   .\Tools\unity-mcp.ps1 -Tool read_console -ParamsJson '{"action":"get","max_count":10}'
#
# Exit code 0 = all commands succeeded, 1 = any failure / connection error.

[CmdletBinding(DefaultParameterSetName = "Batch")]
param(
    [string]$Endpoint = "http://127.0.0.1:7070/mcp",

    [Parameter(ParameterSetName = "List")]
    [switch]$ListTools,

    [Parameter(ParameterSetName = "Resource")]
    [string]$Resource,

    [Parameter(Mandatory, ParameterSetName = "Batch", Position = 0)]
    [string]$CommandsJson,

    [Parameter(ParameterSetName = "Single")]
    [string]$Tool,

    [Parameter(ParameterSetName = "Single")]
    [string]$ParamsJson = "{}"
)

$ErrorActionPreference = "Stop"

function Invoke-Mcp {
    param([hashtable]$Body, [string]$Sid)
    $headers = @{ Accept = "application/json, text/event-stream" }
    if ($Sid) { $headers["Mcp-Session-Id"] = $Sid }
    $json = ConvertTo-Json -InputObject $Body -Depth 12 -Compress
    if ($env:UNITY_MCP_DEBUG) { Write-Host "PAYLOAD: $json" }
    $resp = Invoke-WebRequest -Uri $Endpoint -Method Post -Body $json -ContentType "application/json" `
        -Headers $headers -UseBasicParsing -TimeoutSec 300
    $sidOut = ($resp.Headers["Mcp-Session-Id"] -join ",")
    $dataLines = @($resp.Content -split "`n" |
        Where-Object { $_ -like "data:*" } |
        ForEach-Object { $_.Substring(5).Trim() })
    $objects = @($dataLines | ForEach-Object { try { $_ | ConvertFrom-Json } catch {} })
    [pscustomobject]@{ Session = $sidOut; Objects = $objects; Raw = $resp.Content }
}

# ---- handshake -------------------------------------------------------------
$init = Invoke-Mcp @{
    jsonrpc = "2.0"; id = 1; method = "initialize"
    params  = @{
        protocolVersion = "2025-03-26"
        capabilities    = @{}
        clientInfo      = @{ name = "dsh-agent"; version = "1.0" }
    }
} $null
$sid = $init.Session
Invoke-Mcp @{ jsonrpc = "2.0"; method = "notifications/initialized" } $sid | Out-Null

# ---- modes -----------------------------------------------------------------
if ($ListTools) {
    $r = Invoke-Mcp @{ jsonrpc = "2.0"; id = 2; method = "resources/read"; params = @{ uri = "mcpforunity://custom-tools" } } $sid
    $txt = ($r.Objects | Where-Object { $_.result } | ForEach-Object { $_.result.contents[0].text }) -join "`n"
    $parsed = $txt | ConvertFrom-Json
    "tools: $($parsed.data.tool_count)"
    $parsed.data.tools | ForEach-Object { $_.name }
    exit 0
}

if ($Resource) {
    $r = Invoke-Mcp @{ jsonrpc = "2.0"; id = 2; method = "resources/read"; params = @{ uri = $Resource } } $sid
    $ok = $r.Objects | Where-Object { $_.result } | Select-Object -First 1
    if (-not $ok) {
        "resource read failed:"
        $r.Raw
        exit 1
    }
    $ok.result.contents | ForEach-Object { $_.text }
    exit 0
}

if ($Tool) {
    $p = ConvertFrom-Json -InputObject $ParamsJson
    $CommandsJson = ConvertTo-Json -InputObject @(@{ tool = $Tool; params = $p }) -Depth 12 -Compress
}

# ---- batch_execute ---------------------------------------------------------
# NOTE: on Windows PowerShell 5.1, ConvertFrom-Json -InputObject returns a
# PSObject-wrapped array that serializes as {value:[...],Count:1}; wrapping the
# already-assigned variable with @() produces a clean Object[] that converts
# back to a proper JSON array.
$parsedCommands = ConvertFrom-Json -InputObject $CommandsJson
$commands = @($parsedCommands)
$call = Invoke-Mcp @{
    jsonrpc = "2.0"; id = 3; method = "tools/call"
    params  = @{
        name      = "batch_execute"
        arguments = @{ commands = $commands }
    }
} $sid

$resultObj = $call.Objects | Where-Object { $_.result -or $_.error } | Where-Object { $_.id -eq 3 } | Select-Object -First 1
if (-not $resultObj) {
    "no result payload. raw:"
    $call.Raw
    exit 1
}
if ($resultObj.error) {
    "jsonrpc error: " + ($resultObj.error | ConvertTo-Json -Depth 6 -Compress)
    exit 1
}

$sc = $resultObj.result.structuredContent
if ($sc) {
    $sc | ConvertTo-Json -Depth 12
    if ($sc.success -eq $false) { exit 1 } else { exit 0 }
}
# fallback: plain text content
$resultObj.result.content | ForEach-Object { $_.text }
exit 0
