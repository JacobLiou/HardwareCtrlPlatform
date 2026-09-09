#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Publish Station.App template (self-contained, default win-x86 for UDL COM).
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [ValidateSet('win-x86', 'win-x64', 'win-arm64')]
    [string]$RuntimeId = 'win-x86',

    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$appProjectPath = Join-Path $projectRoot 'src\Station.App\Station.App.csproj'

if (-not (Test-Path $appProjectPath)) {
    Write-Error "Project not found: $appProjectPath"
    exit 1
}

if ([string]::IsNullOrEmpty($OutputPath)) {
    $OutputPath = Join-Path $projectRoot 'publish'
}

$fullOutputPath = Join-Path (Join-Path $OutputPath $RuntimeId) $Configuration

Write-Host '========================================' -ForegroundColor Cyan
Write-Host 'HardwareCtrlPlatform - Station.App publish' -ForegroundColor Cyan
Write-Host '========================================' -ForegroundColor Cyan
Write-Host "Project: $appProjectPath"
Write-Host "Configuration: $Configuration"
Write-Host "Runtime: $RuntimeId"
Write-Host "Output: $fullOutputPath"

New-Item -ItemType Directory -Force -Path $fullOutputPath | Out-Null

dotnet publish $appProjectPath `
    -c $Configuration `
    -r $RuntimeId `
    --self-contained true `
    -o $fullOutputPath `
    /p:PlatformTarget=x86

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Published to $fullOutputPath" -ForegroundColor Green
