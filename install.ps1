param(
    [switch]$NoAutostart,
    [switch]$NoLaunch,
    [switch]$HideTray
)

$ErrorActionPreference = 'Stop'
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$buildScript = Join-Path $projectDir 'build.ps1'
$exePath = Join-Path $projectDir 'MacSnip.exe'

# 1. Build executable (stops running instance automatically if needed)
& $buildScript

# 2. Register Startup shortcut in user's Startup folder (unless -NoAutostart)
$startupDir = [Environment]::GetFolderPath('Startup')
$lnkPath = Join-Path $startupDir 'MacSnip.lnk'

if (-not $NoAutostart) {
    $wsh = $null
    $sc = $null
    try {
        $wsh = New-Object -ComObject WScript.Shell
        $sc = $wsh.CreateShortcut($lnkPath)
        $sc.TargetPath = $exePath
        $sc.Arguments = '--startup'
        $sc.WorkingDirectory = $projectDir
        $sc.Description = 'MacSnip for Windows (Ctrl+Shift+4/5)'
        $sc.IconLocation = "$exePath,0"
        $sc.Save()
    }
    finally {
        if ($sc) { [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($sc) }
        if ($wsh) { [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($wsh) }
    }

    # 3. Ensure Windows StartupApproved marks MacSnip.lnk as Enabled
    $approvedKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder'
    if (-not (Test-Path $approvedKey)) {
        New-Item -Path $approvedKey -Force | Out-Null
    }
    [byte[]]$enabledBytes = 0x02,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00
    Set-ItemProperty -Path $approvedKey -Name 'MacSnip.lnk' -Value $enabledBytes -ErrorAction SilentlyContinue
}

# 4. Persist tray visibility preference if -HideTray is requested
if ($HideTray) {
    $appSettingsKey = 'HKCU:\Software\MacSnip'
    if (-not (Test-Path $appSettingsKey)) {
        New-Item -Path $appSettingsKey -Force | Out-Null
    }
    Set-ItemProperty -Path $appSettingsKey -Name 'ShowTrayIcon' -Value 0 -Type DWord -ErrorAction SilentlyContinue
}

# 5. Launch immediately in background (unless -NoLaunch)
if (-not $NoLaunch) {
    $launchArgs = @()
    if ($HideTray) {
        $launchArgs += '--hide-tray'
    }
    if ($launchArgs.Count -gt 0) {
        Start-Process -FilePath $exePath -WorkingDirectory $projectDir -ArgumentList $launchArgs
    } else {
        Start-Process -FilePath $exePath -WorkingDirectory $projectDir
    }
    Start-Sleep -Milliseconds 600

    $proc = Get-Process -Name 'MacSnip' -ErrorAction SilentlyContinue | Where-Object {
        try { $_.Path -eq $exePath } catch { $true }
    } | Select-Object -First 1

    if ($proc) {
        Write-Host "Installed and running! (PID: $($proc.Id))"
        if (-not $NoAutostart) {
            Write-Host "Startup shortcut: $lnkPath -> $exePath"
        }
        if ($HideTray) {
            Write-Host "Tray icon is hidden. Launch MacSnip.exe again or run 'MacSnip.exe --show-tray' to restore it."
        }
    } else {
        throw "MacSnip.exe failed to stay running. Check error.log in $projectDir or %LOCALAPPDATA%\MacSnip."
    }
} else {
    Write-Host "Build and configuration complete."
    if (-not $NoAutostart) {
        Write-Host "Startup shortcut: $lnkPath -> $exePath"
    }
}
