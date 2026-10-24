Describe 'Everyday native formatting contracts' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:EverydayModule = Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
        $script:EverydayFixture = Join-Path $PSScriptRoot 'Fixtures/EverydayViews/EverydayViews.psd1'
        $script:EverydayRuntime = [Diagnostics.Process]::GetCurrentProcess().Path
        function Invoke-EverydayChild {
            param([string] $Script)
            $setup = @(
                '$ErrorActionPreference = ''Stop'''
                "Import-Module '$($script:EverydayModule.Replace("'", "''"))'"
                "Import-Module '$($script:EverydayFixture.Replace("'", "''"))'"
                "try {`n$Script`n} catch { [Console]::Out.WriteLine((`$_ | Microsoft.PowerShell.Utility\Out-String)); exit 1 }; exit 0"
            ) -join "`n"
            $lines = Invoke-TestChild -Runtime $script:EverydayRuntime -Script $setup
            $exit = $LASTEXITCODE
            if ($exit -ne 0) { throw ($lines -join "`n") }
            $lines -join "`n"
        }
    }

    It 'matches native Wide column fitting for <LayoutName>, including boundary widths and groups' -ForEach @(
        @{ LayoutName = 'default'; Layout = @{} }
        @{ LayoutName = 'one column'; Layout = @{ Column = 1 } }
        @{ LayoutName = 'two columns'; Layout = @{ Column = 2 } }
        @{ LayoutName = 'three columns'; Layout = @{ Column = 3 } }
        @{ LayoutName = 'autosize'; Layout = @{ AutoSize = $true } }
    ) {
        $layoutJson = $Layout | ConvertTo-Json -Compress
        $text = Invoke-EverydayChild (@'
$layout = @{}
($layoutJson = '__LAYOUT__' | ConvertFrom-Json).PSObject.Properties | ForEach-Object { $layout[$_.Name] = $_.Value }
foreach ($width in 20, 21, 40, 41, 80) {
    $profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed($width, 40, 'None')
    [ConsoleMonster.Terminal]::Profile = $profile
    foreach ($length in 5, 16, 20) {
        $values = 1..7 | ForEach-Object { [pscustomobject]@{ Name = "$_-" + ('x' * $length); Group = if ($_ -lt 4) { 'A' } else { 'B' } } }
        foreach ($grouped in $false, $true) {
            $parameters = @{} + $layout
            if ($grouped) { $parameters.GroupBy = 'Group' }
            $records = @($values | Microsoft.PowerShell.Utility\Format-Wide Name @parameters)
            $native = $records | Microsoft.PowerShell.Utility\Out-String -Width $width
            $widget = $records | Out-ConsoleFormatCommand -AsWidget
            $actual = [ConsoleMonster.Terminal]::RenderText($widget, $profile)
            $native = (($native -replace "`r", '').Trim("`n") -split "`n" | ForEach-Object { $_.TrimEnd() }) -join "`n"
            $actual = (($actual -replace "`r", '').Trim("`n") -split "`n" | ForEach-Object { $_.TrimEnd() }) -join "`n"
            if ($native -cne $actual) { throw "Wide fit differs: width=$width length=$length grouped=$grouped`nNative:`n$native`nActual:`n$actual" }
        }
    }
}
'WIDE_FIT_OK'
'@.Replace('__LAYOUT__', $layoutJson.Replace("'", "''")))
        $text | Should -Match 'WIDE_FIT_OK'
    }

    It 'preserves exact native records, captures and file bytes for Format-<Kind>' -ForEach @(
        @{ Kind = 'Table'; Arguments = "-Property Name, State -GroupBy Site -AutoSize -Wrap -HideTableHeaders" }
        @{ Kind = 'List'; Arguments = "-Property N*, @{Label='Health';Expression={ `$_.State }} -GroupBy Site" }
        @{ Kind = 'Wide'; Arguments = '-Property Name -Column 2 -GroupBy Site' }
    ) {
        $text = Invoke-EverydayChild (@'
$directory = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $directory
try {
    function Invoke-Cases($prefix) {
        $command = if ($prefix) { $prefix + [char]92 + 'Format-__KIND__' } else { 'Format-__KIND__' }
        $body = 'Get-EverydayValue | ' + $command + ' __ARGS__'
        $invoke = [scriptblock]::Create($body)
        $records = @(& $invoke)
        $explicit = @(& ([scriptblock]::Create($command + ' __ARGS__ -InputObject (Get-EverydayValue | Select-Object -First 1)')))
        $alias = switch ('__KIND__') { 'Table' { 'ft' }; 'List' { 'fl' }; 'Wide' { 'fw' } }
        $aliasText = & ([scriptblock]::Create('Get-EverydayValue | ' + $alias + ' __ARGS__ | Out-String -Width 80'))
        $text = & ([scriptblock]::Create($body + ' | Out-String -Width 80'))
        $continuation = @(& ([scriptblock]::Create($body + ' | ForEach-Object { $_.GetType().FullName }'))) -join ','
        $selection = @(& ([scriptblock]::Create($body + ' | Out-String -Stream -Width 80 | Select-String alpha'))) -join ','
        $out = Join-Path $directory 'out.txt'
        $redirect = Join-Path $directory 'redirect.txt'
        $append = Join-Path $directory 'append.txt'
        & ([scriptblock]::Create($body + ' | Out-File -LiteralPath $out -Width 80'))
        & ([scriptblock]::Create($body + ' > $redirect'))
        Set-Content -LiteralPath $append -Value 'prefix'
        & ([scriptblock]::Create($body + ' >> $append'))
        $global:captured = @()
        & ([scriptblock]::Create($body + ' -OutVariable global:captured | Out-Null'))
        [pscustomobject]@{
            Records = [Management.Automation.PSSerializer]::Serialize($records, 20)
            ExplicitInput = [Management.Automation.PSSerializer]::Serialize($explicit, 20)
            AliasText = $aliasText
            Text = $text
            Continuation = $continuation
            Selection = $selection
            OutFile = [Convert]::ToBase64String([IO.File]::ReadAllBytes($out))
            Redirect = [Convert]::ToBase64String([IO.File]::ReadAllBytes($redirect))
            Append = [Convert]::ToBase64String([IO.File]::ReadAllBytes($append))
            OutVariable = [Management.Automation.PSSerializer]::Serialize($global:captured, 20)
        }
    }
    $before = Invoke-Cases 'Microsoft.PowerShell.Utility'
    Enable-ConsoleFormatting -Feature FormatCommands
    $after = Invoke-Cases ''
    foreach ($name in $before.PSObject.Properties.Name) {
        if ($before.$name -cne $after.$name) { throw "Native mismatch: $name" }
    }
    Disable-ConsoleFormatting
    if ((Get-Command 'Format-__KIND__').CommandType -ne 'Cmdlet') { throw 'Proxy was not removed' }
    'EXACT_NATIVE_OK'
} finally { Remove-Item -LiteralPath $directory -Recurse }
'@.Replace('__KIND__', $Kind).Replace('__ARGS__', $Arguments.Replace("'", "''")))
        $text | Should -Match 'EXACT_NATIVE_OK'
    }

    It 'maps native named views and parameters without changing expression evaluation' {
        $text = Invoke-EverydayChild @'
$native = @{}
foreach ($kind in 'Table', 'List', 'Wide') {
    $command = Get-Command "Microsoft.PowerShell.Utility\Format-$kind"
    $native[$kind] = Get-EverydayValue | & $command -View "Everyday$kind" | Out-String
}
Enable-ConsoleFormatting -Feature FormatCommands
foreach ($kind in 'Table', 'List', 'Wide') {
    $command = Get-Command "Format-$kind"
    $actual = Get-EverydayValue | & $command -View "Everyday$kind" | Out-String
    if ($native[$kind] -cne $actual) { throw "Named view changed: $kind" }
    $builtIn = Get-Command "Microsoft.PowerShell.Utility\Format-$kind"
    if (@(Compare-Object @($builtIn.Parameters.Keys | Sort-Object) @($command.Parameters.Keys | Sort-Object)).Count) { throw "Parameters changed: $kind" }
}
$script:evaluations = 0
Get-EverydayValue | Microsoft.PowerShell.Utility\Format-Table @{Name='Name';Expression={ $script:evaluations++; $_.Name }} | Out-String | Out-Null
$nativeEvaluations = $script:evaluations
$script:evaluations = 0
Get-EverydayValue | Format-Table @{Name='Name';Expression={ $script:evaluations++; $_.Name }} | Out-ConsoleFormatCommand -AsWidget | Export-ConsoleFrame -Format Text
if ($script:evaluations -ne $nativeEvaluations) { throw "Expressions evaluated $script:evaluations times instead of native $nativeEvaluations" }
'PARAMETERS_AND_VIEWS_OK'
'@
        $text | Should -Match 'PARAMETERS_AND_VIEWS_OK'
        $text | Should -Match 'alpha'
    }

    It 'keeps FormatCommands outside All and restores custom functions on disable and removal' {
        $text = Invoke-EverydayChild @'
Enable-ConsoleFormatting -Feature All
if ((Get-Command Format-Table).CommandType -ne 'Cmdlet') { throw 'All intercepted Format-Table' }
Disable-ConsoleFormatting
function global:Format-Table { 'custom-table' }
$original = (Get-Command Format-Table).Definition
Enable-ConsoleFormatting -Feature FormatCommands -WarningAction SilentlyContinue
if ((Get-Command Format-Table).Definition -cne $original) { throw 'Custom function was overwritten' }
Enable-ConsoleFormatting -Feature FormatCommands -Force
Disable-ConsoleFormatting
if ((Get-Command Format-Table).Definition -cne $original) { throw 'Custom function not restored' }
Enable-ConsoleFormatting -Feature FormatCommands -Force
Remove-Module ConsoleMonster
if ((Get-Command Format-Table).Definition -cne $original) { throw 'Module removal did not restore function' }
'RESTORATION_OK'
'@
        $text | Should -Match 'RESTORATION_OK'
    }

    It 'maps native left center right and default numeric alignment to table columns' {
        $text = Invoke-EverydayChild @'
$row = [pscustomobject]@{ A = 1; B = 2; C = 3; Numeric = 4 }
$widget = $row | Microsoft.PowerShell.Utility\Format-Table -Property @{
    Name='Left'; Expression='A'; Alignment='Left'
}, @{ Name='Center'; Expression='B'; Alignment='Center' }, @{
    Name='Right'; Expression='C'; Alignment='Right'
}, Numeric | Out-ConsoleFormatCommand -AsWidget
$actual = @($widget.Columns | ForEach-Object { [string]$_.Alignment }) -join ','
if ($actual -ne 'Left,Center,Right,Right') { throw "Native alignment mapping was $actual" }
'ALIGNMENT_OK'
'@
        $text | Should -Match 'ALIGNMENT_OK'
    }

    It 'restores global Format-<Kind> slots while local functions shadow them' -ForEach @(
        @{ Kind = 'Table' }
        @{ Kind = 'List' }
        @{ Kind = 'Wide' }
    ) {
        $text = Invoke-EverydayChild (@'
$name = 'Format-__KIND__'
Set-Item ("Function:\global:" + $name) { 'GLOBAL_ORIGINAL' }
& {
    Set-Item ("Function:local:" + $name) { 'LOCAL_ORIGINAL' }
    Enable-ConsoleFormatting -Feature FormatCommands -Force
}
Disable-ConsoleFormatting
if ((& $name) -ne 'GLOBAL_ORIGINAL') { throw 'Local function replaced the global backup' }
Enable-ConsoleFormatting -Feature FormatCommands -Force
& {
    Set-Item ("Function:local:" + $name) { 'LOCAL_SHADOW' }
    Disable-ConsoleFormatting
    if ((& $name) -ne 'LOCAL_SHADOW') { throw 'Disable changed a local shadow' }
}
if ((& $name) -ne 'GLOBAL_ORIGINAL') { throw 'Disable under a shadow left the global proxy installed' }
Enable-ConsoleFormatting -Feature FormatCommands -Force
Set-Item ("Function:\global:" + $name) { # ConsoleMonster FormatCommands proxy
    'USER_REPLACEMENT'
}
Disable-ConsoleFormatting
if ((& $name) -ne 'USER_REPLACEMENT') { throw 'Disable replaced a new user function' }
'GLOBAL_SLOTS_OK'
'@.Replace('__KIND__', $Kind))
        $text | Should -Match 'GLOBAL_SLOTS_OK'
    }

    It 'restores the global Out-Default slot used by FormatCommands even with local shadows' {
        $text = Invoke-EverydayChild @'
function global:Out-Default { 'GLOBAL_OUT_DEFAULT' }
& {
    function Out-Default { 'LOCAL_OUT_DEFAULT' }
    Enable-ConsoleFormatting -Feature FormatCommands -Force
}
& {
    function Out-Default { 'LOCAL_DISABLE_SHADOW' }
    Disable-ConsoleFormatting
}
if (${function:global:Out-Default}.ToString() -notmatch 'GLOBAL_OUT_DEFAULT') { throw 'Out-Default global slot was not restored' }
'GLOBAL_OUT_DEFAULT_OK'
'@
        $text | Should -Match 'GLOBAL_OUT_DEFAULT_OK'
    }

    It 'keeps jobs and serialized job results exactly native when formatting is enabled in the worker' {
        $text = Invoke-EverydayChild @'
$modulePath = (Get-Module ConsoleMonster).Path
$fixturePath = (Get-Module EverydayViews).Path
foreach ($kind in 'Table', 'List', 'Wide') {
    $job = Start-Job -ArgumentList $modulePath, $fixturePath, $kind -ScriptBlock {
        param($module, $fixture, $kind)
        Import-Module $module
        Import-Module $fixture
        $before = Get-EverydayValue | & "Microsoft.PowerShell.Utility\Format-$kind" -Property Name | Out-String
        Enable-ConsoleFormatting -Feature FormatCommands
        $after = Get-EverydayValue | & "Format-$kind" -Property Name | Out-String
        if ($before -cne $after) { throw "Job output changed: $kind" }
        Get-EverydayValue | & "Format-$kind" -Property Name
    }
    try {
        $records = @($job | Wait-Job | Receive-Job -ErrorAction Stop)
        if (@($records | Where-Object { -not ($_.PSTypeNames -match 'Deserialized.Microsoft.PowerShell.Commands.Internal.Format') }).Count) { throw 'Job emitted a non-native record' }
    } finally { Remove-Job $job -Force }
}
'JOBS_NATIVE_OK'
'@
        $text | Should -Match 'JOBS_NATIVE_OK'
    }

    It 'keeps native output through a local process remoting session' {
        $text = Invoke-EverydayChild @'
$remote = [Management.Automation.Runspaces.RunspaceFactory]::CreateOutOfProcessRunspace($null)
$ps = [powershell]::Create()
try {
    $remote.Open()
    $ps.Runspace = $remote
    $code = {
        param($module, $fixture)
        $ErrorActionPreference = 'Stop'
        Import-Module $module
        Import-Module $fixture
        foreach ($kind in 'Table', 'List', 'Wide') {
            $expected = Get-EverydayValue | & "Microsoft.PowerShell.Utility\Format-$kind" -Property Name | Out-String
            Enable-ConsoleFormatting -Feature FormatCommands
            $actual = Get-EverydayValue | & "Format-$kind" -Property Name | Out-String
            if ($expected -cne $actual) { throw "Remote native output changed: $kind" }
        }
        Get-EverydayValue | Format-Table -Property Name
    }
    $records = @($ps.AddScript($code.ToString()).AddArgument((Get-Module ConsoleMonster).Path).AddArgument((Get-Module EverydayViews).Path).Invoke())
    if ($ps.HadErrors) { throw ($ps.Streams.Error -join "`n") }
    if ($records.Count -lt 4 -or @($records | Where-Object { -not ($_.PSTypeNames -match 'Deserialized.Microsoft.PowerShell.Commands.Internal.Format') }).Count) { throw 'Remoting returned non-native records' }
    'REMOTING_NATIVE_OK'
} finally { $ps.Dispose(); $remote.Dispose() }
'@
        $text | Should -Match 'REMOTING_NATIVE_OK'
    }

    It 'isolates command restoration between runspaces' {
        $text = Invoke-EverydayChild @'
$first = [powershell]::Create()
$second = [powershell]::Create()
try {
    $setup = {
        param($module, $label)
        Import-Module $module
        Set-Item Function:\global:Format-Table ([scriptblock]::Create("'$label'"))
        Enable-ConsoleFormatting -Feature FormatCommands -Force
    }
    $null = $first.AddScript($setup.ToString()).AddArgument((Get-Module ConsoleMonster).Path).AddArgument('FIRST').Invoke()
    $null = $second.AddScript($setup.ToString()).AddArgument((Get-Module ConsoleMonster).Path).AddArgument('SECOND').Invoke()
    $first.Commands.Clear()
    $a = $first.AddScript('Disable-ConsoleFormatting; Format-Table').Invoke()
    if ("$a" -ne 'FIRST') { throw 'First runspace was not restored' }
    $second.Commands.Clear()
    $b = $second.AddScript('(Get-Command Format-Table).Definition -match ''ConsoleMonster FormatCommands proxy''').Invoke()
    if ("$b" -ne 'True') { throw 'First runspace disabled the second' }
    $second.Commands.Clear()
    $b = $second.AddScript('Disable-ConsoleFormatting; Format-Table').Invoke()
    if ("$b" -ne 'SECOND') { throw 'Second runspace was not restored' }
    'RUNSPACES_OK'
} finally { $first.Dispose(); $second.Dispose() }
'@
        $text | Should -Match 'RUNSPACES_OK'
    }

    It 'keeps transcript body and Format-* records native' {
        $text = Invoke-EverydayChild @'
$path = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N') + '.txt')
try {
    $null = Start-Transcript -Path $path
    'BASELINE_BEGIN' | Microsoft.PowerShell.Core\Out-Default
    foreach ($kind in 'Table', 'List', 'Wide') {
        Get-EverydayValue | & "Microsoft.PowerShell.Utility\Format-$kind" -Property Name | Microsoft.PowerShell.Core\Out-Default
    }
    'BASELINE_END' | Microsoft.PowerShell.Core\Out-Default
    Enable-ConsoleFormatting -Feature FormatCommands
    'ENABLED_BEGIN' | Microsoft.PowerShell.Core\Out-Default
    foreach ($kind in 'Table', 'List', 'Wide') {
        $expected = Get-EverydayValue | & "Microsoft.PowerShell.Utility\Format-$kind" -Property Name | Out-String
        $actual = Get-EverydayValue | & "Format-$kind" -Property Name | Out-String
        if ($expected -cne $actual) { throw "Transcribed formatting changed: $kind" }
        Get-EverydayValue | & "Format-$kind" -Property Name | Out-Default
    }
    'ENABLED_END' | Microsoft.PowerShell.Core\Out-Default
    $null = Stop-Transcript
    $body = Get-Content -LiteralPath $path -Raw
    if ($body -notmatch 'alpha' -or $body -match '[╭╰│]') { throw 'Transcript does not contain native output' }
    $before = [regex]::Match($body, '(?s)BASELINE_BEGIN\r?\n(.*?)BASELINE_END').Groups[1].Value
    $after = [regex]::Match($body, '(?s)ENABLED_BEGIN\r?\n(.*?)ENABLED_END').Groups[1].Value
    if (-not $before -or $before -cne $after) { throw 'Transcript native body changed' }
    'TRANSCRIPT_NATIVE_OK'
} finally {
    try { Stop-Transcript -ErrorAction Stop | Out-Null } catch { }
    Remove-Item -LiteralPath $path -ErrorAction SilentlyContinue
}
'@
        $text | Should -Match 'TRANSCRIPT_NATIVE_OK'
    }
}
