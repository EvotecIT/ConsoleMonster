Describe 'Show-Console, default formatting and the taskbar indicator' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        # A child process with redirected output (plain text) and en-US culture.
        function Invoke-ViewChild {
            param([Parameter(Mandatory)][string] $Script, [hashtable] $Environment = @{})
            $lines = [System.Collections.Generic.List[string]]::new()
            $lines.Add("foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CONSOLEMONSTER_LANGUAGE', 'CONSOLEMONSTER_TASKBAR', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'COLUMNS') { [Environment]::SetEnvironmentVariable(`$n, `$null) }")
            foreach ($name in $Environment.Keys) {
                $lines.Add("[Environment]::SetEnvironmentVariable('$name', '$($Environment[$name])')")
            }
            $lines.Add("[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')")
            $lines.Add("[System.Threading.Thread]::CurrentThread.CurrentUICulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')")
            $lines.Add("Import-Module '$($script:ModulePath)' -Force")
            $lines.Add($Script)
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            # PowerShell 7 may colour its own table headers (PSStyle); the assertions read the plain text.
            (($output | ForEach-Object { "$_" }) -join "`n") -replace "$([char] 27)\[[0-9;]*m", ''
        }
    }

    It 'picks a view for each shape' {
        $text = Invoke-ViewChild -Script @'
'== one'
[pscustomobject] @{ Name = 'srv01'; Up = $true } | Show-Console -Width 60
'== many'
1..3 | ForEach-Object { [pscustomobject] @{ Name = "srv0$_"; Count = $_ } } | Show-Console -Width 60
'== tree'
[pscustomobject] @{ Name = 'root'; Children = @([pscustomobject] @{ Name = 'leaf'; Children = @() }) } | Show-ConsoleTree -ChildrenProperty Children -Width 60
'== numbers'
3, 9 | Show-Console -Width 40
'== cards'
1..2 | ForEach-Object { [pscustomobject] @{ Name = "srv0$_"; Count = $_ } } | Show-ConsoleList -Width 60
'@
        # One object is a record: its title (Name) and its other fields.
        $text | Should -Match 'srv01\s*\n\s+Up\s+(?:.\s+)?yes'
        $text | Should -Match 'Name\s+.\s+Count'
        $text | Should -Match 'root\s*\n\s+.+leaf'
        $text | Should -Match '9'
        ($text -split '== cards')[1] | Should -Match 'Count\s+1'
    }

    It 'leaves redirected output, pipelines and Out-String unchanged and Disable removes the Out-Default function' {
        $text = Invoke-ViewChild -Script @'
$before = Get-Item $PSHOME | Out-String
# The header line of PowerShell's own view on this platform (Windows: Mode LastWriteTime Length Name; Unix: UnixMode User ...).
$beforeLines = @($before -split "`r?`n")
for ($i = 1; $i -lt $beforeLines.Count; $i++) { if ($beforeLines[$i] -match '^\s*-{2,}') { "HEADER=$($beforeLines[$i - 1].Trim())"; break } }
$pipeBefore = @(Get-ChildItem $PSHOME -File | Select-Object -First 3 | ForEach-Object Name) -join ','
Enable-ConsoleFormatting
"ENABLED=$([bool] (Get-Command Out-Default -CommandType Function -ErrorAction Ignore))"
$after = Get-Item $PSHOME | Out-String
$pipeAfter = @(Get-ChildItem $PSHOME -File | Select-Object -First 3 | ForEach-Object Name) -join ','
"SAMESTRING=$($before -eq $after)"
"SAMEPIPE=$($pipeBefore -eq $pipeAfter)"
Get-Item $PSHOME
Disable-ConsoleFormatting
"DISABLED=$(-not (Get-Command Out-Default -CommandType Function -ErrorAction Ignore))"
'@
        $text | Should -Match 'ENABLED=True'
        $text | Should -Match 'SAMESTRING=True'
        $text | Should -Match 'SAMEPIPE=True'
        $text | Should -Match 'DISABLED=True'
        # Redirected: Get-Item still prints PowerShell's own view (its own header on this platform), not a ConsoleMonster table.
        $header = (($text -split "`n") | Where-Object { $_ -like 'HEADER=*' } | Select-Object -First 1).Substring(7)
        $header | Should -Not -BeNullOrEmpty
        @($text -split "`n" | Where-Object { $_ -notlike 'HEADER=*' -and $_.Trim() -eq $header }).Count | Should -BeGreaterThan 0
        $text | Should -Not -Match '[┌╭]'
    }

    It 'keeps an existing Out-Default function unless -Force is used' {
        $text = Invoke-ViewChild -Script @'
function global:Out-Default { process { $_ | Microsoft.PowerShell.Core\Out-Default } }
Enable-ConsoleFormatting -WarningVariable w -WarningAction SilentlyContinue
"KEPT=$((Get-Command Out-Default -CommandType Function).Definition -notmatch 'Out-ConsoleDefault')"
"WARNED=$($w.Count -gt 0)"
'@
        $text | Should -Match 'KEPT=True'
        $text | Should -Match 'WARNED=True'
    }

    It 'renders registered types through Out-ConsoleDefault and passes the rest on' {
        $text = Invoke-ViewChild -Script @'
Register-ConsoleView -TypeName 'Contoso.Server' -View Table -Property Name, @{ Name = 'State'; Expression = { $_.State }; Presenter = 'StatusWord' }
$servers = 1..2 | ForEach-Object { $o = [pscustomobject] @{ Name = "srv0$_"; State = 'Running' }; $o.PSObject.TypeNames.Insert(0, 'Contoso.Server'); $o }
@($servers) + @('a plain string') | Out-ConsoleDefault -Force
Unregister-ConsoleView -TypeName 'Contoso.Server'
'== built-in'
$servers | Out-ConsoleDefault -Force
Enable-ConsoleFormatting -Feature Output -NoBuiltIn
'== native'
$servers | Out-ConsoleDefault -Force
'@
        $text | Should -Match 'srv01.+Running'
        $text | Should -Match 'a plain string'
        $builtIn, $native = ($text -split '== built-in')[1] -split '== native'
        # Removing a custom view restores the generic record view; opting out restores PowerShell's native table.
        $builtIn | Should -Match '[|│] srv01\s+[|│] Running\s+[|│]'
        $builtIn | Should -Not -Match '\[PASS\]'
        $native | Should -Match 'Name\s+State\s*\n-+\s+-+'
    }

    It 'preserves native table views for typed records unless a ConsoleMonster view is explicitly registered' {
        $text = Invoke-ViewChild -Script @'
$path = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString() + '.format.ps1xml')
$xml = '<Configuration><ViewDefinitions><View><Name>NativeRecord</Name><ViewSelectedBy><TypeName>Contoso.NativeRecord</TypeName></ViewSelectedBy><TableControl><TableHeaders><TableColumnHeader><Label>Native heading</Label></TableColumnHeader></TableHeaders><TableRowEntries><TableRowEntry><TableColumnItems><TableColumnItem><ScriptBlock>"native:" + $_.Name</ScriptBlock></TableColumnItem></TableColumnItems></TableRowEntry></TableRowEntries></TableControl></View></ViewDefinitions></Configuration>'
try {
    [IO.File]::WriteAllText($path, $xml)
    $row = [pscustomobject] @{ Name = 'srv01'; Internal = 'private-data' }
    $row.PSObject.TypeNames.Insert(0, 'Contoso.NativeRecord')
    Enable-ConsoleFormatting -Feature Output
    $row | Out-ConsoleDefault -Force
    Update-FormatData -PrependPath $path -ErrorAction Stop
    '== native'
    $row | Out-ConsoleDefault -Force
    Register-ConsoleView -TypeName System.Management.Automation.PSCustomObject -Property @{ Label = 'Chosen'; Expression = { 'explicit-view' } }
    '== explicit'
    $row | Out-ConsoleDefault -Force
} finally {
    Disable-ConsoleFormatting
    Remove-Item -LiteralPath $path
}
'@
        $fallback, $native = $text -split '== native'
        $native, $explicit = $native -split '== explicit'
        $fallback | Should -Match 'private-data'
        $native | Should -Match 'Native heading\s*\n-+'
        $native | Should -Match 'native:srv01'
        $native | Should -Not -Match 'Internal|private-data'
        $explicit | Should -Match 'Chosen'
        $explicit | Should -Match 'explicit-view'
        $explicit | Should -Not -Match 'native:srv01|private-data'
    }

    It 'selects type-specific and default PowerShell table rows for every object' {
        $text = Invoke-ViewChild -Script @'
$path = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString() + '.format.ps1xml')
$xml = '<Configuration><ViewDefinitions><View><Name>ReviewConditional</Name><ViewSelectedBy><TypeName>Review.Conditional</TypeName></ViewSelectedBy><TableControl><TableHeaders><TableColumnHeader><Label>Result</Label></TableColumnHeader></TableHeaders><TableRowEntries><TableRowEntry><EntrySelectedBy><TypeName>Review.Special</TypeName></EntrySelectedBy><TableColumnItems><TableColumnItem><PropertyName>Special</PropertyName></TableColumnItem></TableColumnItems></TableRowEntry><TableRowEntry><TableColumnItems><TableColumnItem><PropertyName>Default</PropertyName></TableColumnItem></TableColumnItems></TableRowEntry></TableRowEntries></TableControl></View></ViewDefinitions></Configuration>'
try {
    [IO.File]::WriteAllText($path, $xml)
    Update-FormatData -PrependPath $path -ErrorAction Stop
    $values = @(
        [pscustomobject] @{ Kind = 'special'; Special = 'selected'; Conditional = 'wrong'; Default = 'wrong' }
        [pscustomobject] @{ Kind = 'default'; Special = 'wrong'; Conditional = 'wrong'; Default = 'fallback' }
    )
    foreach ($value in $values) { $value.PSObject.TypeNames.Insert(0, 'Review.Conditional') }
    $values[0].PSObject.TypeNames.Add('Review.Special')
    '== native'
    $values | Format-Table | Out-String
    '== console'
    $values | Show-ConsoleTable -AsString
} finally {
    Remove-Item -LiteralPath $path
}
'@
        $sections = $text -split '== console'
        $sections.Count | Should -Be 2
        foreach ($section in $sections) {
            $section | Should -Match 'selected'
            $section | Should -Match 'fallback'
            $section | Should -Not -Match 'wrong'
        }
    }

    It 'keeps case-distinct dictionary columns and resolves an exact property name' {
        $text = Invoke-ViewChild -Script @'
$data = [System.Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
$data.Add('Name', 'first')
$data.Add('name', 'second')
'== both'
Show-ConsoleTable -InputObject $data -Property Name, name -AsString
'== lower'
Show-ConsoleTable -InputObject $data -Property name -AsString
'== grouped'
$data.Add('Size', 7)
Show-ConsoleTable -InputObject $data -Property Name, name, Size -GroupBy Name -AsString
'@
        $sections = $text -split '== lower'
        $sections[0] | Should -Match 'Name.+name'
        $sections[0] | Should -Match 'first.+second'
        $grouped = $sections[1] -split '== grouped'
        $grouped[0] | Should -Match 'second'
        $grouped[0] | Should -Not -Match 'first'
        $grouped[1] | Should -Match 'first'
        $grouped[1] | Should -Match 'second'
        $grouped[1] | Should -Match 'Size'
    }

    It 'takes over a shaper-only registration and keeps an existing explicit view' {
        $text = Invoke-ViewChild -Script @'
Register-ConsoleView -TypeName 'Review.Shape' -Shaper { [ordered] @{ Shaped = 'replacement' } }
$value = [pscustomobject] @{ Original = 'unshaped' }
$value.PSObject.TypeNames.Insert(0, 'Review.Shape')
$value | Out-ConsoleDefault -Force
Register-ConsoleView -TypeName 'Review.Shape' -View Table -Property @{ Name = 'OnlyCustom'; Expression = { 'kept' } }
Register-ConsoleView -TypeName 'Review.Shape' -Shaper { [ordered] @{ Shaped = 'updated' } }
$value | Out-ConsoleDefault -Force
'@
        $text | Should -Match 'replacement'
        $text | Should -Not -Match 'unshaped'
        $text | Should -Match 'OnlyCustom'
        $text | Should -Match 'kept'
    }

    It 'omits shortcut aliases and prints a profile snippet without changing anything' {
        $text = Invoke-ViewChild -Script @'
"FCT=$((Get-Alias fct -ErrorAction Ignore).Definition) FCL=$((Get-Alias fcl -ErrorAction Ignore).Definition)"
$snippet = @(Enable-ConsoleFormatting -ProfileSnippet -NoBuiltIn)
"LINES=$($snippet.Count)"
$snippet
"SHIM=$([bool] (Get-Command Out-Default -CommandType Function -ErrorAction Ignore))"
'@
        $text | Should -Match 'FCT= FCL='
        $text | Should -Match 'LINES=3'
        $text | Should -Match '(?m)^Import-Module ConsoleMonster$'
        $text | Should -Match '(?m)^Enable-ConsoleFormatting -NoBuiltIn$'
        $text | Should -Match 'SHIM=False'
    }

    It 'shows commands and modules with the built-in views' {
        $text = Invoke-ViewChild -Script @'
Get-Command Get-Item, Get-ChildItem, gci | Show-ConsoleTable -Width 120
Get-Module ConsoleMonster | Show-ConsoleTable -Width 120
'@
        $text | Should -Match 'Name\s+\S\s+CommandType\s+\S\s+Version\s+\S\s+Source'
        $text | Should -Match 'Get-Item\s+\S\s+\[Cmdlet\]'
        $text | Should -Match 'gci -> Get-ChildItem\s+\S\s+\[Alias\]'
        $text | Should -Match 'Name\s+\S\s+Version\s+\S\s+ModuleType\s+\S\s+Commands\s+\S\s+Path'
    }

    It 'shows events, addresses and volumes with the built-in views on Windows' -Skip:(-not ($IsWindows -or $PSVersionTable.PSEdition -eq 'Desktop')) {
        $text = Invoke-ViewChild -Script @'
Get-WinEvent -LogName System -MaxEvents 2 -ErrorAction Ignore | Show-ConsoleTable -Width 140
Get-Volume -ErrorAction Ignore | Where-Object DriveLetter | Select-Object -First 1 | Show-ConsoleTable -Width 140
'@
        $text | Should -Match 'TimeCreated\s+\S\s+Id\s+\S\s+Level\s+\S\s+ProviderName\s+\S\s+Message'
        $text | Should -Match 'DriveLetter\s+\S.*Health\s+\S\s+Free\s+\S\s+Size'
    }

    It 'Watch-Console prints every run without a console, and a failed run does not stop it' {
        $text = Invoke-ViewChild -Script @'
$script:n = 0
Watch-Console { $script:n++; if ($script:n -eq 2) { throw 'second run failed' }; [pscustomobject] @{ Run = $script:n } } -Count 3 -PollIntervalSeconds 0.1 -Title 'demo'
'END'
'@
        $text | Should -Match 'demo\s+#1'
        $text | Should -Match 'demo\s+#3'
        $text | Should -Match 'second run failed'
        $text | Should -Match 'END'
    }

    It 'Watch-Console shows the command''s own error for a stopped run, not the exception that carried it' {
        $text = Invoke-ViewChild -Script @'
# A path that cannot exist on any platform: a missing child of the temp folder.
$missing = Join-Path ([System.IO.Path]::GetTempPath()) ('cm-missing-' + [guid]::NewGuid().ToString('N') + [System.IO.Path]::DirectorySeparatorChar + 'missing.txt')
Watch-Console { Get-Item -LiteralPath $missing -ErrorAction Stop } -Count 1 -Title 'demo'
'@
        $text | Should -Match 'ItemNotFoundException'
        $text | Should -Not -Match 'ActionPreferenceStopException'
    }

    It 'Watch-Console piped into another command prints into the pipeline and collects the other streams' {
        $text = Invoke-ViewChild -Script @'
$text = Watch-Console { Write-Host 'host text'; Write-Warning 'careful'; Write-Error 'broken'; 42 } -Count 2 -PollIntervalSeconds 0.1 -Title 'demo' | Out-String
"RUNS=$(([regex]::Matches($text, 'demo\s+#\d')).Count)"
"ERRORS=$($text -match '1 error')"
"NOTES=$($text -match 'host text' -and $text -match 'careful' -and $text -match 'broken')"
'@
        $text | Should -Match 'RUNS=2'
        $text | Should -Match 'ERRORS=True'
        $text | Should -Match 'NOTES=True'
    }

    It 'unrolls a collection that arrives as one object, as Out-Default does' {
        $text = Invoke-ViewChild -Script @'
@(@(1, 2), @(3, 4)) | Out-ConsoleDefault -Force
Write-Output -NoEnumerate @(5, 6) | Out-ConsoleDefault -Force
'@
        $text | Should -Match '1\s*\n2\s*\n3\s*\n4\s*\n5\s*\n6'
        $text | Should -Not -Match 'SyncRoot'
    }

    It 'never writes the taskbar sequence in redirected output, even in Windows Terminal' {
        $text = Invoke-ViewChild -Environment @{ WT_SESSION = 'test' } -Script @'
Show-ConsoleStatusLine 'working' -Spinner
Show-ConsoleStatusLine 'done' -Status Pass -Complete
'@
        $text | Should -Not -Match ([regex]::Escape([char] 27 + ']9;4'))
        $text | Should -Match 'done'
    }
}
