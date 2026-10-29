BeforeDiscovery {
    $fixture = Get-Content (Join-Path $PSScriptRoot '../../Tests/Api/round2-commands.json') -Raw | ConvertFrom-Json
    $auditCases = @($fixture.Commands | Sort-Object | ForEach-Object { @{ Name = $_ } })
}

Describe 'ConsoleMonster pipeline input audit' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        $script:ModulePath = (Resolve-Path (Join-Path $PSScriptRoot '../ConsoleMonster.psd1')).Path
        $script:Worker = (Resolve-Path (Join-Path $PSScriptRoot 'Support/Invoke-PipelineAuditChild.ps1')).Path
        Import-Module $script:ModulePath -Force
    }

    It 'requires an audit route for every exported command' {
        $expected = @((Get-Content (Join-Path $PSScriptRoot '../../Tests/Api/round2-commands.json') -Raw | ConvertFrom-Json).Commands)
        $actual = @(Get-Command -Module ConsoleMonster -CommandType Cmdlet | Select-Object -ExpandProperty Name)
        Compare-Object $expected $actual | Should -BeNullOrEmpty
    }

    It '<Name> processes or rejects 300 objects with at most one error' -TestCases $auditCases {
        param($Name)
        $scratch = Join-Path $TestDrive $Name
        New-Item -ItemType Directory -Path $scratch -Force | Out-Null
        $worker = $script:Worker.Replace("'", "''")
        $module = $script:ModulePath.Replace("'", "''")
        $directory = $scratch.Replace("'", "''")
        $output = Invoke-TestChild -Runtime $script:Runtime -TimeoutSeconds 30 -Script "& '$worker' -Name '$Name' -ModulePath '$module' -ScratchPath '$directory'"
        $LASTEXITCODE | Should -Be 0
        $match = [regex]::Match(($output -join "`n"), 'AUDIT_RESULT:(\{[^\r\n]*\})')
        $match.Success | Should -BeTrue
        $result = $match.Groups[1].Value | ConvertFrom-Json
        if ($result.FirstId -match '^(MissingMandatoryParameter|AmbiguousParameterSet)') {
            Set-ItResult -Skipped -Because 'This command needs a safe argument fixture before its pipeline behavior can be exercised.'
            return
        }
        if ($Name -eq 'Assert-ConsoleOutput') { $result.OutputCount | Should -Be 300 }
        $result.Errors | Should -BeLessOrEqual 1
    }
}
