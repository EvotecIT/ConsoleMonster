Describe 'Format-Console output reaches every PowerShell consumer' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        $script:IsCore = $PSVersionTable.PSEdition -eq 'Core' -and $PSVersionTable.PSVersion -ge [version] '7.2'
        $script:Folder = Join-Path ([System.IO.Path]::GetTempPath()) ('cm-out-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
        New-Item -ItemType Directory -Path $script:Folder -Force | Out-Null

        # Runs a script in a child process (redirected output, en-US) with $svc = two sample records, and returns its lines.
        function Invoke-OutputChild {
            param([Parameter(Mandatory)][string] $Script)
            $lines = @(
                "foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'TERM', 'WT_SESSION', 'COLUMNS', 'LINES') { [Environment]::SetEnvironmentVariable(`$n, `$null) }"
                "[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')"
                "Import-Module '$($script:ModulePath)' -Force"
                "`$folder = '$($script:Folder)'"
                "`$svc = @([pscustomobject] @{ Name = 'alpha'; Status = 'Running' }, [pscustomobject] @{ Name = 'beta'; Status = 'Stopped' })"
                $Script
            )
            Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
        }

        function Get-Result {
            param([string[]] $Lines, [string] $Name)
            $line = $Lines | Where-Object { $_ -like "$Name=*" } | Select-Object -First 1
            if ($line) { $line.Substring($Name.Length + 1) }
        }
    }

    AfterAll {
        Remove-Item -LiteralPath $script:Folder -Recurse -Force -ErrorAction SilentlyContinue
    }

    It 'writes text to a pipeline consumer: Out-String, Out-File, Set-Content, more and -OutVariable' {
        $out = Invoke-OutputChild @'
$text = $svc | Show-ConsoleTable | Out-String
"OUTSTRING=$($text -match 'alpha' -and $text -match 'Status')"
$svc | Show-ConsoleTable | Out-File (Join-Path $folder 'outfile.txt')
"OUTFILE=$((Get-Content (Join-Path $folder 'outfile.txt') -Raw) -match 'beta')"
$svc | Show-ConsoleTable | Set-Content (Join-Path $folder 'setcontent.txt')
"SETCONTENT=$((Get-Content (Join-Path $folder 'setcontent.txt') -Raw) -match 'beta')"
$paged = @($svc | Show-ConsoleTable | more) -join "`n"
"MORE=$($paged -match 'alpha')"
$svc | Show-ConsoleTable -OutVariable captured | Out-Null
"OUTVARIABLE=$((@($captured) -join "`n") -match 'alpha')"
"ESCAPES=$($text.Contains([char] 27))"
'@
        Get-Result $out 'OUTSTRING' | Should -Be 'True'
        Get-Result $out 'OUTFILE' | Should -Be 'True'
        Get-Result $out 'SETCONTENT' | Should -Be 'True'
        Get-Result $out 'MORE' | Should -Be 'True'
        Get-Result $out 'OUTVARIABLE' | Should -Be 'True'
        Get-Result $out 'ESCAPES' | Should -Be 'False'
    }

    It 'captures -AsString on every PowerShell version' {
        $out = Invoke-OutputChild @'
$text = $svc | Show-ConsoleTable -AsString
"TYPE=$($text.GetType().Name)"
"TABLE=$($text -match 'alpha' -and $text -match 'beta')"
$list = $svc | Show-ConsoleList -AsString
"LIST=$($list -match 'Running')"
$json = $svc | Show-Console -AsString -View Json
"JSON=$($json -match '"Name": "alpha"')"
'@
        Get-Result $out 'TYPE' | Should -Be 'String'
        Get-Result $out 'TABLE' | Should -Be 'True'
        Get-Result $out 'LIST' | Should -Be 'True'
        Get-Result $out 'JSON' | Should -Be 'True'
    }

    It 'captures assignment, > file and *> on PowerShell 7.2+, and draws through the host on 5.1' {
        $out = Invoke-OutputChild @'
$x = $svc | Show-ConsoleTable
"ASSIGNED=$("$x" -match 'alpha')"
$svc | Show-ConsoleTable > (Join-Path $folder 'redirect.txt')
$redirected = Get-Content (Join-Path $folder 'redirect.txt') -Raw
"REDIRECT=$([bool] ($redirected -match 'alpha'))"
"REDIRECTESC=$([bool] ($redirected -and $redirected.Contains([char] 27)))"
$svc | Show-ConsoleTable *> (Join-Path $folder 'all.txt')
"ALL=$((Get-Content (Join-Path $folder 'all.txt') -Raw) -match 'alpha')"
'@
        Get-Result $out 'ALL' | Should -Be 'True'
        Get-Result $out 'REDIRECTESC' | Should -Be 'False'
        if ($script:IsCore) {
            Get-Result $out 'ASSIGNED' | Should -Be 'True'
            Get-Result $out 'REDIRECT' | Should -Be 'True'
        } else {
            # Windows PowerShell 5.1 has no $PSStyle to keep escape codes out of captured text: the table is drawn through
            # the host (use -AsString to capture there).
            Get-Result $out 'ASSIGNED' | Should -Be 'False'
            Get-Result $out 'REDIRECT' | Should -Be 'False'
            ($out -join "`n") | Should -Match 'alpha'
        }
    }

    It 'gives Show-Console the same output model: -AsString, plain lines when piped, the rendering at the prompt on 7.2+' {
        $out = Invoke-OutputChild @'
$text = $svc | Show-Console -AsString
"TYPE=$($text.GetType().Name) $($text -match 'alpha')"
$piped = $svc | Show-Console | Out-String
"PIPED=$($piped -match 'beta' -and -not $piped.Contains([char] 27))"
$x = $svc | Show-Console
"ASSIGNED=$("$x" -match 'alpha')"
'@
        Get-Result $out 'TYPE' | Should -Be 'String True'
        Get-Result $out 'PIPED' | Should -Be 'True'
        Get-Result $out 'ASSIGNED' | Should -Be ($script:IsCore).ToString()
    }

    It 'records the table in a transcript' {
        $out = Invoke-OutputChild @'
Start-Transcript -Path (Join-Path $folder 'transcript.txt') | Out-Null
$svc | Show-ConsoleTable
Stop-Transcript | Out-Null
$t = Get-Content (Join-Path $folder 'transcript.txt') -Raw
"TRANSCRIPT=$($t -match 'alpha' -and $t -match 'beta')"
"TRANSCRIPTESC=$($t.Contains([char] 27))"
'@
        Get-Result $out 'TRANSCRIPT' | Should -Be 'True'
        Get-Result $out 'TRANSCRIPTESC' | Should -Be 'False'
    }

    It 'keeps -PassThru writing the input objects, and refuses -AsString with -PassThru' {
        $out = Invoke-OutputChild @'
$passed = @($svc | Show-ConsoleTable -PassThru | Where-Object Status -eq 'Running')
"PASSED=$($passed.Count) $($passed[0].Name)"
try { $svc | Show-ConsoleTable -AsString -PassThru | Out-Null; 'BOTH=accepted' } catch { "BOTH=$($_.FullyQualifiedErrorId -like 'AsStringWithPassThru*')" }
'@
        Get-Result $out 'PASSED' | Should -Be '1 alpha'
        Get-Result $out 'BOTH' | Should -Be 'True'
    }

    It 'leaves the output of a function or script block alone: the view is drawn, not returned' {
        $out = Invoke-OutputChild @'
function Get-Data { $svc | Show-ConsoleTable; 'done' }
$result = @(Get-Data)
"RESULT=$($result.Count) $($result[-1])"
$null = Get-Data
$block = @(& { $svc | Show-ConsoleTable })
"BLOCK=$($block.Count)"
'@
        Get-Result $out 'RESULT' | Should -Be '1 done'
        Get-Result $out 'BLOCK' | Should -Be '0'
        # Each call drew the table on the screen.
        @($out | Where-Object { $_ -match 'alpha' }).Count | Should -BeGreaterOrEqual 3
    }

    It 'shows a rendering that crossed a serialization boundary as its text' {
        $out = Invoke-OutputChild @'
$x = $svc | Show-ConsoleTable
if ($x) {
    $copy = [System.Management.Automation.PSSerializer]::Deserialize([System.Management.Automation.PSSerializer]::Serialize($x))
    $text = $copy | Out-String
    "DESERIALIZED=$($copy.PSObject.TypeNames[0])"
    "SHOWN=$($text -match 'alpha' -and $text -notmatch 'Widget')"
} else {
    'DESERIALIZED=none'
}
'@
        if ($script:IsCore) {
            Get-Result $out 'DESERIALIZED' | Should -Be 'Deserialized.ConsoleMonster.PowerShell.ConsoleRendering'
            Get-Result $out 'SHOWN' | Should -Be 'True'
        } else {
            Get-Result $out 'DESERIALIZED' | Should -Be 'none'
        }
    }
}

Describe 'Widgets draw themselves' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path
    }

    It 'renders a widget at Out-Default and Out-String, and Format-List * still lists its properties' {
        $lines = @(
            "foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'TERM', 'WT_SESSION', 'COLUMNS', 'LINES') { [Environment]::SetEnvironmentVariable(`$n, `$null) }"
            "Import-Module '$($script:ModulePath)' -Force"
            # The format view runs under strict mode too (no `$PSStyle on Windows PowerShell 5.1).
            'Set-StrictMode -Version Latest'
            "`$t = New-ConsoleTable -InputObject @([pscustomobject] @{ Name = 'alpha'; Size = 1 }, [pscustomobject] @{ Name = 'beta'; Size = 2 })"
            "'== shown'"
            '$t'
            "'== aswidget'"
            "[pscustomobject] @{ Name = 'gamma' } | Show-ConsoleTable -AsWidget"
            "'== outstring'"
            '($t | Out-String).Trim()'
            "'== list'"
            '$t | Format-List * | Out-String -Width 200'
        )
        $text = (Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")) -join "`n"
        $shown = ($text -split '== aswidget')[0]
        $shown | Should -Match 'alpha'
        $shown | Should -Not -Match 'CardsBelow'
        ($text -split '== aswidget')[1] | Should -Match 'gamma'
        (($text -split '== outstring')[1] -split '== list')[0] | Should -Match 'beta'
        ($text -split '== list')[1] | Should -Match 'CardsBelow'
    }

    It 'keeps ConsoleMonster.format.ps1xml in step with the widget types' {
        Import-Module $script:ModulePath -Force
        $file = Join-Path $PSScriptRoot '..\ConsoleMonster.format.ps1xml'
        $expected = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.WidgetFormatData'))::Generate()
        ([System.IO.File]::ReadAllText($file) -replace "`r`n", "`n") | Should -Be ($expected -replace "`r`n", "`n") -Because 'Module/Build/Update-ConsoleFormatData.ps1 regenerates it'
    }
}
