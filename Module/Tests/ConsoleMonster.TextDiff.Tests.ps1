Describe 'Compare-ConsoleText adapter contracts' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
    }

    It 'returns exact unified text from the text parameter set including empty input' {
        $actual = Compare-ConsoleText -ReferenceText '' -DifferenceText "hello`n" -AsUnified -Context 0
        $actual | Should -BeExactly "--- Before`n+++ After`n@@ -0,0 +1,1 @@`n+hello`n"
        Compare-ConsoleText -ReferenceText '' -DifferenceText '' -AsUnified | Should -BeExactly ''
    }

    It 'resolves relative and qualified FileSystem drive paths from the PowerShell location' {
        $before = Join-Path $TestDrive 'before.txt'
        $after = Join-Path $TestDrive 'after.txt'
        [IO.File]::WriteAllText($before, "key = one`n")
        [IO.File]::WriteAllText($after, "key = two`n")
        Push-Location $TestDrive
        try {
            $actual = Compare-ConsoleText -ReferencePath './before.txt' -DifferencePath 'TestDrive:/after.txt' -AsUnified -Context 0
            $actual | Should -BeExactly "--- before.txt`n+++ after.txt`n@@ -1,1 +1,1 @@`n-key = one`n+key = two`n"
            $qualified = 'Microsoft.PowerShell.Core\FileSystem::' + $before
            Compare-ConsoleText -ReferencePath $qualified -DifferencePath $after -AsUnified -Context 0 | Should -BeExactly $actual
        } finally {
            Pop-Location
        }
    }

    It 'uses explicit names for text in unified and rendered output' {
        $actual = Compare-ConsoleText -ReferenceText "old`n" -DifferenceText "new`n" -ReferenceName 'Deployed' -DifferenceName 'Proposed' -AsUnified
        $actual | Should -BeExactly "--- Deployed`n+++ Proposed`n@@ -1,1 +1,1 @@`n-old`n+new`n"
        $rendered = Use-ConsoleTarget -Width 60 {
            Compare-ConsoleText -ReferenceText "old`n" -DifferenceText "new`n" -ReferenceName '[old] config' -DifferenceName '[new] config'
        }
        $rendered | Should -Match '\[old\] config -> \[new\] config'
        $rendered | Should -Match '\+ new'
    }

    It 'overrides resolved filenames independently: <Case>' -TestCases @(
        @{ Case = 'reference'; Names = @{ ReferenceName = 'Deployed' }; ExpectedReference = 'Deployed'; ExpectedDifference = 'after.txt' }
        @{ Case = 'difference'; Names = @{ DifferenceName = 'Proposed' }; ExpectedReference = 'before.txt'; ExpectedDifference = 'Proposed' }
        @{ Case = 'both'; Names = @{ ReferenceName = 'Deployed'; DifferenceName = 'Proposed' }; ExpectedReference = 'Deployed'; ExpectedDifference = 'Proposed' }
    ) {
        param($Case, $Names, $ExpectedReference, $ExpectedDifference)
        $before = Join-Path $TestDrive 'before.txt'
        $after = Join-Path $TestDrive 'after.txt'
        Set-Content -LiteralPath $before -Value 'old' -Encoding UTF8
        Set-Content -LiteralPath $after -Value 'new' -Encoding UTF8
        $actual = Compare-ConsoleText -ReferencePath $before -DifferencePath $after @Names -Context 0 -AsUnified
        $actual | Should -BeExactly "--- $ExpectedReference`n+++ $ExpectedDifference`n@@ -1,1 +1,1 @@`n-old`n+new`n"
        $rendered = Use-ConsoleTarget -Width 60 {
            Compare-ConsoleText -ReferencePath $before -DifferencePath $after @Names -Context 0
        }
        $rendered | Should -Match ([regex]::Escape("$ExpectedReference -> $ExpectedDifference"))
    }

    It 'ignores whitespace only when asked while preserving line boundaries' {
        Compare-ConsoleText -ReferenceText "a = 1`n" -DifferenceText "a=1`n" -IgnoreWhitespace -AsUnified | Should -BeExactly ''
        Compare-ConsoleText -ReferenceText "a = 1`n" -DifferenceText "a=1`n" -AsUnified | Should -Match '-a = 1'
        Compare-ConsoleText -ReferenceText "a`nb`n" -DifferenceText "ab`n" -IgnoreWhitespace -AsUnified | Should -Match '-b'
    }

    It 'rejects a non-file provider and a wildcard matching multiple files' {
        { Compare-ConsoleText -ReferencePath 'Variable:/PSVersionTable' -DifferencePath 'Variable:/PSVersionTable' -AsUnified -ErrorAction Stop } |
            Should -Throw '*exactly one FileSystem file*'
        [IO.File]::WriteAllText((Join-Path $TestDrive 'first.txt'), 'one')
        [IO.File]::WriteAllText((Join-Path $TestDrive 'second.txt'), 'two')
        { Compare-ConsoleText -ReferencePath (Join-Path $TestDrive '*.txt') -DifferencePath (Join-Path $TestDrive 'first.txt') -AsUnified -ErrorAction Stop } |
            Should -Throw '*exactly one FileSystem file*'
    }

    It 'surfaces missing files and rejects mixed parameter sets and negative context' {
        { Compare-ConsoleText -ReferencePath (Join-Path $TestDrive 'missing.txt') -DifferencePath (Join-Path $TestDrive 'missing.txt') -AsUnified -ErrorAction Stop } | Should -Throw
        { Compare-ConsoleText -ReferenceText 'one' -DifferencePath 'two' -AsUnified -ErrorAction Stop } | Should -Throw
        { Compare-ConsoleText -ReferenceText 'one' -DifferenceText 'two' -Context -1 -ErrorAction Stop } | Should -Throw
    }

    It 'renders to the host without returning comparison objects' {
        $output = @(Compare-ConsoleText -ReferenceText "before`n" -DifferenceText "after`n")
        $output.Count | Should -Be 0
        $rendered = Use-ConsoleTarget -Width 60 {
            Compare-ConsoleText -ReferenceText "before`n" -DifferenceText "after`n"
        }
        $rendered | Should -Match 'Before -> After'
    }
}
