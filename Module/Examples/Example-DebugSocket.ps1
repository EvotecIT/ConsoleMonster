# Drives a ConsoleMonster app the way a browser test drives a page. The app runs in another PowerShell process on a
# headless terminal (no window) with the debug socket switched on; this script finds the socket through the app's
# discovery file, types into the app, waits for its answer, reads the screen and the control tree, and quits it.
# Agents use the same two functions to test cmon, the TestimoX CLI or any other ConsoleMonster app.
Import-Module ConsoleMonster

function Connect-ConsoleDebugSocket {
    <#
    .SYNOPSIS
    Connects to the debug socket of a ConsoleMonster process and says hello with its token.
    .PARAMETER ProcessId
    The process started with CONSOLEMONSTER_HEADLESS and CONSOLEMONSTER_DEBUG_SOCKET.
    .PARAMETER TimeoutSeconds
    How long to wait for the process to publish its discovery file.
    #>
    param([Parameter(Mandatory)][int] $ProcessId, [int] $TimeoutSeconds = 30)
    $folder = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'ConsoleMonster/Debug'
    $file = Join-Path $folder "$ProcessId.json"
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while (-not (Test-Path -LiteralPath $file)) {
        if ((Get-Date) -gt $deadline) { throw "Process $ProcessId did not start a debug socket." }
        Start-Sleep -Milliseconds 100
    }
    $discovery = Get-Content -LiteralPath $file -Raw | ConvertFrom-Json
    $client = New-Object System.Net.Sockets.TcpClient
    $client.Connect('127.0.0.1', [int] $discovery.port)
    $stream = $client.GetStream()
    $connection = [pscustomobject] @{
        Client = $client
        Reader = New-Object System.IO.StreamReader -ArgumentList $stream
        Writer = New-Object System.IO.StreamWriter -ArgumentList $stream
    }
    $connection.Writer.NewLine = "`n"
    $null = Invoke-ConsoleDebugRequest -Connection $connection -Request @{ cmd = 'hello'; token = $discovery.token }
    $connection
}

function Invoke-ConsoleDebugRequest {
    <#
    .SYNOPSIS
    Sends one request (hello, screen, tree, keys, text, resize, click or wait) and returns the reply.
    .PARAMETER Connection
    The connection from Connect-ConsoleDebugSocket.
    .PARAMETER Request
    The request, for example @{ cmd = 'keys'; keys = 'Down Enter' }.
    #>
    param([Parameter(Mandatory)] $Connection, [Parameter(Mandatory)][hashtable] $Request)
    $Connection.Writer.WriteLine(($Request | ConvertTo-Json -Compress -Depth 5))
    $Connection.Writer.Flush()
    $reply = $Connection.Reader.ReadLine() | ConvertFrom-Json
    if (-not $reply.ok) { throw "The debug socket refused '$($Request.cmd)': $($reply.message)" }
    $reply
}

# The app under test: a name box and a greeting, as any app could have.
$app = {
    Import-Module ConsoleMonster
    Show-ConsoleApp -Content {
        New-ConsoleTextInput -Key Name -Label 'Name'
        New-ConsoleBinding -WidgetId Greeting -Content { param($State) "Hello $($State.Name)" }
    }
}
$encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($app.ToString()))
$log = Join-Path ([IO.Path]::GetTempPath()) "cm-debug-example-$PID.log"

# The child inherits these for its start only: a 70x14 headless terminal and a socket on any free port.
$saved = $env:CONSOLEMONSTER_HEADLESS, $env:CONSOLEMONSTER_DEBUG_SOCKET, $env:CONSOLEMONSTER_INPUT
$env:CONSOLEMONSTER_HEADLESS = '70x14'
$env:CONSOLEMONSTER_DEBUG_SOCKET = '0'
$env:CONSOLEMONSTER_INPUT = $null
try {
    $child = Start-Process -FilePath (Get-Process -Id $PID).Path -ArgumentList '-NoLogo', '-NoProfile', '-EncodedCommand', $encoded -NoNewWindow -PassThru -RedirectStandardError $log
} finally {
    $env:CONSOLEMONSTER_HEADLESS, $env:CONSOLEMONSTER_DEBUG_SOCKET, $env:CONSOLEMONSTER_INPUT = $saved
}

try {
    $debug = Connect-ConsoleDebugSocket -ProcessId $child.Id
    $null = Invoke-ConsoleDebugRequest $debug @{ cmd = 'text'; text = 'Ada' }
    $answer = Invoke-ConsoleDebugRequest $debug @{ cmd = 'wait'; text = 'Hello Ada'; timeout = 10000 }
    if (-not $answer.found) { throw 'The app did not greet Ada.' }
    $screen = Invoke-ConsoleDebugRequest $debug @{ cmd = 'screen' }
    $tree = Invoke-ConsoleDebugRequest $debug @{ cmd = 'tree' }
    Write-ConsoleText "Focused control: $($tree.focus)"
    Write-ConsoleText ($screen.text.TrimEnd())
    $null = Invoke-ConsoleDebugRequest $debug @{ cmd = 'keys'; keys = 'Escape' }
    Wait-Process -Id $child.Id -Timeout 15
} finally {
    if ($debug) { $debug.Client.Close() }
    if (-not $child.HasExited) { Stop-Process -Id $child.Id -Force }
    Remove-Item -LiteralPath $log -ErrorAction SilentlyContinue
}
