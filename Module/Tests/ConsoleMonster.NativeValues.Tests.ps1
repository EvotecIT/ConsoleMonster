Describe 'Native formatter raw values and shared presenters' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        . (Join-Path $PSScriptRoot 'Support/Convert-TestNativeView.ps1')
        $script:NativeRows = @(
            [pscustomobject]@{ Name='one'; State='Pass'; Bytes=2048L; Date=[datetime]'2026-10-21'; Duration=[timespan]::FromMinutes(2); Tags=@('a','b') }
            [pscustomobject]@{ Name='two'; State='Warning'; Bytes=4096L; Date=[datetime]'2026-10-22'; Duration=[timespan]::FromMinutes(3); Tags=@('x','y') }
            [pscustomobject]@{ Name='three'; State='Fail'; Bytes=8192L; Date=[datetime]'2026-10-23'; Duration=[timespan]::FromMinutes(4); Tags=@('last') }
        )
        $script:NativeProperties = @('Name','State','Bytes','Date','Duration','Tags')
    }

    It 'produces the same presented table cells as Show-ConsoleTable from raw values' {
        $native = Convert-TestNativeView -Rows $script:NativeRows -Arguments @{Property=$script:NativeProperties}
        $console = $script:NativeRows | New-ConsoleTable -Property $script:NativeProperties
        $nativeText = $native | Export-ConsoleFrame -Format Text -Width 160
        $nativeText | Should -Be ($console | Export-ConsoleFrame -Format Text -Width 160)
        $nativeText | Should -Match '✓ Pass'
        $nativeText | Should -Match '▴ Warning'
        $nativeText | Should -Match '✗ Fail'
        $nativeText | Should -Match '2 KB'
        $nativeText | Should -Match '2026-10-21'
        $nativeText | Should -Match '2 m 00 s'
        $nativeText | Should -Match 'a, b'
        $native.Rows[0][2].HasValue | Should -BeTrue
        $native.Rows[0][2].Value | Should -Be 2048
        $native.Rows[0][3].Value | Should -BeOfType datetime
    }

    It 'keeps list labels and uses the same status bytes date duration and collection presenters' {
        $native = Convert-TestNativeView -Rows $script:NativeRows -Kind List -Arguments @{Property=$script:NativeProperties}
        $text = $native | Export-ConsoleFrame -Format Markdown
        foreach ($value in @('| Name | one |','| State | ✓ Pass |','| State | ▴ Warning |','| State | ✗ Fail |','| Bytes | 2 KB |','| Duration | 2 m 00 s |','| Tags | a, b |')) {
            $text | Should -Match ([regex]::Escape($value))
        }
        $text | Should -Match '\| Date \| 2026-10-21'
    }

    It 'keeps automatic presenter alignment consistent in native plain and grouped Markdown tables' {
        $native = Convert-TestNativeView -Rows $script:NativeRows -Arguments @{Property=$script:NativeProperties}
        $plain = $script:NativeRows | New-ConsoleTable -Property $script:NativeProperties  | Export-ConsoleFrame -Format Markdown
        $grouped = $script:NativeRows | New-ConsoleTable -Property $script:NativeProperties -GroupBy State -ShowGroupColumn  | Export-ConsoleFrame -Format Markdown
        $alignment = '| :--- | :--- | ---: | :--- | ---: | :--- |'
        foreach ($markdown in @(($native | Export-ConsoleFrame -Format Markdown), $plain, $grouped)) {
            $markdown | Should -Match ([regex]::Escape($alignment))
        }
        $explicit = Convert-TestNativeView -Rows $script:NativeRows -Arguments @{Property=@('Name', @{Expression='Duration';Alignment='Left'})}
        ($explicit | Export-ConsoleFrame -Format Markdown) | Should -Match '\| :--- \| :--- \|'
    }

    It 'infers formatted column alignment from raw values while explicit alignment wins' {
        $rows = @([pscustomobject]@{State='Pass';Name='server';Count=12.5;Duration=[timespan]::FromSeconds(125)})
        $properties = @(
            @{Label='Health';Expression='State';FormatString='[{0}]'},
            @{Label='Text';Expression='Name';FormatString='[{0}]'},
            @{Label='Count';Expression='Count';FormatString='N2'},
            @{Label='Duration';Expression='Duration';FormatString='c'},
            @{Label='Explicit';Expression='State';FormatString='[{0}]';Alignment='Right'}
        )
        $native = Convert-TestNativeView -Rows $rows -Arguments @{Property=$properties}
        $console = $rows | New-ConsoleTable -Property $properties
        foreach($widget in @($native,$console)) {
            ($widget | Export-ConsoleFrame -Format Markdown) | Should -Match '\| :--- \| :--- \| ---: \| ---: \| ---: \|'
        }
        ($native | Export-ConsoleFrame -Format Markdown) | Should -Match ([regex]::Escape('✓ \[Pass\]'))
    }

    It 'shapes default columns and wildcard selections without reading a getter twice' {
        $global:RawGetterReads = 0
        $row = [pscustomobject]@{ Name='getter'; Bytes=2048L }
        $row | Add-Member -MemberType ScriptProperty -Name State -Value { $global:RawGetterReads++; 'Pass' }
        try {
            $native = Convert-TestNativeView -Rows @($row)
            $text = $native | Export-ConsoleFrame -Format Text -Width 100
            $text | Should -Match '✓ Pass'
            $text | Should -Match '2 KB'
            $global:RawGetterReads | Should -Be 1
            $native = Convert-TestNativeView -Rows $script:NativeRows -Arguments @{Property=@('N*','S*')}
            ($native.Columns.Header -join ',') | Should -Be 'Name,State'
        } finally { Remove-Variable -Name RawGetterReads -Scope Global }
    }

    It 'evaluates calculated expressions and getters once while native labels alignment and format strings win' {
        $script:RawEvaluations = 0
        $properties = @(
            @{ Label='Health'; Expression={ $script:RawEvaluations++; $_.State }; FormatString='[{0}]' }
            @{ Label='Amount'; Expression='Bytes'; FormatString='N1'; Alignment='Center' }
        )
        $native = Convert-TestNativeView -Rows $script:NativeRows -Arguments @{Property=$properties}
        $script:RawEvaluations | Should -Be 3
        $native.Columns[0].Header | Should -Be 'Health'
        $native.Columns[1].Alignment.ToString() | Should -Be 'Center'
        $text = $native | Export-ConsoleFrame -Format Text -Width 100
        $text | Should -Match '✓ \[Pass\]'
        $text | Should -Match ([regex]::Escape((2048).ToString('N1', [cultureinfo]::CurrentCulture)))
        $text | Should -Not -Match '2 KB'
    }

    It 'retains native named table and list view formatting and expression counts' {
        $path = Join-Path $PSScriptRoot 'Fixtures/EverydayViews/RawValues.format.ps1xml'
        Update-FormatData -PrependPath $path
        $row = [pscustomobject]@{PSTypeName='ConsoleMonster.Tests.RawNative';State='Pass';Bytes=2048L}
        $global:RawViewEvaluations = 0
        try {
            foreach ($kind in 'Table','List') {
                $native = Convert-TestNativeView -Rows @($row) -Kind $kind -Arguments @{View=('Raw'+$kind)}
                $text = $native | Export-ConsoleFrame -Format Text -Width 100
                $text | Should -Match '✓ Pass'
                $text | Should -Match ([regex]::Escape((2048).ToString('N1', [cultureinfo]::CurrentCulture)))
                $text | Should -Match 'Exact bytes'
            }
            $global:RawViewEvaluations | Should -Be 1
        } finally { Remove-Variable -Name RawViewEvaluations -Scope Global }
    }

    It 'keeps status presenters on every native group and after an empty first value' {
        $rows = @(
            [pscustomobject]@{State=$null;Group='A'}
            [pscustomobject]@{State='Pass';Group='A'}
            [pscustomobject]@{State='Fail';Group='B'}
        )
        $native = Convert-TestNativeView -Rows $rows -Arguments @{Property=@('State');GroupBy='Group'}
        $text = $native | Export-ConsoleFrame -Format Text -Width 60
        $text | Should -Match '✓ Pass'
        $text | Should -Match '✗ Fail'
    }

    It 'retains native scripted row-selection counts and evaluates only the chosen cells' {
        Update-FormatData -PrependPath (Join-Path $PSScriptRoot 'Fixtures/EverydayViews/RawValues.format.ps1xml')
        $rows = @(
            [pscustomobject]@{PSTypeName='ConsoleMonster.Tests.ConditionalNative';State='Pass';Alternate='Wrong';UseAlternate=$false}
            [pscustomobject]@{PSTypeName='ConsoleMonster.Tests.ConditionalNative';State='Wrong';Alternate='Warning';UseAlternate=$true}
            [pscustomobject]@{PSTypeName='ConsoleMonster.Tests.ConditionalNative';State='Fail';Alternate='Wrong';UseAlternate=$false}
        )
        $global:RawSelections = 0
        $global:RawCells = 0
        try {
            $records = @($rows | Microsoft.PowerShell.Utility\Format-Table -View RawConditional)
            $expectedSelections = $global:RawSelections
            $expectedCells = $global:RawCells
            $expectedText = $records | & ([Management.Automation.CmdletInfo]::new('Out-ConsoleFormatCommand', (Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.CmdletOutConsoleFormatCommand'))) -AsWidget | Export-ConsoleFrame -Format Text -Width 80
            $global:RawSelections = 0
            $global:RawCells = 0
            $native = Convert-TestNativeView -Rows $rows -Arguments @{View='RawConditional'}
            $text = $native | Export-ConsoleFrame -Format Text -Width 80
            $global:RawSelections | Should -Be $expectedSelections
            $global:RawCells | Should -Be $expectedCells
            $text | Should -BeExactly $expectedText
            $text | Should -Match '✓ Pass'
            $text | Should -Match '✗ Fail'
        } finally { Remove-Variable -Name RawSelections, RawCells -Scope Global }
    }

    It 'captures raw values from type-selected native table entries' {
        Update-FormatData -PrependPath (Join-Path $PSScriptRoot 'Fixtures/EverydayViews/RawValues.format.ps1xml')
        $rows = @(
            [pscustomobject]@{PSTypeName='ConsoleMonster.Tests.TypeSelectedNative';Bytes=2048L;Alternate=1L}
            [pscustomobject]@{PSTypeName='ConsoleMonster.Tests.AlternateNative';Bytes=1L;Alternate=4096L}
        )
        $native = Convert-TestNativeView -Rows $rows -Arguments @{View='RawTypeSelected'}
        $native.Rows[0][0].Value | Should -Be 2048
        $native.Rows[1][0].Value | Should -Be 4096
        $text = $native | Export-ConsoleFrame -Format Text -Width 80
        $text | Should -Match '2 KB'
        $text | Should -Match '4 KB'
    }

    It 'expands wildcard calculated expressions into the native columns with their labels and raw values' {
        $row = [pscustomobject]@{Name='first';Note='second'}
        $native = Convert-TestNativeView -Rows @($row) -Arguments @{Property=@(@{Label='Names';Expression='N*'})}
        ($native.Columns.Header -join ',') | Should -Be 'Names,Names'
        $native.Rows[0][0].Value | Should -Be 'first'
        $native.Rows[0][1].Value | Should -Be 'second'
    }

    It 'retains native scalar and dictionary shapes instead of substituting their implementation properties' {
        foreach($value in @('literal-marker', @{Name='dictionary-marker';State='Pass'})) {
            $native = Convert-TestNativeView -Rows @($value)
            $actual = $native | Export-ConsoleFrame -Format Text -Width 100
            $actual | Should -Match '(literal-marker|dictionary-marker)'
            $actual | Should -Not -Match 'IsReadOnly|IsFixedSize|SyncRoot|Length'
        }
    }

    It 'accepts native calculated-property key abbreviations and preserves unlabeled expressions' {
        foreach ($kind in 'Table','List') {
            $property = @{la='Amount';ex='Bytes';f='N1'}
            if ($kind -eq 'Table') { $property.w=20; $property.a='Right' }
            $native = Convert-TestNativeView -Rows @($script:NativeRows[0]) -Kind $kind -Arguments @{Property=@($property)}
            $text = $native | Export-ConsoleFrame -Format Text -Width 100
            $text | Should -Match 'Amount'
            $text | Should -Match ([regex]::Escape((2048).ToString('N1', [cultureinfo]::CurrentCulture)))
            foreach($expression in @('Bytes', { $_.Bytes })) {
                $property = @{Expression=$expression}
                $records = @($script:NativeRows[0] | & ('Microsoft.PowerShell.Utility\Format-'+$kind) -Property $property)
                if($kind -eq 'Table') { $expected=$records[0].shapeInfo.tableColumnInfoList[0].propertyName }
                else { $expected=$records[2].formatEntryInfo.listViewFieldList[0].propertyName }
                $native = Convert-TestNativeView -Rows @($script:NativeRows[0]) -Kind $kind -Arguments @{Property=@($property)}
                $text = $native | Export-ConsoleFrame -Format Text -Width 100
                $text | Should -Match ([regex]::Escape($expected.Trim()))
                $text | Should -Not -Match 'capture.Read'
            }
        }
    }

    It 'retains hidden headers and the label of a native view grouping expression' {
        Update-FormatData -PrependPath (Join-Path $PSScriptRoot 'Fixtures/EverydayViews/RawValues.format.ps1xml')
        $row = [pscustomobject]@{PSTypeName='ConsoleMonster.Tests.HiddenNative';State='Pass';Group='A'}
        $native = Convert-TestNativeView -Rows @($row) -Arguments @{View='RawHidden'}
        $native.ShowHeader | Should -BeFalse
        $text = $native | Export-ConsoleFrame -Format Text -Width 100
        $text | Should -Match 'Native group: A'
        $text | Should -Match '✓ Pass'
        $text | Should -Not -Match 'Hidden health'
    }

    It 'checks unsupported <Variant> metadata before evaluating native conditional rows' -ForEach @(@{Variant='Wrap'},@{Variant='Format'},@{Variant='Group'}) {
        param($Variant)
        Update-FormatData -PrependPath (Join-Path $PSScriptRoot 'Fixtures/EverydayViews/RawValues.format.ps1xml')
        function Invoke-TestCaptureRecords {
            [CmdletBinding()]
            param($Row, $View)
            $capture = [Activator]::CreateInstance((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.NativeFormatCapture'), [object[]]@($PSCmdlet, 'Table', @{View=$View}))
            $parameters = $capture.Prepare($Row)
            $capture.BeginItem($Row)
            $Row | Microsoft.PowerShell.Utility\Format-Table @parameters
        }
        $row = [pscustomobject]@{PSTypeName='ConsoleMonster.Tests.FallbackNative';State='Fail';Alternate='Warning';Group='A'}
        $global:RawFallbackSelections=0
        try {
            $expected = $row | Microsoft.PowerShell.Utility\Format-Table -View ('RawFallback'+$Variant) | Out-String -Width 100
            $expectedCount = $global:RawFallbackSelections
            $expectedCount | Should -BeGreaterThan 0
            $global:RawFallbackSelections=0
            $actual = Invoke-TestCaptureRecords -Row $row -View ('RawFallback'+$Variant) | Out-String -Width 100
            $global:RawFallbackSelections | Should -Be $expectedCount
            $actual | Should -BeExactly $expected
            $actual | Should -Match 'Warning'
            $actual | Should -Not -Match 'Fail'
        } finally { Remove-Variable -Name RawFallbackSelections -Scope Global }
    }
}
