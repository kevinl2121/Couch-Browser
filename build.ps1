#Requires -Version 5.1
<#
  Builds Couch Browser into .\dist\CouchBrowser.exe
  Usage: powershell -ExecutionPolicy Bypass -File .\build.ps1
#>
$ErrorActionPreference = 'Stop'
$proj = Join-Path $PSScriptRoot 'src\CouchBrowser.csproj'
$out  = Join-Path $PSScriptRoot 'dist'

function Find-Dotnet {
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $p = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
    if (Test-Path $p) { return $p }
    return $null
}

$dotnet = Find-Dotnet
$hasSdk = $dotnet -and ((& $dotnet --list-sdks 2>$null) -match '^(8|9|10)\.')
if (-not $hasSdk) {
    Write-Host '==> Installing the .NET 8 SDK (one-time)...' -ForegroundColor Cyan
    winget install --id Microsoft.DotNet.SDK.8 -e --accept-source-agreements --accept-package-agreements
    $dotnet = Find-Dotnet
    if (-not $dotnet) { throw '.NET SDK install failed. Install it from dotnet.microsoft.com and run this again.' }
}

Get-Process CouchBrowser -ErrorAction SilentlyContinue | Stop-Process -Force
Write-Host '==> Building...' -ForegroundColor Cyan
& $dotnet publish $proj -c Release -o $out --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed. Copy the error lines above into the chat.' }
Write-Host "Built: $(Join-Path $out 'CouchBrowser.exe')" -ForegroundColor Green