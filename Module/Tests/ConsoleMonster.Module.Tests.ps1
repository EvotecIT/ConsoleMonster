Describe 'ConsoleMonster module' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        # Runs a script in a child process of the same runtime with redirected output, so ConsoleMonster detects a
        # redirected (Stream) terminal and the output can be inspected.
        function Invoke-ConsoleChild {
            param(
                [Parameter(Mandatory)][string] $Script,
                [hashtable] $Environment = @{}
            )
            $lines = [System.Collections.Generic.List[string]]::new()
            # Clear every capability-detection input so ambient settings (NO_COLOR, CI, TERM...) cannot change the result.
            $lines.Add("foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'BUILD_BUILDID', 'JENKINS_URL', 'GITLAB_CI', 'TEAMCITY_VERSION', 'TERM', 'COLORTERM', 'TERM_PROGRAM', 'WT_SESSION', 'COLUMNS', 'LINES') { [Environment]::SetEnvironmentVariable(`$n, `$null) }")
            $lines.Add("Import-Module '$($script:ModulePath)' -Force")
            foreach ($key in $Environment.Keys) {
                $lines.Add("`$env:$key = '$($Environment[$key])'")
            }
            $lines.Add($Script)
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            ($output | ForEach-Object { "$_" }) -join "`n"
        }
    }

    It 'exports the Console cmdlets on this runtime' {
        Import-Module $script:ModulePath -Force
        $commands = Get-Command -Module ConsoleMonster
        $commands.Count | Should -Be 141
        $manifest = Import-PowerShellDataFile -LiteralPath $script:ModulePath
        $cmdlets = @($commands | Where-Object CommandType -EQ Cmdlet)
        $cmdlets.Count | Should -Be 141
        ($cmdlets.Name | Sort-Object) -join ',' | Should -BeExactly (($manifest.CmdletsToExport | Sort-Object) -join ',')
        ($commands | Where-Object CommandType -EQ Alias) | Should -BeNullOrEmpty
        foreach ($name in 'Get-ConsoleDemo', 'Show-ConsoleDemo', 'New-ConsoleFlow', 'New-ConsoleFlowNode', 'New-ConsoleFlowEdge', 'New-ConsoleFlowBranch', 'Update-ConsoleFlow') { $cmdlets.Name | Should -Contain $name }
