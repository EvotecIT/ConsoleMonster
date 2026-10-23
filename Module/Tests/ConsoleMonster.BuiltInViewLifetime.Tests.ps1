Describe 'Built-in view lifetime and native grouping' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:Runtime = [Diagnostics.Process]::GetCurrentProcess().Path
        $script:ModulePath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
        $script:AdapterPath = Join-Path $PSScriptRoot 'Support/Convert-TestNativeView.ps1'

        function Invoke-BuiltInRegression {
            param([string] $Script)
            $setup = @"
`$ErrorActionPreference = 'Stop'
`$modulePath = '$($script:ModulePath.Replace("'", "''"))'
`$adapterPath = '$($script:AdapterPath.Replace("'", "''"))'
`$probePath = '$((Join-Path $PSScriptRoot 'Support/Measure-BuiltInRunspace.ps1').Replace("'", "''"))'
"@
            $body = $setup + "`ntry {`n" + $Script + "`n} catch { Write-Output (`$_ | Out-String); exit 1 }"
            $output = Invoke-TestChild -Runtime $script:Runtime -Script $body
            if ($LASTEXITCODE -ne 0) { throw ($output -join "`n") }
            $output -join "`n"
        }
    }

    It 'renders every built-in family after its initializing runspace is disposed' {
        $text = Invoke-BuiltInRegression @'
$render = [IO.File]::ReadAllText($probePath)
$first = $null
foreach ($iteration in 1, 2, 3) {
    $shell = [powershell]::Create()
    try {
        $shell.Runspace.SessionStateProxy.SetVariable('modulePath', $modulePath)
        $null = $shell.AddScript($render)
        $result = @($shell.Invoke())
        if ($shell.HadErrors -or $result.Count -ne 1) { throw "Runspace $iteration failed: $($shell.Streams.Error -join '; ')" }
        if ($null -eq $first) { $first = $result[0] }
        foreach ($family in 'Member', 'Command', 'Module', 'History') {
            if ($result[0][$family] -cne $first[$family]) { throw "$family changed after runspace disposal." }
        }
    } finally { $shell.Dispose() }
}
'RUNSPACE_LIFETIME_OK'
'@
        $text | Should -Match 'RUNSPACE_LIFETIME_OK'
    }

    It 'preserves selected native grouping for <Route>' -ForEach @(
        @{ Route = 'default'; Arguments = '@{}'; ExpectedGroups = 2; ExpectedLabel = 'OWNER GROUP' }
        @{ Route = 'named view'; Arguments = "@{ View = 'OwnerCommandView' }"; ExpectedGroups = 2; ExpectedLabel = 'OWNER GROUP' }
        @{ Route = 'explicit grouping'; Arguments = "@{ GroupBy = 'ModuleName' }"; ExpectedGroups = 1; ExpectedLabel = 'Microsoft.PowerShell.Utility' }
    ) {
        $code = @'
$builder = [Management.Automation.TableControl]::Create().GroupByProperty('Name', $null, 'OWNER GROUP').AddHeader('Left', 0, 'OwnerChosen')
$table = $builder.StartRowDefinition().AddPropertyColumn('Name').EndRowDefinition().EndTable()
$definition = [Management.Automation.ExtendedTypeDefinition]::new('System.Management.Automation.CmdletInfo')
$definition.FormatViewDefinition.Add([Management.Automation.FormatViewDefinition]::new('OwnerCommandView', $table))
$initial = [Management.Automation.Runspaces.InitialSessionState]::CreateDefault()
$initial.Formats.Clear()
$initial.Formats.Add([Management.Automation.Runspaces.SessionStateFormatEntry]::new($definition))
$shell = [powershell]::Create($initial)
try {
$null = $shell.AddScript({
param($modulePath, $adapterPath)
Import-Module $modulePath
. $adapterPath
$rows = @(Get-Command Get-Date, Get-Random)
$arguments = __ARGUMENTS__
$native = $rows | Microsoft.PowerShell.Utility\Format-Table @arguments | Out-String -Width 160
$adapted = Convert-TestNativeView -Rows $rows -Arguments $arguments | Export-ConsoleFrame -Format Text -Width 160
$label = '__LABEL__'
$pattern = [regex]::Escape($label) + $(if ($label -eq 'OWNER GROUP') { '\s*:' } else { '' })
$nativeCount = [regex]::Matches($native, $pattern).Count
$adaptedCount = [regex]::Matches($adapted, $pattern).Count
if ($nativeCount -ne __COUNT__ -or $adaptedCount -ne $nativeCount) { throw "Grouping changed: native=$nativeCount adapted=$adaptedCount`n$adapted" }
if ($adapted -notmatch 'OwnerChosen' -or $adapted -notmatch 'Get-Date' -or $adapted -notmatch 'Get-Random') { throw 'The selected native columns or values changed.' }
if ($label -eq 'OWNER GROUP' -and $adapted -match 'ModuleName\s*:') { throw 'Built-in grouping replaced the selected view.' }
'NATIVE_GROUPING_OK'
}.ToString()).AddArgument($modulePath).AddArgument($adapterPath)
$result = @($shell.Invoke())
if ($shell.HadErrors) { throw ($shell.Streams.Error -join '; ') }
$result
} finally { $shell.Dispose() }
'@
        $code = $code.Replace('__ARGUMENTS__', $Arguments).Replace('__LABEL__', $ExpectedLabel).Replace('__COUNT__', [string]$ExpectedGroups)
        Invoke-BuiltInRegression $code | Should -Match 'NATIVE_GROUPING_OK'
    }
}
