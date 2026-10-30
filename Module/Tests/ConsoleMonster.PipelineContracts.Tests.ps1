Describe 'Supported comparison and chart pipeline contracts' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force -ErrorAction Stop
    }

    It 'returns the same complete changes for piped and explicit difference records without errors' {
        $before = @(
            [pscustomobject]@{ Name = 'one'; Value = 1 }
            [pscustomobject]@{ Name = 'old'; Value = 2 }
        )
        $after = @(
            [pscustomobject]@{ Name = 'one'; Value = 3 }
            [pscustomobject]@{ Name = 'new'; Value = 4 }
        )
        $errors = @()
        $piped = @($after | Compare-ConsoleObject -ReferenceObject $before -Property Name -PassThru -ErrorAction Stop -ErrorVariable errors -WarningAction SilentlyContinue -WarningVariable warnings)
        $explicit = @(Compare-ConsoleObject -ReferenceObject $before -DifferenceObject $after -Property Name -PassThru -ErrorAction Stop)
        $errors.Count | Should -Be 0
        $piped.Count | Should -Be 3
        $actual = @($piped | ForEach-Object { '{0}:{1}' -f $_.Kind, $_.Key } | Sort-Object)
        ($actual -join ',') | Should -Be 'Added:new,Changed:one,Removed:old'
        ($piped | ConvertTo-Json -Depth 8) | Should -Be ($explicit | ConvertTo-Json -Depth 8)
    }

    It 'keeps every row from multiple <Container> inputs with null observation <WithNull>' -TestCases @(
        @{ Container = 'DataTable'; WithNull = $false }
        @{ Container = 'DataSet'; WithNull = $false }
        @{ Container = 'DataTable'; WithNull = $true }
        @{ Container = 'DataSet'; WithNull = $true }
    ) {
        param($Container, $WithNull)
        $containers = @()
        foreach ($group in 0, 1) {
            $table = [System.Data.DataTable]::new()
            $null = $table.Columns.Add('Name', [string])
            $null = $table.Columns.Add('Value', [int])
            foreach ($offset in 1, 2) {
                $value = $group * 2 + $offset
                $null = $table.Rows.Add("row$value", $value)
            }
            if ($Container -eq 'DataSet') {
                $data = [System.Data.DataSet]::new()
                $data.Tables.Add($table)
                $containers += ,$data
            } else {
                $containers += ,$table
            }
        }
        $errors = @()
        $warnings = @()
        $chart = & {
            # Unary comma keeps the actual data container on both PowerShell runtimes.
            ,($containers[0])
            if ($WithNull) { $null }
            ,($containers[1])
        } | New-ConsoleChart -Property Value -Label Name -ErrorAction Stop -ErrorVariable errors -WarningAction SilentlyContinue -WarningVariable warnings
        $errors.Count | Should -Be 0
        $chart.Items.Count | Should -Be 4
        ($chart.Items | ForEach-Object Label) -join ',' | Should -Be 'row1,row2,row3,row4'
        ($chart.Items | ForEach-Object Value) -join ',' | Should -Be '1,2,3,4'
        $warnings.Count | Should -Be ([int]$WithNull)
        if ($WithNull) { $warnings[0] | Should -Match '1 rows without chartable values skipped' }
    }
}
