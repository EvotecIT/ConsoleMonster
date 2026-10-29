Describe 'Show-ConsoleHelp' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        $script:HelpStepsPath = Join-Path $TestDrive 'help-child-steps.txt'
        $script:HelpModuleRoot = Join-Path $TestDrive 'help-modules'
        $null = New-Item -ItemType Directory -Path $script:HelpModuleRoot

        # A child process with redirected output (plain text), en-US culture and a function with comment-based help.
        function Invoke-HelpChild {
            param([Parameter(Mandatory)][string] $Script)
            $lines = @(
                # Load the native cmdlet used below, then give wildcard help an owned, empty available-module catalog.
                "`$env:PSModulePath = Join-Path `$PSHOME 'Modules'"
                'Import-Module Microsoft.PowerShell.Management -ErrorAction Stop'
                "`$helpStepsPath = '$($script:HelpStepsPath.Replace("'", "''"))'"
                @'
function Write-HelpStep {
    param([string] $Step)
    [IO.File]::AppendAllText($helpStepsPath, [DateTimeOffset]::Now.ToString('o') + " PID=$PID $Step" + [Environment]::NewLine)
}
Write-HelpStep 'module import begin'
'@
                "foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CONSOLEMONSTER_LANGUAGE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'COLUMNS') { [Environment]::SetEnvironmentVariable(`$n, `$null) }"
                "[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')"
                "[System.Threading.Thread]::CurrentThread.CurrentUICulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')"
                "Import-Module '$($script:ModulePath)' -Force"
                "Write-HelpStep 'module import end'"
                "`$env:PSModulePath = '$($script:HelpModuleRoot.Replace("'", "''"))'"
                @'
function Get-Thing {
    <#
    .SYNOPSIS
    Gets things from a store.
    .DESCRIPTION
    Reads the things in the store and returns one object per thing.
    .PARAMETER Name
    The thing's name; wildcards work.
    .PARAMETER Store
    The store to read.
    .EXAMPLE
    Get-Thing -Name 'a*' | Sort-Object Size
    Gets the things whose names start with a, smallest first.
    .LINK
    https://example.org/things
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory, Position = 0, ValueFromPipeline)][string] $Name, [string] $Store = 'default', [switch] $Force)
    process { }
}
'@
                $Script
                "Write-HelpStep 'script end'"
            )
            [IO.File]::WriteAllText($script:HelpStepsPath, '')
            try {
                $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            } catch {
                $step = Get-Content -LiteralPath $script:HelpStepsPath -Tail 1 -ErrorAction SilentlyContinue
                throw "$($_.Exception.Message) Last Help child step: $step (markers: $script:HelpStepsPath)."
            }
            (($output | ForEach-Object { "$_" }) -join "`n") -replace "$([char] 27)\[[0-9;]*m", ''
        }
    }

    It 'accepts command names from the pipeline and rejects other objects once' {
        $text = Invoke-HelpChild @'
$Error.Clear()
$help = 'Get-Thing' | Show-ConsoleHelp -AsString
"HELP=$($help -match 'Gets things from a store')"
try { 1..300 | Show-ConsoleHelp -ErrorAction Stop } catch { "ID=$($_.FullyQualifiedErrorId.Split(',')[0]) MESSAGE=$($_.Exception.Message)" }
"ERRORS=$($Error.Count)"
$Error.Clear()
try { 1..300 | ForEach-Object { [pscustomobject]@{Name='Get-Thing'} } | Show-ConsoleHelp -ErrorAction Stop } catch { "OBJECT=$($_.FullyQualifiedErrorId.Split(',')[0])" }
"OBJECT_ERRORS=$($Error.Count)"
'@
        $text | Should -Match 'HELP=True'
        $text | Should -Match 'ID=ExpectedCommandName'
        $text | Should -Match 'Use Show-Console'
        $text | Should -Match 'ERRORS=1'
        $text | Should -Match 'OBJECT=ExpectedCommandName'
        $text | Should -Match 'OBJECT_ERRORS=1'
    }

    It 'shows a function''s comment-based help: synopsis, syntax, parameters, examples and links' {
        $text = Invoke-HelpChild 'Show-ConsoleHelp Get-Thing -Width 100 -AsString'
        $text | Should -Match '(?m)^Get-Thing\s*$'
        $text | Should -Match 'Gets things from a store\.'
        $text | Should -Match 'Get-Thing \[-Name\] <String> \[-Store <String>\] \[-Force\] \[<CommonParameters>\]'
        $text | Should -Match '-Name\s+\S\s+String\s+\S\s+yes\s+\S\s+1\s+\S\s+ByValue'
        $text | Should -Match '-Store\s+\S\s+String\s+\S\s+no\s+\S\s+named\s+\S\s+no\s+\S\s+default'
        $text | Should -Match '-Force\s+\S\s+switch'
        $text | Should -Match "Get-Thing -Name 'a\*' \| Sort-Object Size"
        $text | Should -Match 'https://example.org/things'
        $text | Should -Match 'about_CommonParameters'
    }

    It 'shows a cmdlet''s syntax and parameters, also without help files' {
        $text = Invoke-HelpChild 'Show-ConsoleHelp Get-Item -Width 120 -AsString'
        $text | Should -Match 'Syntax'
        $text | Should -Match 'Get-Item \[-Path\] <String\[\]>'
        $text | Should -Match '-LiteralPath'
        # Without a help file PowerShell puts the syntax in the synopsis; it is not repeated above the syntax.
        ($text -split 'Syntax')[0] | Should -Not -Match '\[-Path\]'
    }

    It 'lists single and multiple wildcard matches without literal command discovery' {
        $text = Invoke-HelpChild @'
$script:literalWildcardLookups = 0
$ExecutionContext.InvokeCommand.CommandNotFoundAction = {
    param($name, $eventArgs)
    if ($name -eq 'Get-Thin*') { $script:literalWildcardLookups++ }
}
Write-HelpStep 'wildcard single begin'
Show-ConsoleHelp 'Get-Thin*' -Width 100 -AsString
Write-HelpStep 'wildcard single end'
function Get-Thinner { <# .SYNOPSIS
Makes things thinner. #> param() }
Write-HelpStep 'wildcard multiple begin'
Show-ConsoleHelp 'Get-Thin*' -Width 100 -AsString
Write-HelpStep 'wildcard multiple end'
$ExecutionContext.InvokeCommand.CommandNotFoundAction = $null
"WILDCARD_LOOKUPS=$script:literalWildcardLookups"
'@
        $text | Should -Match 'Gets things from a store\.'
        $text | Should -Match 'Get-Thinner\s+\S\s+Function\s+\S\s+Makes things thinner\.'
        $text | Should -Match 'WILDCARD_LOOKUPS=0'
    }

    It 'reads a script''s help by path' {
        $text = Invoke-HelpChild @'
Write-HelpStep 'script path begin'
$path = Join-Path ([IO.Path]::GetTempPath()) "cm-help-$PID.ps1"
Set-Content -LiteralPath $path -Value "<#`n.SYNOPSIS`nCleans the cache.`n.EXAMPLE`nPS C:\> .\clean.ps1 -Days 7`n#>`nparam([int] `$Days = 30)"
Show-ConsoleHelp $path -Width 100 -AsString
Remove-Item -LiteralPath $path
Write-HelpStep 'script path end'
'@
        $text | Should -Match 'Cleans the cache\.'
        $text | Should -Match '-Days\s+\S\s+Int32'
        $text | Should -Match '(?m)^\s+\.\\clean\.ps1 -Days 7'
        $text | Should -Not -Match 'PS C:\\>'
    }

    It 'points programs to their own help' {
        $text = Invoke-HelpChild @'
Write-HelpStep 'program begin'
$exe = if ($IsLinux -or $IsMacOS) { 'ls' } else { 'whoami' }
Show-ConsoleHelp $exe -ErrorVariable e -ErrorAction SilentlyContinue
"PROGRAM=$($e[0].FullyQualifiedErrorId -like 'ProgramHasNoHelp*')"
Write-HelpStep 'program end'
'@
        $text | Should -Match 'PROGRAM=True'
    }

    It 'keeps the child help search independent of the inherited module catalog' {
        $modulePath = $env:PSModulePath
        try {
            $catalog = Join-Path $PSScriptRoot 'Fixtures\HelpCatalog'
            $env:PSModulePath = $catalog + [IO.Path]::PathSeparator + $modulePath
            $text = Invoke-HelpChild @'
Write-HelpStep 'foreign catalog single begin'
'== single'
Show-ConsoleHelp 'Get-Thin*' -Width 100 -AsString
Write-HelpStep 'foreign catalog single end'
function Get-Thinner { <# .SYNOPSIS
Makes things thinner. #> param() }
Write-HelpStep 'foreign catalog multiple begin'
'== multiple'
Show-ConsoleHelp 'Get-Thin*' -Width 100 -AsString
Write-HelpStep 'foreign catalog multiple end'
'@
        } finally {
            $env:PSModulePath = $modulePath
        }
        $text | Should -Match 'Gets things from a store\.'
        ($text -split '== multiple')[0] | Should -Match 'Syntax'
        ($text -split '== multiple')[1] | Should -Match 'Get-Thinner\s+\S\s+Function\s+\S\s+Makes things thinner\.'
        $text | Should -Not -Match 'Get-ThinOutside'
    }

    It 'writes plain lines into a pipeline and fails for an unknown command' {
        $text = Invoke-HelpChild @'
$lines = @(Show-ConsoleHelp Get-Thing | Where-Object { $_ -match 'Syntax' })
"LINES=$($lines.Count) TYPE=$($lines[0].GetType().Name)"
Show-ConsoleHelp Get-NoSuchCommandAnywhere -ErrorVariable e -ErrorAction SilentlyContinue
"ERRORS=$($e.Count)"
'@
        $text | Should -Match 'LINES=1 TYPE=String'
        $text | Should -Match 'ERRORS=1'
    }
}
