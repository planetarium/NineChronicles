#!/usr/bin/env pwsh
param (
  [Parameter(Mandatory=$true)][string]$version
)

$InformationPreference = "Continue"

function New-TemporaryDirectory {
  $parent = [System.IO.Path]::GetTempPath()
  [string] $name = [System.Guid]::NewGuid()
  New-Item -ItemType Directory -Path (Join-Path $parent $name)
}

$9cRoot = git rev-parse --show-toplevel
$PackagePath = Join-Path $9cRoot "nekoyume" "Assets" "Packages"
$LibplanetVersion = $version
$LibplanetDownloadUrlBase = "https://github.com/planetarium/libplanet/releases/download"

Write-Information "Libplanet version: $LibplanetVersion"
foreach ($file in Get-ChildItem $PackagePath -Filter Libplanet*.dll) {
  $packageName = $file.Basename
  Write-Information "Downloading $packageName $LibplanetVersion..."
  $nupkgName = "$packageName.$LibplanetVersion"
  $nupkgFile = "$nupkgName.nupkg"
  $nupkgUrl = "$LibplanetDownloadUrlBase/$LibplanetVersion/$nupkgFile"
  $tmpDir = New-TemporaryDirectory
  Push-Location "$tmpDir"
  Invoke-WebRequest -Uri $nupkgUrl -OutFile $nupkgFile
  Expand-Archive $nupkgFile
  Pop-Location
  $assembliesDir = Join-Path $tmpDir $nupkgName "lib" "netstandard2.0"
  foreach ($f in Get-ChildItem $assembliesDir) {
    Move-Item $f -Force -Destination (Join-Path $PackagePath $f.Name)
  }
}

