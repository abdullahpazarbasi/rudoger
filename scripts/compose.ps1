#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ComposeArguments
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$environmentFile = Join-Path $repositoryRoot ".env.local"

if (-not (Test-Path -LiteralPath $environmentFile -PathType Leaf)) {
    throw "Missing .env.local. Copy .env.dist to .env.local and replace the placeholders."
}

& docker compose `
    --project-directory $repositoryRoot `
    --env-file (Join-Path $repositoryRoot ".env") `
    --env-file $environmentFile `
    @ComposeArguments

exit $LASTEXITCODE
