#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Read-DotEnvFile {
    param([Parameter(Mandatory)][string] $Path)

    $values = @{}
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $values
    }

    foreach ($rawLine in Get-Content -LiteralPath $Path) {
        $line = $rawLine.Trim()
        if ($line.Length -eq 0 -or $line.StartsWith("#")) {
            continue
        }

        $separator = $line.IndexOf("=")
        if ($separator -le 0) {
            continue
        }

        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($value.Length -ge 2) {
            $isDoubleQuoted = $value[0] -eq '"' -and $value[$value.Length - 1] -eq '"'
            $isSingleQuoted = $value[0] -eq "'" -and $value[$value.Length - 1] -eq "'"
            if ($isDoubleQuoted -or $isSingleQuoted) {
                $value = $value.Substring(1, $value.Length - 2)
            }
        }

        $values[$key] = $value
    }

    return $values
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$settings = @{}

foreach ($environmentFile in @(
    (Join-Path $repositoryRoot ".env"),
    (Join-Path $repositoryRoot ".env.local")
)) {
    $fileSettings = Read-DotEnvFile -Path $environmentFile
    foreach ($key in $fileSettings.Keys) {
        $settings[$key] = $fileSettings[$key]
    }
}

$connectionString = if ($settings.ContainsKey("ConnectionStrings__Rudoger")) {
    [string] $settings["ConnectionStrings__Rudoger"]
} else {
    ""
}
$mssqlPort = if ($settings.ContainsKey("MSSQL_PORT")) {
    [string] $settings["MSSQL_PORT"]
} else {
    "1433"
}

$processConnectionString = [Environment]::GetEnvironmentVariable("ConnectionStrings__Rudoger")
if ($null -ne $processConnectionString) {
    $connectionString = $processConnectionString
}
$processPort = [Environment]::GetEnvironmentVariable("MSSQL_PORT")
if ($null -ne $processPort) {
    $mssqlPort = $processPort
}

if ([string]::IsNullOrWhiteSpace($connectionString)) {
    throw "ConnectionStrings__Rudoger is missing. Configure .env.local first."
}
if ($connectionString.IndexOf("REPLACE_WITH", [StringComparison]::Ordinal) -ge 0) {
    throw "ConnectionStrings__Rudoger still contains a placeholder."
}

$parsedPort = 0
if (-not [int]::TryParse($mssqlPort, [ref] $parsedPort) -or $parsedPort -lt 1 -or $parsedPort -gt 65535) {
    throw "MSSQL_PORT must be an integer between 1 and 65535."
}

$segments = $connectionString.Split(@([char] ";"), [StringSplitOptions]::None)
$serverFound = $false
for ($index = 0; $index -lt $segments.Length; $index++) {
    $separator = $segments[$index].IndexOf("=")
    if ($separator -le 0) {
        continue
    }

    $key = $segments[$index].Substring(0, $separator).Trim()
    if ($key.Equals("Server", [StringComparison]::OrdinalIgnoreCase) -or
        $key.Equals("Data Source", [StringComparison]::OrdinalIgnoreCase)) {
        $segments[$index] = "Server=localhost,$parsedPort"
        $serverFound = $true
    }
}

if (-not $serverFound) {
    throw "ConnectionStrings__Rudoger does not contain a Server or Data Source entry."
}

Write-Output ($segments -join ";")
