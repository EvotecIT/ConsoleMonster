Describe 'Progress columns and translated labels' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        # A child process with redirected output (plain text), no capability overrides, en-US numbers and the given UI culture.
        function Invoke-ProgressChild {
            param([Parameter(Mandatory)][string] $Script, [string] $UICulture = 'en-US', [string] $Language)
            $lines = [System.Collections.Generic.List[string]]::new()
            $lines.Add("foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CONSOLEMONSTER_LANGUAGE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'COLUMNS') { [Environment]::SetEnvironmentVariable(`$n, `$null) }")
            if ($Language) {
                $lines.Add("[Environment]::SetEnvironmentVariable('CONSOLEMONSTER_LANGUAGE', '$Language')")
            }
            $lines.Add("[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')")
            $lines.Add("[System.Threading.Thread]::CurrentThread.CurrentUICulture = [System.Globalization.CultureInfo]::GetCultureInfo('$UICulture')")
            $lines.Add("Import-Module '$($script:ModulePath)' -Force")
            $lines.Add($Script)
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            ($output | ForEach-Object { "$_" }) -join "`n"
        }
    }

    It 'renders lanes through the chosen columns' {
        $text = Invoke-ProgressChild -Script @'
$lanes = New-ConsoleLanes -Lane 'SRV01', 'SRV02' -Total 10 -Columns Description, Percent, Count, Status
$lanes.Apply(([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Live.LiveEvent'))::new('lanes', 'state', 'SRV01', 'copying', 'running', 0, 0, 0))
$lanes.Apply(([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Live.LiveEvent'))::new('lanes', 'advance', 'SRV01', 'copying', 'pass', 4, 0, 1000))
$lanes | Show-Console -Width 60
'@
        $lines = @($text -split "`n")
        ($lines | Where-Object { $_ -match '^SRV01' }) | Should -Match '^SRV01\s+40%\s+4/10\s+copying$'
        ($lines | Where-Object { $_ -match '^SRV02' }) | Should -Match '^SRV02\s+queued$'
    }

    It 'rejects an unknown column name' {
        { New-ConsoleLanes -Columns 'Nope' } | Should -Throw
    }

    It 'follows the UI culture for built-in labels' {
        $text = Invoke-ProgressChild -UICulture 'pl-PL' -Script @'
1..5 | ForEach-Object { [pscustomobject]@{ Server = "SRV0$_"; State = 'ok' } } | Show-ConsoleTable -Top 2 -Width 60
'@
        $text | Should -Match 'Pokazano 2 z 5'
    }

    It 'takes the language from CONSOLEMONSTER_LANGUAGE over the UI culture' {
        $text = Invoke-ProgressChild -UICulture 'pl-PL' -Language 'de' -Script @'
1..5 | ForEach-Object { [pscustomobject]@{ Server = "SRV0$_" } } | Show-ConsoleTable -Top 2 -Width 60
'@
        $text | Should -Match '2 von 5 angezeigt'
    }

    It 'keeps English for languages without a translation' {
        $text = Invoke-ProgressChild -UICulture 'ja-JP' -Script @'
1..5 | ForEach-Object { [pscustomobject]@{ Server = "SRV0$_" } } | Show-ConsoleTable -Top 2 -Width 60
'@
        $text | Should -Match 'Showing 2 of 5'
    }
}
