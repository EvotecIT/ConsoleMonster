Describe 'Dependency-free module console views' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:ViewModule = Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
        $script:ViewFixture = Join-Path $PSScriptRoot 'Fixtures/EverydayViews/EverydayViews.psd1'
        $script:ViewRuntime = [Diagnostics.Process]::GetCurrentProcess().Path
    }

    It 'loads declarative views with module ordering <Order>' -ForEach @(
        @{ Order = 'before' }
        @{ Order = 'after' }
    ) {
        $scriptText = @'
$ErrorActionPreference = 'Stop'
__IMPORTS__
$values = @(Get-EverydayValue)
if ($values.Count -ne 2 -or $values[0].HiddenDetail -ne 'private-detail') { throw 'The module changed its objects' }
$rendered = $values | Format-ConsoleTable -AsString -Width 80
if ($rendered -notmatch 'Health' -or $rendered -match 'HiddenDetail|private-detail') { throw 'Module views did not load' }
Register-ConsoleView -TypeName EverydayViews.Server -Property Name, HiddenDetail
$override = $values | Format-ConsoleTable -AsString -Width 80
if ($override -notmatch 'HiddenDetail') { throw 'Explicit registration did not win' }
'MODULE_VIEWS_OK'
'@
        $imports = if ($Order -eq 'before') {
            "Import-Module '$($script:ViewFixture)'`nImport-Module '$($script:ViewModule)'"
        } else {
            "Import-Module '$($script:ViewModule)'`nImport-Module '$($script:ViewFixture)'"
        }
        $result = Invoke-TestChild -Runtime $script:ViewRuntime -Script $scriptText.Replace('__IMPORTS__', $imports)
        $LASTEXITCODE | Should -Be 0
        ($result -join "`n") | Should -Match 'MODULE_VIEWS_OK'
    }

    It 'runs the example module without ConsoleMonster and imports the same data explicitly' {
        $result = Invoke-TestChild -Runtime $script:ViewRuntime -Script @"
`$ErrorActionPreference = 'Stop'
Import-Module '$($script:ViewFixture)'
if (@(Get-EverydayValue).Count -ne 2) { throw 'Example module needs ConsoleMonster' }
Import-Module '$($script:ViewModule)'
Register-ConsoleView -Path '$($script:ViewFixture.Replace('EverydayViews.psd1', 'EverydayViews.consoleviews.psd1'))'
`$text = Get-EverydayValue | Format-ConsoleTable -AsString -Width 80
if (`$text -notmatch 'Health') { throw 'Explicit import failed' }
'EXPLICIT_VIEWS_OK'
"@
        $LASTEXITCODE | Should -Be 0
        ($result -join "`n") | Should -Match 'EXPLICIT_VIEWS_OK'
    }

    It 'combines types sharing a module view group on first <Kind> rendering' -ForEach @(
        @{ Kind = 'Table' }
        @{ Kind = 'List' }
    ) {
        $result = Invoke-TestChild -Runtime $script:ViewRuntime -Script @"
`$ErrorActionPreference = 'Stop'
Import-Module '$($script:ViewModule)'
Import-Module '$($script:ViewFixture)'
`$first = @(Get-EverydayValue)[0]
`$second = [pscustomobject]@{PSTypeName='EverydayViews.OtherServer';Name='gamma';State='Pass';Site='A'}
`$firstRender = @(`$first, `$second) | Format-Console$Kind -GroupBy Site -AsString -Width 80
`$secondRender = @(`$first, `$second) | Format-Console$Kind -GroupBy Site -AsString -Width 80
if (`$firstRender -cne `$secondRender) { throw 'First-use group layout differs from later rendering' }
`$text = `$firstRender
if ('$Kind' -eq 'Table' -and [regex]::Matches(`$text, 'Health').Count -ne 1) { throw 'The shared group was split into separate tables' }
if (`$text -notmatch 'alpha' -or `$text -notmatch 'gamma') { throw 'A shared-group row was lost' }
'SHARED_VIEW_GROUP_OK'
"@
        $LASTEXITCODE | Should -Be 0
        ($result -join "`n") | Should -Match 'SHARED_VIEW_GROUP_OK'
    }

    It 'rejects unsupported schema data' {
        $path = Join-Path $TestDrive 'invalid.consoleviews.psd1'
        "@{ SchemaVersion = 2; Views = @() }" | Set-Content -LiteralPath $path
        $result = Invoke-TestChild -Runtime $script:ViewRuntime -Script @"
`$ErrorActionPreference = 'Stop'
Import-Module '$($script:ViewModule)'
try {
    Register-ConsoleView -Path '$path'
    throw 'Invalid schema was accepted'
} catch {
    if (`$_.Exception.Message -notmatch 'SchemaVersion') { throw }
}
'INVALID_VIEWS_REJECTED'
"@
        $LASTEXITCODE | Should -Be 0
        ($result -join "`n") | Should -Match 'INVALID_VIEWS_REJECTED'
    }

    It 'validates all column options before registering any view' {
        $path = Join-Path $TestDrive 'atomic.consoleviews.psd1'
        @'
@{
    SchemaVersion = 1
    Views = @(
        @{ TypeName = 'ConsoleMonster.Tests.AtomicView'; Property = @('Name') }
        @{ TypeName = 'ConsoleMonster.Tests.BadView'; Property = @(@{ Name = 'Bad'; UnsupportedColumnOption = 1 }) }
    )
}
'@ | Set-Content -LiteralPath $path
        $result = Invoke-TestChild -Runtime $script:ViewRuntime -Script @"
`$ErrorActionPreference = 'Stop'
Import-Module '$($script:ViewModule)'
try {
    Register-ConsoleView -Path '$path'
    throw 'Invalid columns were accepted'
} catch {
    if (`$_.Exception.Message -notmatch 'UnsupportedColumnOption') { throw }
}
`$row = [pscustomobject]@{PSTypeName='ConsoleMonster.Tests.AtomicView';Name='atomic';Detail='retained-detail'}
`$text = `$row | Format-ConsoleTable -AsString
if (`$text -notmatch 'retained-detail') { throw 'A partial file was installed' }
'ATOMIC_VIEWS_OK'
"@
        $LASTEXITCODE | Should -Be 0
        ($result -join "`n") | Should -Match 'ATOMIC_VIEWS_OK'
    }
}
