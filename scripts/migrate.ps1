#!/usr/bin/env pwsh

& (Join-Path $PSScriptRoot "compose.ps1") run --rm migrate
exit $LASTEXITCODE
