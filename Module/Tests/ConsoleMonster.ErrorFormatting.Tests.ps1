Describe 'Opt-in compact host errors preserve PowerShell contracts' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:ErrorRuntime = [Diagnostics.Process]::GetCurrentProcess().Path
        $script:ErrorModule = Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
        $script:ErrorHarness = Join-Path $PSScriptRoot 'Support/ErrorFormattingHarness.ps1'
        function Invoke-ErrorFormattingChild {
            param([string] $Body)
            $setup = "Import-Module '$($script:ErrorModule.Replace("'", "''"))' -ErrorAction Stop`n" +
                ". '$($script:ErrorHarness.Replace("'", "''"))'`n" +
                "try {`n$Body`n} catch { [Console]::Out.WriteLine((`$_ | Out-String)); exit 1 }"
            $result = Invoke-TestChild -Runtime $script:ErrorRuntime -Script $setup
            if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
            $result -join "`n"
        }
    }

    It 'keeps records, continuation, Out-String, Out-File and redirection byte-for-byte native' {
        $text = Invoke-ErrorFormattingChild @'
$ErrorActionPreference = 'Stop'
$directory = Join-Path ([IO.Path]::GetTempPath()) ('cm-errors-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $directory
try {
    try { [int]::Parse('abc') } catch { $record = $_ }
    $capture = { Get-ErrorFormattingSnapshot $record $directory }
    $before = & $capture
    Enable-ConsoleFormatting -Feature Errors
    $after = & $capture
    foreach ($name in $before.PSObject.Properties.Name) {
        if ($before.$name -cne $after.$name) { throw "Native capture changed: $name" }
    }
    Disable-ConsoleFormatting
} finally { Remove-Item -LiteralPath $directory -Recurse -Force }
'CAPTURES_NATIVE_OK'
'@
        $text | Should -Match 'CAPTURES_NATIVE_OK'
    }

    It 'keeps error records, ErrorVariable, preferences, catch and success status native' {
        $text = Invoke-ErrorFormattingChild @'
$ErrorActionPreference = 'Continue'
$errorView = $ErrorView
$writeError = Get-Command Write-Error
Enable-ConsoleFormatting -Feature Errors
$record = [Management.Automation.ErrorRecord]::new([InvalidOperationException]::new('Synthetic failure'), 'SyntheticError', 'InvalidOperation', 'synthetic-target')
$Error.Clear()
Write-Error -ErrorRecord $record -ErrorVariable held 2>$null
$success = $?
if ($success -or $held.Count -ne 1 -or $Error.Count -ne 1) { throw 'Error accounting changed.' }
if (-not [object]::ReferenceEquals($held[0], $Error[0])) { throw 'ErrorVariable and Error differ.' }
if ($held[0].FullyQualifiedErrorId -notlike 'SyntheticError*') { throw 'Error ID changed.' }
if ($held[0].TargetObject -ne 'synthetic-target') { throw 'Target changed.' }
try { Write-Error -ErrorRecord $record -ErrorAction Stop; throw 'Expected stop' } catch {
    if ($_.Exception.Message -ne 'Synthetic failure') { throw }
}
$ErrorActionPreference = 'SilentlyContinue'
$Error.Clear()
Write-Error -ErrorRecord $record
$success = $?
if ($success -or $Error.Count -ne 1) { throw 'SilentlyContinue changed.' }
if ($ErrorView -cne $errorView -or (Get-Command Write-Error).ImplementingType -ne $writeError.ImplementingType) { throw 'Native producer or ErrorView changed.' }
Disable-ConsoleFormatting
'ERROR_STATE_NATIVE_OK'
'@
        $text | Should -Match 'ERROR_STATE_NATIVE_OK'
    }

    It 'excludes Errors from All and prints its explicit Detailed profile snippet' {
        $text = Invoke-ErrorFormattingChild @'
$flags = [ConsoleMonster.PowerShell.ConsoleFormattingFeature]
if (($flags::All -band $flags::Errors) -ne 0) { throw 'Errors became implicit.' }
$snippet = Enable-ConsoleFormatting -Feature Errors -Detailed -ProfileSnippet
if ($snippet[-1] -cne 'Enable-ConsoleFormatting -Feature Errors -Detailed') { throw 'Wrong profile snippet.' }
if (Get-Command Out-Default -CommandType Function -ErrorAction Ignore) { throw 'Snippet changed the session.' }
'EXPLICIT_ONLY_OK'
'@
        $text | Should -Match 'EXPLICIT_ONLY_OK'
    }

    It 'restores the original Out-Default exactly on disable and module removal' {
        $text = Invoke-ErrorFormattingChild @'
function global:Out-Default { 'user-owned' }
$saved = ${function:global:Out-Default}.ToString()
Enable-ConsoleFormatting -Feature Errors -Force
Disable-ConsoleFormatting -Feature Errors
if (${function:global:Out-Default}.ToString() -cne $saved) { throw 'Disable did not restore the function.' }
Enable-ConsoleFormatting -Feature Errors -Force
Remove-Module ConsoleMonster
if (${function:global:Out-Default}.ToString() -cne $saved) { throw 'Removal did not restore the function.' }
'RESTORE_EXACT_OK'
'@
        $text | Should -Match 'RESTORE_EXACT_OK'
    }

    It 'maps the original invocation and folds causes through the shared compact view' {
        $text = Invoke-ErrorFormattingChild @'
try { [int]::Parse('abc') } catch { $record = $_ }
$widget = $record | Format-ConsoleError -Compact -AsWidget
$text = [ConsoleMonster.Terminal]::RenderText($widget, [ConsoleMonster.Testing.TestTerminal]::Profile(120))
if ($text -notmatch '^Error: Exception calling' -or $text -notmatch '\^+' -or $text -notmatch '\[int\]::Parse') { throw 'Missing compact invocation.' }
if ($text -notmatch '\+1 inner exception' -or $text -match 'Caused by') { throw 'Causes were not folded.' }
if ($widget.ShowStack) { throw 'Compact default exposed stacks.' }
if ($widget.DetailsHint) {
    $footer = '+1 inner exception ' + [char]0x00b7 + ' ' + $widget.DetailsHint
    if (-not $text.EndsWith($footer)) { throw 'Folded cause and available details hint were not combined.' }
} elseif (-not $text.EndsWith('+1 inner exception')) { throw 'Folded cause changed without a details command.' }
'SHARED_VIEW_OK'
'@
        $text | Should -Match 'SHARED_VIEW_OK'
    }

    It 'leaves job output native when Errors is enabled inside the job' {
        $text = Invoke-ErrorFormattingChild @'
$module = (Get-Module ConsoleMonster).Path
$script = { param($module, $enabled)
    Import-Module $module
    if ($enabled) { Enable-ConsoleFormatting -Feature Errors }
    try { [int]::Parse('abc') } catch { $_ | Out-String -Width 100 }
}
$jobs = @(Start-Job $script -ArgumentList $module,$false; Start-Job $script -ArgumentList $module,$true)
try {
    $null = $jobs | Wait-Job
    $before = Receive-Job $jobs[0]
    $after = Receive-Job $jobs[1]
    if ($before -cne $after) { throw 'Job text changed.' }
} finally { $jobs | Remove-Job -Force }
'JOB_NATIVE_OK'
'@
        $text | Should -Match 'JOB_NATIVE_OK'
    }

    It 'marks the original extent beyond the native display excerpt on both runtimes' {
        $text = Invoke-ErrorFormattingChild @'
$expression = "[int]::Parse('" + ('abc' * 45) + "')"
$record = & ([scriptblock]::Create('try { ' + $expression + ' } catch { $_ }'))
$widget = $record | Format-ConsoleError -Compact -AsWidget
$text = [ConsoleMonster.Terminal]::RenderText($widget, [ConsoleMonster.Testing.TestTerminal]::Profile(200))
$marker = @($text -split "`n" | Where-Object { $_.TrimStart().StartsWith('^') })
if ($marker.Count -ne 1 -or $marker[0].Trim().Length -ne $expression.Length) { throw 'Original single-line extent was shortened.' }
$multi = "[int]::Parse(`n    'abc'`n)"
$record = & ([scriptblock]::Create('try { ' + $multi + ' } catch { $_ }'))
$widget = $record | Format-ConsoleError -Compact -AsWidget
$text = [ConsoleMonster.Terminal]::RenderText($widget, [ConsoleMonster.Testing.TestTerminal]::Profile(200))
$marker = @($text -split "`n" | Where-Object { $_.TrimStart().StartsWith('^') })
if ($marker.Count -ne 1 -or $marker[0].Trim().Length -ne '[int]::Parse('.Length) { throw 'Multiline extent did not end at the source line boundary.' }
'ORIGINAL_EXTENT_OK'
'@
        $text | Should -Match 'ORIGINAL_EXTENT_OK'
    }
}
