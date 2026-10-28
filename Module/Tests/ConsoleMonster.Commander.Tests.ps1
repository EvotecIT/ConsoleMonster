Describe 'Commander core from PowerShell' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        $script:Core = [ConsoleMonster.Terminal].Assembly
        function Get-CoreType([string] $Name) { $script:Core.GetType($Name, $true) }
        function Invoke-Operation($Operation) { $Operation.RunAsync([Threading.CancellationToken]::None).GetAwaiter().GetResult() }
    }

    It 'copies with a conflict policy and reports a summary' {
        $source = New-Item -ItemType Directory -Path (Join-Path $TestDrive 'src')
        $target = New-Item -ItemType Directory -Path (Join-Path $TestDrive 'dst')
        Set-Content -LiteralPath (Join-Path $source 'a.txt') -Value 'new'
        Set-Content -LiteralPath (Join-Path $source 'b.txt') -Value 'b'
        Set-Content -LiteralPath (Join-Path $target 'a.txt') -Value 'old'
        $options = (Get-CoreType 'ConsoleMonster.FileSystem.FileOperationOptions')::new()
        $options.ConflictPolicy = 'Skip'
        $operation = (Get-CoreType 'ConsoleMonster.FileSystem.FileOperation')::Copy([string[]] @((Join-Path $source 'a.txt'), (Join-Path $source 'b.txt')), $target.FullName, $options)
        $result = Invoke-Operation $operation
        $result.Completed | Should -Be 1
        $result.Skipped | Should -Be 1
        $result.Succeeded | Should -BeTrue
        $result.ToString() | Should -BeExactly '1 done, 1 skipped'
        Get-Content -LiteralPath (Join-Path $target 'a.txt') | Should -BeExactly 'old'
        Get-Content -LiteralPath (Join-Path $target 'b.txt') | Should -BeExactly 'b'
    }

    It 'continues past a failing item and offers a retry' {
        $present = Join-Path $TestDrive 'present.txt'
        Set-Content -LiteralPath $present -Value 'p'
        $operation = (Get-CoreType 'ConsoleMonster.FileSystem.FileOperation')::Copy([string[]] @((Join-Path $TestDrive 'missing.txt'), $present), (Join-Path $TestDrive 'out'))
        $result = Invoke-Operation $operation
        $result.Errors.Count | Should -Be 1
        $result.Errors[0].Kind.ToString() | Should -BeExactly 'NotFound'
        Test-Path -LiteralPath (Join-Path $TestDrive 'out/present.txt') | Should -BeTrue
        $result.RetryFailed().Sources | Should -HaveCount 1
    }

    It 'marks panel rows by pattern and plans a copy to the other panel' {
        $left = New-Item -ItemType Directory -Path (Join-Path $TestDrive 'left')
        $right = New-Item -ItemType Directory -Path (Join-Path $TestDrive 'right')
        foreach ($name in 'one.ps1', 'two.ps1', 'notes.md') { Set-Content -LiteralPath (Join-Path $left $name) -Value $name }
        $model = (Get-CoreType 'ConsoleMonster.Apps.Commander.CommanderModel')::new($left.FullName, $right.FullName)
        try {
            $model.Active.MarkPattern('*.ps1') | Should -Be 2
            $model.Active.MarkedCount | Should -Be 2
            $copy = $model.PlanCopy($null, $null)
            $copy.Targets | Should -Be @((Join-Path $right.FullName 'one.ps1'), (Join-Path $right.FullName 'two.ps1'))
            (Invoke-Operation $copy).Succeeded | Should -BeTrue
            $model.AfterOperation($copy)
            $model.Other.Entries.Name | Should -Contain 'one.ps1'
        } finally { $model.Dispose() }
    }

    It 'describes the key bar from the command registry' {
        $commands = (Get-CoreType 'ConsoleMonster.Apps.Commander.CommanderCommands')::CreateDefault()
        ($commands | Where-Object FunctionKey -NE 0 | Sort-Object FunctionKey | ForEach-Object { $_.FunctionKey.ToString() + $_.BarLabel }) -join ' ' |
            Should -BeExactly '1Help 2Menu 3View 4Edit 5Copy 6Move 7Mkdir 8Delete 9Palette 10Quit'
        $commands.Find('copy').Alternatives[0].ToString() | Should -BeExactly 'Alt+5'
    }
}
