#!/usr/bin/env pwsh -ExecutionPolicy RemoteSigned -Scope CurrentUser

Param(
  [Parameter(Mandatory=$True)]
  [String] $outDir,
  [Parameter(Mandatory=$True)]
  [String] $platform,
  [Parameter(Mandatory=$True)]
  [String] $gameDir,
  [Parameter(Mandatory=$True)]
  [String] $launcherDir,
  [Parameter(Mandatory=$True)]
  [String] $privateKey,
  [Parameter(Mandatory=$True)]
  [String] $timestamp
)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
  Write-Error "python, a prerequisite, is not installed on the system."
  exit 1
} elseif (-not ($(python --version) -match '^Python (\d+)\.(\d+)\.\d+$')) {
  Write-Error "The python command seems not to refer to a CPython executable."
  exit 1
} elseif ([int]$Matches[1] -lt 3 -or
           ([int]$Matches[1] -eq 3 -and [int]$Matches[2] -lt 7)) {
  Write-Error "The installed Python is older than 3.8."
  exit 1
}

if ($PSScriptRoot -eq $null)
{
  $PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
}

$venvDir = "$PSScriptRoot\.pack-venv"
$scriptDir = "$PSScriptRoot\..\..\tools\pack"

if (-not (Test-Path $venvDir)) {
  python -m venv "$venvDir"
}

&"$venvDir\Scripts\pip" install -r "$scriptDir\requirements.txt"
&"$venvDir\Scripts\python" "$scriptDir\pack.py" `
  $outDir `
  $platform `
  $gameDir `
  $launcherDir `
  $privateKey `
  $timestamp
exit $LastExitCode
