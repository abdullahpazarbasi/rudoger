#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $DotNetArguments
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

& docker run --rm `
    --env HOME=/tmp/dotnet-home `
    --env DOTNET_CLI_HOME=/tmp/dotnet-home `
    --env NUGET_PACKAGES=/tmp/nuget `
    --volume rudoger-dotnet-home:/tmp/dotnet-home `
    --volume rudoger-nuget:/tmp/nuget `
    --volume "${repositoryRoot}:/workspace" `
    --workdir /workspace `
    mcr.microsoft.com/dotnet/sdk:10.0 `
    dotnet @DotNetArguments

exit $LASTEXITCODE
