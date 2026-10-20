Describe 'Show-ConsoleHelp' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        # A child process with redirected output (plain text), en-US culture and a function with comment-based help.
        function Invoke-HelpChild {
            param([Parameter(Mandatory)][string] $Script)
            $lines = @(
                "foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CONSOLEMONSTER_LANGUAGE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'COLUMNS') { [Environment]::SetEnvironmentVariable(`$n, `$null) }"
                "[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')"
                "[System.Threading.Thread]::CurrentThread.CurrentUICulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')"
                "Import-Module '$($script:ModulePath)' -Force"
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
            )
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            (($output | ForEach-Object { "$_" }) -join "`n") -replace "$([char] 27)\[[0-9;]*m", ''
        }
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

    It 'lists wildcard matches, reads a script''s help by path and points programs to their own help' {
        $text = Invoke-HelpChild @'
'== list'
Show-ConsoleHelp 'Get-Thin*' -Width 100 -AsString
function Get-Thinner { <# .SYNOPSIS
Makes things thinner. #> param() }
Show-ConsoleHelp 'Get-Thin*' -Width 100 -AsString
'== script'
$path = Join-Path ([IO.Path]::GetTempPath()) "cm-help-$PID.ps1"
Set-Content -LiteralPath $path -Value "<#`n.SYNOPSIS`nCleans the cache.`n.EXAMPLE`nPS C:\> .\clean.ps1 -Days 7`n#>`nparam([int] `$Days = 30)"
Show-ConsoleHelp $path -Width 100 -AsString
Remove-Item -LiteralPath $path
'== program'
$exe = if ($IsLinux -or $IsMacOS) { 'ls' } else { 'whoami' }
Show-ConsoleHelp $exe -ErrorVariable e -ErrorAction SilentlyContinue
"PROGRAM=$($e[0].FullyQualifiedErrorId -like 'ProgramHasNoHelp*')"
'@
        $list = ($text -split '== script')[0]
        $list | Should -Match 'Gets things from a store\.'
        $list | Should -Match 'Get-Thinner\s+\S\s+Function\s+\S\s+Makes things thinner\.'
        $script = (($text -split '== script')[1] -split '== program')[0]
        $script | Should -Match 'Cleans the cache\.'
        $script | Should -Match '-Days\s+\S\s+Int32'
        $script | Should -Match '(?m)^\s+\.\\clean\.ps1 -Days 7'
        $script | Should -Not -Match 'PS C:\\>'
        $text | Should -Match 'PROGRAM=True'
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
