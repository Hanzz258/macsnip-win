param(
    [switch]$RemoveBinaries
)

$ErrorActionPreference = 'Stop'
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$exePath = Join-Path $projectDir 'MacSnip.exe'
$icoPath = Join-Path $projectDir 'app.ico'
$logPath = Join-Path $projectDir 'error.log'
$logOldPath = Join-Path $projectDir 'error.log.old'
$startupDir = [Environment]::GetFolderPath('Startup')
$lnkPath = Join-Path $startupDir 'MacSnip.lnk'
$approvedKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder'
$appSettingsKey = 'HKCU:\Software\MacSnip'

if (Test-Path $exePath) {
    try {
        & $exePath --stop | Out-Null
    } catch {}
    Start-Sleep -Milliseconds 200
}

Get-Process -Name 'MacSnip' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

if (Test-Path $lnkPath) {
    Remove-Item -Path $lnkPath -Force
    Write-Host "Removed Startup shortcut: $lnkPath"
}

if (Test-Path $approvedKey) {
    Remove-ItemProperty -Path $approvedKey -Name 'MacSnip.lnk' -ErrorAction SilentlyContinue
}

if (Test-Path $appSettingsKey) {
    Remove-Item -Path $appSettingsKey -Recurse -Force -ErrorAction SilentlyContinue
}

if ($RemoveBinaries) {
    foreach ($file in @($exePath, $icoPath, $logPath, $logOldPath)) {
        if (Test-Path $file) {
            Remove-Item -Path $file -Force
            Write-Host "Removed: $file"
        }
    }
}

Write-Host "MacSnip stopped and uninstalled from Startup."
