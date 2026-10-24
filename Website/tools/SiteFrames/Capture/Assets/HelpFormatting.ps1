param(
    [Parameter(Mandatory = $true)]
    [string] $ExamplePath,
    [switch] $Formatted,
    [switch] $Paged,
    [string] $DiagnosticPath
)

# Capture-only setup: the published example is executed whole and shown verbatim.
# Resolve the host sink after installing the adapter, as the next interactive prompt does.
if ($Formatted) {
    Enable-ConsoleFormatting -Feature Help
}
if ($Paged) {
    $keys = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('wait:300 Space wait:150 b wait:150 q Space')
    $keys.RealTime = $true
    ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::Override = $keys
}
try {
    & { . $ExamplePath } | Out-Default
    if ($Paged -and (([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::Override.Remaining -ne 1 -or
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired)) {
        throw 'Help pager did not navigate forward/back, quit and release input.'
    }
} catch {
    if ($DiagnosticPath) {
        [IO.File]::WriteAllText($DiagnosticPath, ($_ | Out-String))
    }
    throw
} finally {
    Disable-ConsoleFormatting
    ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::Override = $null
}
