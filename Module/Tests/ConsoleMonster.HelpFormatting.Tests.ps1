Describe 'Opt-in native help formatting contracts' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:HelpModule = Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
        $script:HelpFixture = Join-Path $PSScriptRoot 'Fixtures/HelpFormatting/HelpFormatting.psd1'
        $script:HelpCapture = Join-Path $PSScriptRoot 'Support/Get-HelpFormattingCapture.ps1'
        $script:HelpRuntime = [Diagnostics.Process]::GetCurrentProcess().Path
        function Invoke-HelpFormattingChild {
            param([string] $Script)
            $body = @(
                '$ErrorActionPreference = ''Stop'''
                "Import-Module '$script:HelpModule'"
                "Import-Module '$script:HelpFixture'"
                ". '$script:HelpCapture'"
                'function global:Out-HelpFormattingTestPager { param($PagerArguments) process { $_ } }'
                '$env:PAGER = ''Out-HelpFormattingTestPager'''
                "try {`n$Script`n} catch { [Console]::Error.WriteLine((`$_ | Out-String)); exit 1 }; exit 0"
            ) -join "`n"
            $result = Invoke-TestChild -Runtime $script:HelpRuntime -Script $body
            if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
            $result -join "`n"
        }
    }

    It 'selects the native section set for <View>' -ForEach @(
        @{ View = 'Default'; Parameters = $false; Examples = $false; Types = $false; Remarks = $true }
        @{ View = 'Question'; Parameters = $false; Examples = $false; Types = $false; Remarks = $true }
        @{ View = 'Detailed'; Parameters = $true; Examples = $true; Types = $false; Remarks = $true }
        @{ View = 'Examples'; Parameters = $false; Examples = $true; Types = $false; Remarks = $false }
        @{ View = 'Full'; Parameters = $true; Examples = $true; Types = $true; Remarks = $true }
    ) {
        $text = Invoke-HelpFormattingChild (@'
$arguments = @{ Name = 'Get-HelpFormattingValue' }
$view = '__VIEW__'
if ($view -eq 'Question') { $help = Get-HelpFormattingValue -? }
else {
    if ($view -ne 'Default') { $arguments[$view] = $true }
    $help = Microsoft.PowerShell.Core\Get-Help @arguments
}
$reader = (Get-Command Show-ConsoleHelp).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.PSHelp')
$method = $reader.GetMethod('CreateView', [Reflection.BindingFlags]'Static,NonPublic')
$command = (Get-Command Get-HelpFormattingValue).PSObject.BaseObject
$widget = $method.Invoke($null, @($help, $command, $false, $false, $true))
($widget | Export-ConsoleFrame -Format Text -Width 120) -join "`n"
'@.Replace('__VIEW__', $View))
        if ($View -eq 'Examples') {
            $text | Should -Not -Match 'Reads a synthetic formatting value|HELP_DESCRIPTION|(?m)^Syntax\s*$|example\.invalid'
        } else {
            $text | Should -Match 'Reads a synthetic formatting value|HELP_DESCRIPTION'
            $text | Should -Match '(?m)^Syntax\s*$'
            $text | Should -Match 'example\.invalid/help-formatting'
        }
        if ($Parameters) { $text | Should -Match 'HELP_NAME_DESCRIPTION' }
        else { $text | Should -Not -Match 'HELP_NAME_DESCRIPTION' }
        if ($Examples) { $text | Should -Match 'HELP_EXAMPLE_REMARKS' }
        else { $text | Should -Not -Match 'HELP_EXAMPLE_REMARKS' }
        if ($Types) { $text | Should -Match '(?m)^Inputs\s*$'; $text | Should -Match 'HELP_NOTES' }
        else { $text | Should -Not -Match '(?m)^Inputs\s*$|HELP_NOTES' }
        if ($Remarks) { $text | Should -Match 'Get-Help Get-HelpFormattingValue -Examples / -Detailed / -Full' }
    }

    It 'preserves native captures and file bytes for <Invocation>' -ForEach @(
        @{ Invocation = 'Get-Help Get-HelpFormattingValue' }
        @{ Invocation = 'Get-Help Get-HelpFormattingValue -Full' }
        @{ Invocation = 'Get-Help Get-HelpFormattingValue -Detailed' }
        @{ Invocation = 'Get-Help Get-HelpFormattingValue -Examples' }
        @{ Invocation = 'Get-Help Get-HelpFormattingValue -Parameter *' }
        @{ Invocation = 'help Get-HelpFormattingValue -Examples' }
        @{ Invocation = 'Get-HelpFormattingValue -?' }
        @{ Invocation = 'Get-Help about_ConsoleMonsterHelpFormatting' }
        @{ Invocation = 'Get-Help about_ConsoleMonsterMarkdown' }
    ) {
        $text = Invoke-HelpFormattingChild (@'
$directory = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $directory
try {
    $before = Get-HelpFormattingCapture '__INVOCATION__' $directory
    Enable-ConsoleFormatting -Feature Help
    $after = Get-HelpFormattingCapture '__INVOCATION__' $directory
    foreach ($name in $before.PSObject.Properties.Name) {
        if ($before.$name -cne $after.$name) { throw "Native help changed: $name" }
    }
    Disable-ConsoleFormatting
    'NATIVE_HELP_CAPTURE_OK'
} finally { Remove-Item -LiteralPath $directory -Recurse }
'@.Replace('__INVOCATION__', $Invocation))
        $text | Should -Match 'NATIVE_HELP_CAPTURE_OK'
    }

    It 'excludes Help from All and restores the exact commands on disable and module removal' {
        $text = Invoke-HelpFormattingChild @'
$nativeHelp = (Get-Command help).Definition
$nativeGetHelp = Get-Command Get-Help
Enable-ConsoleFormatting -Feature All
if ((Get-Command help).Definition -cne $nativeHelp) { throw 'All opted into Help' }
Disable-ConsoleFormatting
foreach ($remove in $false, $true) {
    Enable-ConsoleFormatting -Feature Help
    Enable-ConsoleFormatting -Feature Help
    if (-not [object]::ReferenceEquals((Get-Command Get-Help), $nativeGetHelp)) { throw 'Get-Help was replaced' }
    if ($remove) { Remove-Module ConsoleMonster } else { Disable-ConsoleFormatting }
    if ((Get-Command help).Definition -cne $nativeHelp) { throw 'Native help was not restored exactly' }
    if ((Get-Command Out-Default).CommandType -ne 'Cmdlet') { throw 'Out-Default was not restored' }
}
'RESTORE_HELP_OK'
'@
        $text | Should -Match 'RESTORE_HELP_OK'
    }

    It 'restores forced custom help functions and preserves later user replacements' {
        $text = Invoke-HelpFormattingChild @'
function global:help { param([string]$Name) "custom:$Name" }
$custom = (Get-Command help).Definition
Enable-ConsoleFormatting -Feature Help -WarningVariable warnings
if ($warnings.Count -ne 1 -or (Get-Command help).Definition -cne $custom) { throw 'Custom help was overwritten without Force' }
Enable-ConsoleFormatting -Feature Help -Force
if (-not (Get-Command help).Parameters.ContainsKey('Full')) { throw 'Forced Help lost the native parameter surface' }
Disable-ConsoleFormatting
if ((Get-Command help).Definition -cne $custom) { throw 'Custom help was not restored' }
Enable-ConsoleFormatting -Feature Help -Force
function global:help { 'new owner' }
Disable-ConsoleFormatting
if ((help) -cne 'new owner') { throw 'A later user function was overwritten' }
'CUSTOM_HELP_OK'
'@
        $text | Should -Match 'CUSTOM_HELP_OK'
    }

    It 'preserves feature independence and leaves user help aliases alone' {
        $text = Invoke-HelpFormattingChild @'
$native = (Get-Command help).Definition
Enable-ConsoleFormatting -Feature Help, FormatCommands
Disable-ConsoleFormatting -Feature Help
if ((Get-Command help).Definition -cne $native -or (Get-Command Format-Table).CommandType -ne 'Function') { throw 'Selective disable changed another feature' }
Enable-ConsoleFormatting -Feature Help
Disable-ConsoleFormatting -Feature All
if ((Get-Command help).Definition -ceq $native) { throw 'Disabling All also disabled Help' }
Disable-ConsoleFormatting
Set-Alias -Name help -Value Get-Item -Scope Global
Enable-ConsoleFormatting -Feature Help -Force -WarningVariable warning
if ($warning.Count -ne 1 -or (Get-Alias help).Definition -ne 'Get-Item' -or (Get-Command Out-Default).CommandType -ne 'Cmdlet') { throw 'A user help alias was overwritten' }
Remove-Item Alias:help
'INDEPENDENT_HELP_OK'
'@
        $text | Should -Match 'INDEPENDENT_HELP_OK'
    }

    It 'keeps pipeline property binding, errors, background jobs and thread jobs native' {
        $text = Invoke-HelpFormattingChild @'
$names = [pscustomobject]@{ Name='Get-HelpFormattingValue' }, [pscustomobject]@{ Name='Get-HelpFormattingShort' }
$before = $names | Get-Help -Full | Out-String -Width 100
$errorId = try { Get-Help Get-HelpFormattingValue -Full -Examples } catch { $_.FullyQualifiedErrorId }
Enable-ConsoleFormatting -Feature Help
$after = $names | Get-Help -Full | Out-String -Width 100
if ($before -cne $after) { throw 'Pipeline help property binding changed' }
$afterError = try { Get-Help Get-HelpFormattingValue -Full -Examples } catch { $_.FullyQualifiedErrorId }
if ($afterError -cne $errorId) { throw 'Native error changed' }
foreach ($kind in 'Start-Job', 'Start-ThreadJob') {
    if (-not (Get-Command $kind -ErrorAction Ignore)) { "UNAVAILABLE=$kind"; continue }
    $job = & $kind -ArgumentList $modulePath,$fixturePath -ScriptBlock {
        param($modulePath,$fixturePath)
        Import-Module $modulePath
        Import-Module $fixturePath
        $before = Get-Help Get-HelpFormattingValue -Full | Out-String -Width 100
        Enable-ConsoleFormatting -Feature Help
        $after = Get-Help Get-HelpFormattingValue -Full | Out-String -Width 100
        if ($before -cne $after) { throw 'Job help changed' }
        Get-Help Get-HelpFormattingValue -Full
    }
    try {
        $result = @($job | Wait-Job -Timeout 45 | Receive-Job -ErrorAction Stop)
        if ($job.State -ne 'Completed' -or $result.Count -ne 1 -or $result[0].Name -ne 'Get-HelpFormattingValue') { throw "Failed $kind" }
    } finally { $job | Remove-Job -Force }
}
'JOBS_AND_BINDING_OK'
'@.Replace('$modulePath,$fixturePath -ScriptBlock', "'$script:HelpModule','$script:HelpFixture' -ScriptBlock")
        $text | Should -Match 'JOBS_AND_BINDING_OK'
    }

    It 'preserves native help through a real local process remoting session' {
        $text = Invoke-HelpFormattingChild @'
$remote = [Management.Automation.Runspaces.RunspaceFactory]::CreateOutOfProcessRunspace($null)
$ps = [powershell]::Create()
try {
    $remote.Open()
    $ps.Runspace = $remote
    $code = {
        param($module,$fixture)
        $ErrorActionPreference = 'Stop'
        Import-Module $module
        Import-Module $fixture
        $before = Get-Help Get-HelpFormattingValue -Full | Out-String -Width 100
        Enable-ConsoleFormatting -Feature Help
        $after = Get-Help Get-HelpFormattingValue -Full | Out-String -Width 100
        if ($before -cne $after) { throw 'Remoting text changed' }
        Get-Help Get-HelpFormattingValue -Full
    }
    $records = @($ps.AddScript($code.ToString()).AddArgument((Get-Module ConsoleMonster).Path).AddArgument((Get-Module HelpFormatting).Path).Invoke())
    if ($ps.HadErrors) { throw ($ps.Streams.Error -join "`n") }
    if ($records.Count -ne 1 -or $records[0].Name -ne 'Get-HelpFormattingValue' -or $records[0].PSTypeNames -notcontains 'Deserialized.MamlCommandHelpInfo') { throw 'Remoting did not return native help' }
    'REMOTE_HELP_OK'
} finally { $ps.Dispose(); $remote.Dispose() }
'@
        $text | Should -Match 'REMOTE_HELP_OK'
    }
}
