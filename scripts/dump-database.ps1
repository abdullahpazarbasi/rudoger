#!/usr/bin/env pwsh

param(
    [string] $OutputPath = "",
    [switch] $Force,
    [switch] $Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($Help) {
    Write-Output "Usage: .\scripts\dump-database.ps1 [-OutputPath PATH] [-Force]"
    Write-Output ""
    Write-Output "Exports the configured Rudoger database schema and table data to a T-SQL file."
    Write-Output "The default output is artifacts/database/rudoger-<UTC timestamp>.sql."
    exit 0
}

$dotnet = Get-Command "dotnet" -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    throw "dotnet was not found. Install the .NET 10 SDK."
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $timestamp = [DateTimeOffset]::UtcNow.ToString("yyyyMMddTHHmmssZ")
    $OutputPath = Join-Path $repositoryRoot "artifacts/database/rudoger-$timestamp.sql"
} elseif (-not [IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path (Get-Location).Path $OutputPath
}

$fullOutputPath = [IO.Path]::GetFullPath($OutputPath)
if (-not [IO.Path]::GetExtension($fullOutputPath).Equals(".sql", [StringComparison]::OrdinalIgnoreCase)) {
    throw "Output path must use the .sql extension."
}

$outputDirectory = [IO.Path]::GetDirectoryName($fullOutputPath)
if ([string]::IsNullOrWhiteSpace($outputDirectory)) {
    throw "Output path must include a directory."
}
[void] (New-Item -ItemType Directory -Path $outputDirectory -Force)

if ((Test-Path -LiteralPath $fullOutputPath) -and -not $Force) {
    throw "Output already exists: $fullOutputPath. Use -Force to replace it."
}

$connectionString = & (Join-Path $PSScriptRoot "db-connection-string.ps1")
$toolProject = Join-Path $repositoryRoot "Tools/DatabaseDump/Rudoger.DatabaseDump.csproj"
$dotnetArguments = @(
    "run",
    "--project", $toolProject,
    "--configuration", "Release",
    "--no-launch-profile",
    "--",
    "--output", $fullOutputPath
)
if ($Force) {
    $dotnetArguments += "--force"
}

$variableName = "RUDOGER_DATABASE_DUMP_CONNECTION_STRING"
$previousConnectionString = [Environment]::GetEnvironmentVariable($variableName, "Process")
try {
    [Environment]::SetEnvironmentVariable($variableName, $connectionString, "Process")
    & $dotnet.Source @dotnetArguments
    $exitCode = $LASTEXITCODE
} finally {
    [Environment]::SetEnvironmentVariable($variableName, $previousConnectionString, "Process")
    $connectionString = $null
}

if ($exitCode -ne 0) {
    throw "Database dump tool failed with exit code $exitCode."
}
