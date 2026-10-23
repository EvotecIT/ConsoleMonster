<# .SYNOPSIS
Checks first-use grid mode restoration in a fresh, hidden Windows console owned by the test host.
#>
param([string] $ModulePath, [string] $AssemblyPath, [string] $Receipt, [string] $Entry, [string] $Scenario)
$ErrorActionPreference = 'Stop'
try {
    Import-Module $ModulePath
    if ($PSEdition -eq 'Desktop') {
        $assembly = [Reflection.Assembly]::LoadFrom($AssemblyPath)
    } else {
        $context = [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext([ConsoleMonster.Interactive.Grid].Assembly)
        $assembly = $context.LoadFromAssemblyPath($AssemblyPath)
    }
    $verify = $assembly.GetType('ConsoleMonster.TestHost.GridOutputModeProbe', $true).GetMethod('Verify')
    . (Join-Path $PSScriptRoot 'Invoke-GridViewSession.ps1')
    $body = @'
$options = [ConsoleMonster.Interactive.InteractiveOptions]::new()
$options.Input = [ConsoleMonster.Input.ScriptedInput]::new('__KEYS__')
$options.Mode = [ConsoleMonster.Interactive.InteractionMode]::Rich
[ConsoleMonster.Interactive.InteractiveOptions]::Default = $options
$row = [pscustomobject]@{ Name = 'alpha'; Id = 1 }
Enable-ConsoleFormatting -Feature GridView
if ('__ENTRY__' -eq 'Fallback') { $picked = @($row | Out-GridView -PassThru -ErrorAction Stop) }
elseif ('__SCENARIO__' -eq 'Unavailable') {
    $failed = $false
    try { $row | Show-ConsoleGrid -Mode None -ErrorAction Stop } catch { $failed = $true }
    if (-not $failed) { throw 'The unavailable grid did not fail.' }
    $picked = @()
} else { $picked = @($row | Show-ConsoleGrid -ErrorAction Stop) }
if ('__SCENARIO__' -eq 'Accepted') {
    if ($picked.Count -ne 1 -or -not [object]::ReferenceEquals($picked[0], $row)) { throw 'The selected row changed.' }
} elseif ($picked.Count -ne 0) { throw 'A cancelled grid returned a row.' }
Disable-ConsoleFormatting
if ([ConsoleMonster.Input.ConsoleInput]::IsAcquired) { throw 'The grid input lease survived.' }
'SELECTION_OK'
'@
    $keys = if ($Scenario -eq 'Accepted') { 'F10' } else { 'Escape' }
    $body = $body.Replace('__KEYS__', $keys).Replace('__ENTRY__', $Entry).Replace('__SCENARIO__', $Scenario)
    $action = [Action] { if ((Invoke-GridViewSession -ModulePath $ModulePath -Script $body) -ne 'SELECTION_OK') { throw 'Missing selection receipt.' } }
    $null = $verify.Invoke($null, @($action))
    [IO.File]::WriteAllText($Receipt, 'GRID_MODE_OK ' + $Entry + ' ' + $Scenario + ' 3->3')
} catch {
    [IO.File]::WriteAllText($Receipt, $_.Exception.ToString())
    exit 1
}
