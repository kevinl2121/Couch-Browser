#Requires -Version 5.1
<# Downloads the latest uBlock Origin Lite (Chromium build) for Couch Browser.
   Rerun anytime to update. #>
$ErrorActionPreference = 'Stop'
$dest = Join-Path $env:LOCALAPPDATA 'CouchBrowser\Extensions\ublock-lite'

$rel = Invoke-RestMethod 'https://api.github.com/repos/uBlockOrigin/uBOL-home/releases/latest' `
       -Headers @{ 'User-Agent' = 'couch-browser' }
$asset = $rel.assets | Where-Object name -like '*chromium*.zip' | Select-Object -First 1
if (-not $asset) { throw "No Chromium zip found in $($rel.html_url). Download it manually from there." }

Write-Host "==> Downloading $($asset.name)" -ForegroundColor Cyan
$zip = Join-Path $env:TEMP $asset.name
Invoke-WebRequest $asset.browser_download_url -OutFile $zip -UseBasicParsing

if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
Expand-Archive $zip -DestinationPath $dest -Force
Remove-Item $zip -Force

# If the zip has a top-level folder, move its contents up so manifest.json sits in $dest
$manifest = Get-ChildItem $dest -Recurse -Filter manifest.json | Select-Object -First 1
if (-not $manifest) { throw 'manifest.json not found in the download.' }
if ($manifest.DirectoryName -ne $dest) {
    $inner = $manifest.DirectoryName
    Get-ChildItem $inner -Force | Move-Item -Destination $dest
    Remove-Item $inner -Recurse -Force
}
Write-Host "Installed to $dest" -ForegroundColor Green