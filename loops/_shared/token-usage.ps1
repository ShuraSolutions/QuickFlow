<#
.SYNOPSIS
  Prints cumulative token usage of a Claude Code session as JSON.

.DESCRIPTION
  Reads the session transcript(s) that Claude Code writes under
  ~/.claude/projects/<encoded-project-path>/<session-id>.jsonl (plus any
  subagent transcripts under ~/.claude/projects/<...>/<session-id>/),
  sums the `usage` block of every assistant message, and prints one JSON
  object. The same API message is written on several transcript lines
  (one per content block), so usage is de-duplicated by message id.

  Loops call this at the start and end of each milestone and log the
  difference of `total` as "tokens used". The snapshot cannot include the
  message being generated at the moment of the call, so each reading lags
  by at most one assistant turn.

.PARAMETER SessionId
  Session to measure. Default: the most recently modified transcript of
  this project (i.e. the current session).

.PARAMETER ProjectPath
  Project root whose transcripts to read. Default: current directory.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File loops/_shared/token-usage.ps1
#>
param(
    [string]$SessionId,
    [string]$ProjectPath = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'

function Write-Result($obj) {
    $obj | ConvertTo-Json -Compress -Depth 4
}

try {
    $configDir = if ($env:CLAUDE_CONFIG_DIR) { $env:CLAUDE_CONFIG_DIR } else { Join-Path $HOME '.claude' }
    $projectsDir = Join-Path $configDir 'projects'

    # Claude Code encodes the project path by replacing every non-alphanumeric char with '-'.
    $encoded = ($ProjectPath.TrimEnd('\', '/')) -replace '[^A-Za-z0-9]', '-'
    $projDir = Get-ChildItem -LiteralPath $projectsDir -Directory |
        Where-Object { $_.Name -ieq $encoded } | Select-Object -First 1
    if (-not $projDir) { throw "No transcript folder for '$ProjectPath' (expected '$encoded' under $projectsDir)." }

    if ($SessionId) {
        $main = Get-Item -LiteralPath (Join-Path $projDir.FullName "$SessionId.jsonl")
    } else {
        $main = Get-ChildItem -LiteralPath $projDir.FullName -Filter '*.jsonl' -File |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if (-not $main) { throw "No session transcripts in $($projDir.FullName)." }
    }
    $sid = [IO.Path]::GetFileNameWithoutExtension($main.Name)

    $files = @($main)
    $subDir = Join-Path $projDir.FullName $sid
    if (Test-Path -LiteralPath $subDir) {
        $files += Get-ChildItem -LiteralPath $subDir -Filter '*.jsonl' -File -Recurse
    }

    $byMessage = @{}
    foreach ($f in $files) {
        foreach ($line in [IO.File]::ReadLines($f.FullName)) {
            if ($line.IndexOf('"type":"assistant"') -lt 0 -or $line.IndexOf('"usage"') -lt 0) { continue }
            try { $rec = $line | ConvertFrom-Json } catch { continue }
            if ($rec.type -ne 'assistant' -or -not $rec.message -or -not $rec.message.usage) { continue }
            $id = if ($rec.message.id) { $rec.message.id } else { $rec.uuid }
            $byMessage[$id] = $rec.message.usage   # last line of a message carries its final usage
        }
    }

    $sum = @{ input = 0L; cache_creation = 0L; cache_read = 0L; output = 0L }
    foreach ($u in $byMessage.Values) {
        if ($u.input_tokens)                { $sum.input          += [long]$u.input_tokens }
        if ($u.cache_creation_input_tokens) { $sum.cache_creation += [long]$u.cache_creation_input_tokens }
        if ($u.cache_read_input_tokens)     { $sum.cache_read     += [long]$u.cache_read_input_tokens }
        if ($u.output_tokens)               { $sum.output         += [long]$u.output_tokens }
    }

    Write-Result ([ordered]@{
        ok             = $true
        method         = 'measured'
        session_id     = $sid
        transcripts    = $files.Count
        messages       = $byMessage.Count
        input          = $sum.input
        cache_creation = $sum.cache_creation
        cache_read     = $sum.cache_read
        output         = $sum.output
        total          = $sum.input + $sum.cache_creation + $sum.cache_read + $sum.output
        measured_at    = (Get-Date -Format o)
    })
}
catch {
    Write-Result ([ordered]@{
        ok          = $false
        method      = 'estimated'
        error       = $_.Exception.Message
        measured_at = (Get-Date -Format o)
    })
    exit 1
}
