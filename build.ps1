param(
    [switch]$RebuildIcon,
    [switch]$WarningsAsErrors
)

$ErrorActionPreference = 'Stop'
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$icoPath = Join-Path $projectDir 'app.ico'
$csPath  = Join-Path $projectDir 'MacSnip.cs'
$exePath = Join-Path $projectDir 'MacSnip.exe'

function New-AppIcon([string]$OutFile) {
    Add-Type -AssemblyName System.Drawing
    $sizes = @(16, 32, 48, 256)
    $pngBlobs = New-Object System.Collections.Generic.List[byte[]]

    foreach ($sz in $sizes) {
        $bmp = New-Object System.Drawing.Bitmap($sz, $sz, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = $null
        $path = $null
        $brush = $null
        $pen = $null
        $ms = $null
        try {
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
            $g.Clear([System.Drawing.Color]::Transparent)

            $pad = [Math]::Max(1.0, $sz * 0.06)
            $rectSize = $sz - 2.0 * $pad
            $radius = $sz * 0.24

            $path = New-Object System.Drawing.Drawing2D.GraphicsPath
            $d = $radius * 2.0
            $path.AddArc($pad, $pad, $d, $d, 180, 90)
            $path.AddArc($pad + $rectSize - $d, $pad, $d, $d, 270, 90)
            $path.AddArc($pad + $rectSize - $d, $pad + $rectSize - $d, $d, $d, 0, 90)
            $path.AddArc($pad, $pad + $rectSize - $d, $d, $d, 90, 90)
            $path.CloseFigure()

            $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
                (New-Object System.Drawing.PointF(0, 0)),
                (New-Object System.Drawing.PointF($sz, $sz)),
                [System.Drawing.Color]::FromArgb(255, 37, 99, 235),
                [System.Drawing.Color]::FromArgb(255, 14, 165, 233)
            )
            $g.FillPath($brush, $path)

            # Draw crisp white crop corners inside
            $stroke = [Math]::Max(1.6, $sz * 0.095)
            $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, $stroke)
            $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
            $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

            $m = $sz * 0.25
            $r = $sz - $m
            $arm = $sz * 0.17

            # Top-left corner
            $g.DrawLines($pen, @(
                (New-Object System.Drawing.PointF($m, ($m + $arm))),
                (New-Object System.Drawing.PointF($m, $m)),
                (New-Object System.Drawing.PointF(($m + $arm), $m))
            ))
            # Top-right corner
            $g.DrawLines($pen, @(
                (New-Object System.Drawing.PointF(($r - $arm), $m)),
                (New-Object System.Drawing.PointF($r, $m)),
                (New-Object System.Drawing.PointF($r, ($m + $arm)))
            ))
            # Bottom-left corner
            $g.DrawLines($pen, @(
                (New-Object System.Drawing.PointF($m, ($r - $arm))),
                (New-Object System.Drawing.PointF($m, $r)),
                (New-Object System.Drawing.PointF(($m + $arm), $r))
            ))
            # Bottom-right corner
            $g.DrawLines($pen, @(
                (New-Object System.Drawing.PointF(($r - $arm), $r)),
                (New-Object System.Drawing.PointF($r, $r)),
                (New-Object System.Drawing.PointF($r, ($r - $arm)))
            ))

            $ms = New-Object System.IO.MemoryStream
            $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
            $pngBlobs.Add($ms.ToArray())
        }
        finally {
            if ($ms) { $ms.Dispose() }
            if ($pen) { $pen.Dispose() }
            if ($brush) { $brush.Dispose() }
            if ($path) { $path.Dispose() }
            if ($g) { $g.Dispose() }
            if ($bmp) { $bmp.Dispose() }
        }
    }

    $fs = [System.IO.File]::Create($OutFile)
    $bw = New-Object System.IO.BinaryWriter($fs)
    try {
        $bw.Write([uint16]0)
        $bw.Write([uint16]1)
        $bw.Write([uint16]$sizes.Count)

        $offset = 6 + (16 * $sizes.Count)
        for ($i = 0; $i -lt $sizes.Count; $i++) {
            $sz = $sizes[$i]
            $blob = $pngBlobs[$i]
            $wByte = if ($sz -ge 256) { [byte]0 } else { [byte]$sz }
            $bw.Write($wByte)
            $bw.Write($wByte)
            $bw.Write([byte]0)
            $bw.Write([byte]0)
            $bw.Write([uint16]1)
            $bw.Write([uint16]32)
            $bw.Write([uint32]$blob.Length)
            $bw.Write([uint32]$offset)
            $offset += $blob.Length
        }

        foreach ($blob in $pngBlobs) {
            $bw.Write($blob)
        }

        $bw.Flush()
    }
    finally {
        $bw.Close()
        $fs.Close()
    }
}

if ($RebuildIcon -or -not (Test-Path $icoPath)) {
    Write-Host "Generating icon: $icoPath"
    New-AppIcon -OutFile $icoPath
}

if (Test-Path $exePath) {
    try {
        & $exePath --stop | Out-Null
    } catch {}

    # Wait briefly for any running instance pointing to this binary to release the file lock
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        $locked = Get-Process -Name 'MacSnip' -ErrorAction SilentlyContinue | Where-Object {
            try { $_.Path -eq $exePath } catch { $false }
        }
        if (-not $locked) {
            break
        }
        Start-Sleep -Milliseconds 100
    }
}

$sysRoot = if ($env:SystemRoot) { $env:SystemRoot } else { 'C:\Windows' }
$csc = Join-Path $sysRoot 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    $csc = Join-Path $sysRoot 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path $csc)) {
    throw "Cannot find built-in .NET Framework C# compiler (csc.exe) under $sysRoot."
}

$cscArgs = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/warn:4',
    '/codepage:65001',
    "/win32icon:$icoPath",
    '/r:System.dll',
    '/r:System.Drawing.dll',
    '/r:System.Windows.Forms.dll',
    "/out:$exePath",
    $csPath
)
if ($WarningsAsErrors) {
    $cscArgs = @('/warnaserror+') + $cscArgs
}

Write-Host "Compiling MacSnip.exe..."
& $csc @cscArgs

if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

Write-Host "Build succeeded: $exePath"
