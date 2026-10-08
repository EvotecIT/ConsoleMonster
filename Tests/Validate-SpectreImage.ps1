[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $ModulePath,
    [Parameter(Mandatory)][string] $TemporaryDirectory
)

$ErrorActionPreference = 'Stop'
Import-Module -Name $ModulePath -Force

$Source = [OfficeIMO.Drawing.OfficeRasterImage]::new(16, 8)
for ($Y = 0; $Y -lt 8; $Y++) {
    for ($X = 0; $X -lt 16; $X++) {
        $Alpha = if ($X -lt 4) { 0 } else { 255 }
        $Source.SetPixel($X, $Y, [OfficeIMO.Drawing.OfficeColor]::FromRgba([byte]($X * 16), [byte]($Y * 32), 64, $Alpha))
    }
}
$ImageBytes = [OfficeIMO.Drawing.OfficePngWriter]::Encode($Source)
[void][IO.Directory]::CreateDirectory($TemporaryDirectory)
$ImagePath = Join-Path $TemporaryDirectory 'fixture[rgba].png'
[IO.File]::WriteAllBytes($ImagePath, $ImageBytes)
$Writer = [IO.StringWriter]::new()
$Settings = [Spectre.Console.AnsiConsoleSettings]::new()
$Settings.Out = [Spectre.Console.AnsiConsoleOutput]::new($Writer)
$Settings.Ansi = [Spectre.Console.AnsiSupport]::Yes
$Settings.ColorSystem = [Spectre.Console.ColorSystemSupport]::TrueColor
$PreviousConsole = [Spectre.Console.AnsiConsole]::Console
$Console = [Spectre.Console.AnsiConsole]::Create($Settings)
$Console.Profile.Width = 40
$Console.Profile.Capabilities.Unicode = $true
[Spectre.Console.AnsiConsole]::Console = $Console
$PreviousTemp = $env:TEMP
$PreviousTmp = $env:TMP
$env:TEMP = $TemporaryDirectory
$env:TMP = $TemporaryDirectory
$ServerJob = $null
try {
    $Canvas = [ConsoleMonster.SpectreImageRenderer]::Load($ImagePath, 8)
    if ($Canvas.Width -ne 8 -or $Canvas.Height -ne 4) {
        throw 'MaxWidth does not preserve the image aspect ratio.'
    }
    New-SpectreImage -FilePath $ImagePath -MaxWidth 8
    if ($Writer.ToString().Length -eq 0) {
        throw 'Local image produced no terminal output.'
    }
    $TerminalOutput = $Writer.ToString()
    $PlainOutput = [regex]::Replace($TerminalOutput, [string][char]27 + '\[[0-9;]*m', '')
    $Lines = @($PlainOutput -split '\r?\n' | Where-Object { $_.Length -gt 0 })
    if ($Lines.Count -ne 2 -or $Lines[0].Length -ne 8 -or $Lines[1].Length -ne 8 -or -not $Lines[0].StartsWith(' ')) {
        throw 'Rendered size or transparent pixels differ from the image contract.'
    }
    [IO.File]::WriteAllText((Join-Path $TemporaryDirectory 'terminal-local.ansi'), $TerminalOutput)
    $Writer.GetStringBuilder().Clear() | Out-Null
    $Console.Profile.Width = 6
    $Canvas = [ConsoleMonster.SpectreImageRenderer]::Load($ImagePath)
    if ($Canvas.Width -ne 6 -or $Canvas.Height -ne 3) {
        throw 'The image does not fit a compact terminal.'
    }
    $Console.Profile.Width = 40
    $InvalidPath = Join-Path $TemporaryDirectory 'invalid.bin'
    [IO.File]::WriteAllText($InvalidPath, 'This is not an image.')
    $Rejected = $false
    try {
        New-SpectreImage -FilePath $InvalidPath
    } catch {
        $Rejected = $true
    }
    if (-not $Rejected) { throw 'Invalid local image was accepted.' }

    $PortProbe = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $PortProbe.Start()
    $Port = $PortProbe.LocalEndpoint.Port
    $PortProbe.Stop()
    $ServerJob = Start-Job -ArgumentList $Port, $ImageBytes -ScriptBlock {
        param($Port, [byte[]] $ImageBytes)
        $Listener = [Net.HttpListener]::new()
        $Listener.Prefixes.Add("http://localhost:$Port/")
        $Listener.Start()
        Write-Output 'Ready'
        try {
            for ($Index = 0; $Index -lt 3; $Index++) {
                $Context = $Listener.GetContext()
                $Bytes = $ImageBytes
                if ($Context.Request.Url.AbsolutePath -eq '/invalid') {
                    $Bytes = [Text.Encoding]::UTF8.GetBytes('Not an image.')
                } elseif ($Context.Request.Url.AbsolutePath -eq '/missing') {
                    $Context.Response.StatusCode = 404
                    $Bytes = [byte[]]@()
                }
                $Context.Response.ContentLength64 = $Bytes.Length
                $Context.Response.OutputStream.Write($Bytes, 0, $Bytes.Length)
                $Context.Response.Close()
            }
        } finally {
            $Listener.Close()
        }
    }
    $Ready = $false
    for ($Attempt = 0; $Attempt -lt 100 -and -not $Ready; $Attempt++) {
        $Ready = @(Receive-Job -Job $ServerJob) -contains 'Ready'
        if (-not $Ready) { Start-Sleep -Milliseconds 100 }
    }
    if (-not $Ready) { throw 'Local HTTP fixture did not start.' }
    New-SpectreImage -Uri "http://localhost:$Port/image" -MaxWidth 8
    if ($Writer.ToString().Length -eq 0) { throw 'Downloaded image produced no terminal output.' }
    $Rejected = $false
    try {
        New-SpectreImage -Uri "http://localhost:$Port/invalid"
    } catch {
        $Rejected = $true
    }
    if (-not $Rejected) { throw 'Invalid downloaded image was accepted.' }
    New-SpectreImage -Uri "http://localhost:$Port/missing" -WarningVariable DownloadWarning
    if (-not $DownloadWarning) { throw 'Failed image download did not report a warning.' }
    $Files = @(Get-ChildItem -LiteralPath $TemporaryDirectory -File)
    if ($Files.Count -ne 3) { throw 'An image download left a temporary file behind.' }
    Write-Output "Spectre image artifact validation passed on PowerShell $($PSVersionTable.PSVersion)."
} finally {
    [Spectre.Console.AnsiConsole]::Console = $PreviousConsole
    $env:TEMP = $PreviousTemp
    $env:TMP = $PreviousTmp
    if ($ServerJob) {
        Stop-Job -Job $ServerJob -ErrorAction SilentlyContinue
        Remove-Job -Job $ServerJob -Force
    }
    $Writer.Dispose()
}
