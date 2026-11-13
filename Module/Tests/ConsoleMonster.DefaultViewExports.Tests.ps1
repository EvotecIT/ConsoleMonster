Describe 'Registered default view export failures' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:ExportRuntime = [Diagnostics.Process]::GetCurrentProcess().Path
        $module = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
        $fixture = Join-Path $PSScriptRoot 'Support/ExportDefaultViewFixture.cs'
        $script:ExportSetup = @'
$ErrorActionPreference = 'Stop'
Import-Module '__MODULE__'
$core = [ConsoleMonster.Terminal].Assembly
$displayPath = Join-Path (Split-Path $core.Location) 'ConsoleMonster.PowerShell.Display.dll'
$displayAssembly = if ($PSEdition -eq 'Core') {
    [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext($core).LoadFromAssemblyPath($displayPath)
} else {
    [Reflection.Assembly]::LoadFrom($displayPath)
}
$references = @($core.Location, $displayAssembly.Location, [System.Management.Automation.PSObject].Assembly.Location)
if ($PSEdition -eq 'Core') {
    $references += @(Get-ChildItem (Join-Path $PSHOME 'ref') -Filter *.dll | ForEach-Object { $_.FullName })
} else {
    $references += 'System.Core'
}
$compile = @{}
if ($PSEdition -eq 'Core') { $compile.CompilerOptions = '/nowarn:1701' }
$assemblyPath = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N') + '.dll')
try {
    Add-Type -Path '__FIXTURE__' -ReferencedAssemblies $references -OutputAssembly $assemblyPath @compile
    $bytes = [IO.File]::ReadAllBytes($assemblyPath)
    if ($PSEdition -eq 'Core') {
        $context = [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext($core)
        $stream = [IO.MemoryStream]::new($bytes)
        try { $assembly = $context.LoadFromStream($stream) } finally { $stream.Dispose() }
    } else {
        $assembly = [Reflection.Assembly]::Load($bytes)
    }
    $fixture = $assembly.GetType('ExportDefaultViews.Fixture', $true)
    $record = $assembly.GetType('ExportDefaultViews.Record', $true)
} finally {
    if (Test-Path -LiteralPath $assemblyPath) { Remove-Item -LiteralPath $assemblyPath }
}
'@.Replace('__MODULE__', $module.Replace("'", "''")).Replace('__FIXTURE__', $fixture.Replace("'", "''"))
    }

    It 'keeps complete automatic rows through captured host and deferred Show-Console rendering' {
        $body = @'
Set-ConsoleSetting Culture en-US
$rows = 1..3 | ForEach-Object {
    $row = [Activator]::CreateInstance($record)
    $row.Name = 'row-marker-' + $_
    $row.Detail = 'original-detail-' + $_
    $row.Amount = 1234.5
    $row
}
function Invoke-HostDisplay {
    param($Rows)
    $Rows | Show-Console -Title 'fallback-title' -Depth 2 -Culture de-DE -Width 60 6>&1
}
$routes = @('Capture', 'Host')
if ($PSEdition -eq 'Core' -and $PSVersionTable.PSVersion -ge [version]'7.2') { $routes += 'Deferred' }
$routeFailures = [Collections.Generic.List[string]]::new()
foreach ($route in $routes) {
    $lease = $fixture::Register('Stream', 16384, 'late-render')
    try {
        if ($route -eq 'Capture') {
            $actual = $rows | Show-Console -Title 'fallback-title' -Depth 2 -Culture de-DE -Width 60 | Out-String
        } elseif ($route -eq 'Host') {
            $actual = @(Invoke-HostDisplay $rows) -join "`n"
        } else {
            $deferred = $rows | Show-Console -Title 'fallback-title' -Depth 2 -Culture de-DE -Width 60
            if ($deferred.GetType().FullName -ne 'ConsoleMonster.PowerShell.ConsoleRendering') { throw 'The actual deferred route was not reached' }
            # Complete the command and remove its registration before the formatting consumer renders it.
            $lease.Dispose()
            $actual = $deferred.ToText($Host)
        }
        $lease.Dispose()
        if ($route -eq 'Capture') {
            $expected = $rows | Show-Console -Title 'fallback-title' -Depth 2 -Culture de-DE -Width 60 | Out-String
        } elseif ($route -eq 'Host') {
            $expected = @(Invoke-HostDisplay $rows) -join "`n"
        } else {
            $expectedView = $rows | Show-Console -Title 'fallback-title' -Depth 2 -Culture de-DE -Width 60
            $expected = $expectedView.ToText($Host)
        }
        if ($actual.Replace("`r`n", "`n").Trim() -cne $expected.Replace("`r`n", "`n").Trim()) { throw "Show fallback differed on $route" }
        if ($actual -notmatch 'fallback-title' -or $actual -notmatch 'row-marker-3' -or $actual -match 'partial-view-marker') { throw "Show fallback lost rows or retained partial rendering on $route" }
        if ($actual.IndexOf('row-marker-1') -ge $actual.IndexOf('row-marker-2') -or $actual.IndexOf('row-marker-2') -ge $actual.IndexOf('row-marker-3')) { throw "Show fallback changed row order on $route" }
    } catch {
        $routeFailures.Add($route + ': ' + $_.Exception.Message)
    } finally { $lease.Dispose() }
}
if ($routeFailures.Count -gt 0) { throw ($routeFailures -join "`n") }
$lease = $fixture::Register('Stream', 16384, 'cancel-render')
try {
    $failure = $null
    try { $rows | Show-Console -Width 60 | Out-String | Out-Null } catch { $failure = $_ }
    if ($null -eq $failure -or $failure.Exception.ToString() -notmatch 'application rendering canceled') { throw 'Show fallback swallowed rendering cancellation' }
} finally { $lease.Dispose() }
'SHOW_RENDER_LIFECYCLE_OK'
'@
        $result = Invoke-TestChild -Runtime $script:ExportRuntime -Script ("try {`n" + $script:ExportSetup + "`n" + $body + "`n} catch { [Console]::Out.WriteLine((`$_ | Out-String)); exit 1 }")
        if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
        $result -join "`n" | Should -Match 'SHOW_RENDER_LIFECYCLE_OK'
    }

    It 'builds registered templates with the requested Show-Console culture' {
        $path = Join-Path $TestDrive 'culture.consoleviews.psd1'
        @'
@{ SchemaVersion = 2; Views = @(
    @{ TypeName = 'ExportDefaultViews.Record'; ViewId = 'Culture.Record'; View = 'Table'; Batch = 'Collect';
       Title = 'amount={Amount}'; Summary = 'summary={Amount}'; Property = @('Name', 'Amount') }
) }
'@ | Set-Content -LiteralPath $path
        $body = @'
Set-ConsoleSetting Culture en-US
Enable-ConsoleFormatting
Register-ConsoleView -Path '__PATH__'
$row = [Activator]::CreateInstance($record)
$row.Name = 'culture-marker'
$row.Amount = 1234.5
foreach ($requested in 'de-DE', 'pl-PL', 'en-US', '') {
    $arguments = @{ Width = 80 }
    if ($requested) { $arguments.Culture = $requested }
    $text = $row | Show-Console @arguments | Out-String
    $number = if ($requested -in 'de-DE', 'pl-PL') { '1234,5' } else { '1234.5' }
    if ($text -notmatch [regex]::Escape('amount=' + $number) -or $text -notmatch [regex]::Escape('summary=' + $number)) {
        throw "Registered template ignored requested culture '$requested': $text"
    }
}
'CULTURE_CONTEXT_OK'
'@.Replace('__PATH__', $path.Replace("'", "''"))
        $result = Invoke-TestChild -Runtime $script:ExportRuntime -Script ("try {`n" + $script:ExportSetup + "`n" + $body + "`n} catch { [Console]::Out.WriteLine((`$_ | Out-String)); exit 1 }")
        if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
        $result -join "`n" | Should -Match 'CULTURE_CONTEXT_OK'
    }

    It 'discovers late module default views on the first export or automatic display' {
        $moduleDirectory = Join-Path $TestDrive 'LateExportViews'
        New-Item -ItemType Directory -Path $moduleDirectory | Out-Null
        $modulePath = Join-Path $moduleDirectory 'LateExportViews.psm1'
        '# Data-only view fixture' | Set-Content -LiteralPath $modulePath
        @'
@{ SchemaVersion = 2; Views = @(
    @{ TypeName = 'ExportDefaultViews.Record'; ViewId = 'LateExportViews.Record'; View = 'Table'; Batch = 'Collect';
       Title = 'late-registration-marker'; Property = @('Name') }
) }
'@ | Set-Content -LiteralPath (Join-Path $moduleDirectory 'LateExportViews.consoleviews.psd1')
        foreach ($route in 'Export', 'Show') {
            $body = @'
Enable-ConsoleFormatting
Import-Module '__MODULE__'
$row = [Activator]::CreateInstance($record)
$row.Name = 'late-row-marker'
$row.Detail = 'private-detail'
$first = if ('__ROUTE__' -eq 'Export') { $row | Export-ConsoleFrame -Format Text -Width 80 } else { $row | Show-Console -Width 80 | Out-String }
$second = if ('__ROUTE__' -eq 'Export') { $row | Export-ConsoleFrame -Format Text -Width 80 } else { $row | Show-Console -Width 80 | Out-String }
if ($first -notmatch 'late-registration-marker' -or $first -match 'private-detail' -or $first -cne $second) {
    throw "First __ROUTE__ missed the late module view: $first"
}
'FIRST_DISCOVERY_OK'
'@.Replace('__MODULE__', $modulePath.Replace("'", "''")).Replace('__ROUTE__', $route)
            $result = Invoke-TestChild -Runtime $script:ExportRuntime -Script ("try {`n" + $script:ExportSetup + "`n" + $body + "`n} catch { [Console]::Out.WriteLine((`$_ | Out-String)); exit 1 }")
            if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
            $result -join "`n" | Should -Match 'FIRST_DISCOVERY_OK'
        }
    }

    It 'resolves explicit collection rows while preserving collection and rendering boundaries' {
        $body = @'
$lease = $fixture::Register('Stream', 16384, 'none')
try {
    $rows = 1..2 | ForEach-Object {
        $row = [Activator]::CreateInstance($record)
        $row.Name = 'array-row-marker'
        $row.Detail = 'explicit-detail'
        $row
    }
    foreach ($format in 'Text', 'Markdown', 'Html', 'Svg') {
        $text = Export-ConsoleFrame -InputObject $rows -Format $format -Width 80
        if ($text -notmatch 'registered-marker' -or $text -match 'explicit-detail') { throw "Explicit array ignored row registration for $format" }
    }
    $shown = Show-Console -InputObject $rows -Width 80 | Out-String
    if ($shown -notmatch 'registered-marker') { throw 'Explicit Show array ignored row registration' }
    $listType = $assembly.GetType('ExportDefaultViews.RecordList', $true)
    $list = [Activator]::CreateInstance($listType)
    foreach ($row in $rows) { $list.Add($row) }
    $collectionLease = $fixture::RegisterCollection()
    try {
        $text = Export-ConsoleFrame -InputObject $list -Format Text -Width 80
        if ($text -notmatch 'collection-marker' -or $text -match 'registered-marker') { throw 'Registered collection identity was lost' }
    } finally { $collectionLease.Dispose() }
    $dictionary = @{ DictionaryKey = $rows[0] }
    $text = Export-ConsoleFrame -InputObject $dictionary -Format Text -Width 80
    if ($text -notmatch 'DictionaryKey' -or $text -match 'registered-marker') { throw 'Dictionary was expanded as registered rows' }
    $options = ($core.GetType('ConsoleMonster.Export.ExportOptions'))::new()
    $options.Width = 30
    $frame = ($core.GetType('ConsoleMonster.Export.ConsoleExporter'))::Render((New-ConsoleText 'frame-marker'), $options, [ConsoleMonster.Styling.Theme]::Dark)
    $text = Export-ConsoleFrame -InputObject @((New-ConsoleText 'widget-marker'), $frame, $rows[0]) -Format Text -Width 80
    if ($text.IndexOf('widget-marker') -lt 0 -or $text.IndexOf('frame-marker') -le $text.IndexOf('widget-marker') -or $text.IndexOf('registered-marker') -le $text.IndexOf('frame-marker')) { throw 'Collection rendering boundaries or order changed' }
    Register-ConsoleView -TypeName ExportDefaultViews.Record -Property Detail
    $text = Export-ConsoleFrame -InputObject $rows -Format Text -Width 80
    if ($text -notmatch 'explicit-detail' -or $text -match 'registered-marker') { throw 'Schema 1 precedence changed' }
    Unregister-ConsoleView -TypeName ExportDefaultViews.Record
    $text = Show-Console -InputObject $rows -Property Detail -Width 80 | Out-String
    if ($text -notmatch 'explicit-detail' -or $text -match 'registered-marker') { throw 'Explicit Property precedence changed' }
} finally { $lease.Dispose() }
'COLLECTION_INPUT_OK'
'@
        $result = Invoke-TestChild -Runtime $script:ExportRuntime -Script ("try {`n" + $script:ExportSetup + "`n" + $body + "`n} catch { [Console]::Out.WriteLine((`$_ | Out-String)); exit 1 }")
        if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
        $result -join "`n" | Should -Match 'COLLECTION_INPUT_OK'
    }

    It 'enforces collected input bytes and per-view output bytes across batch modes' {
        $body = @'
foreach ($batch in 'Collect', 'Stream', 'Single') {
    foreach ($failure in 'input', 'output') {
        if ($batch -eq 'Single' -and $failure -eq 'input') { continue }
        $lease = $fixture::Register($batch, 512, $failure)
        try {
            $row = [Activator]::CreateInstance($record)
            $row.Name = 'original-marker'
            $row.Detail = if ($failure -eq 'input') { 'z' * 2000 } else { 'detail-marker' }
            $text = $row | Export-ConsoleFrame -Format Text -Width 80
            if ($text -notmatch 'original-marker' -or $text -match 'registered-marker|xxxx') { throw "Budget ignored for $batch/$failure" }
        } finally { $lease.Dispose() }
    }
    $lease = $fixture::Register($batch, 16384, 'none')
    try {
        $rows = 1..2 | ForEach-Object { $row = [Activator]::CreateInstance($record); $row.Name = 'original-marker'; $row }
        $text = $rows | Export-ConsoleFrame -Format Text -Width 80
        if ($batch -eq 'Collect') {
            if ($text -notmatch 'original-marker' -or $text -match 'registered-marker') { throw 'Collect record budget ignored' }
        } elseif ($text -notmatch 'registered-marker') { throw "$batch incorrectly used the Collect record cap" }
    } finally { $lease.Dispose() }
}
'BUDGETS_OK'
'@
        $result = Invoke-TestChild -Runtime $script:ExportRuntime -Script ("try {`n" + $script:ExportSetup + "`n" + $body + "`n} catch { [Console]::Out.WriteLine((`$_ | Out-String)); exit 1 }")
        if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
        $result -join "`n" | Should -Match 'BUDGETS_OK'
    }

    It 'keeps later records and registrations usable after application selector access errors' {
        $body = @'
foreach ($failure in 'scope-member', 'scope-type', 'resolve-member', 'resolve-type') {
    $lease = $fixture::Register('Collect', 16384, $failure)
    try {
        $bad = [Activator]::CreateInstance($record)
        $bad.Name = 'bad'
        $bad.Detail = 'original-marker'
        $good = [Activator]::CreateInstance($record)
        $good.Name = 'good'
        $text = @($bad, $good) | Export-ConsoleFrame -Format Text -Width 80
        if ($text -notmatch 'original-marker' -or $text -notmatch 'registered-marker') { throw "Selector poisoned later records for $failure" }
    } finally { $lease.Dispose() }
}
$lease = $fixture::Register('Collect', 16384, 'none')
try {
    $text = [Activator]::CreateInstance($record) | Export-ConsoleFrame -Format Text
    if ($text -notmatch 'registered-marker') { throw 'Selector poisoned a later registration' }
} finally { $lease.Dispose() }
'SELECTORS_OK'
'@
        $result = Invoke-TestChild -Runtime $script:ExportRuntime -Script ("try {`n" + $script:ExportSetup + "`n" + $body + "`n} catch { [Console]::Out.WriteLine((`$_ | Out-String)); exit 1 }")
        if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
        $result -join "`n" | Should -Match 'SELECTORS_OK'
    }

    It 'falls back to original rows through actual document and PNG rendering' {
        $body = @'
foreach ($failure in 'builder', 'measure', 'render', 'late-render') {
    foreach ($format in 'Text', 'Html', 'Svg', 'Markdown', 'FencedText', 'Png') {
        $lease = $fixture::Register('Collect', 16384, $failure)
        try {
            $row = [Activator]::CreateInstance($record)
            $row.Name = 'original-marker'
            $row.Detail = 'detail-marker'
            $actual = $row | Export-ConsoleFrame -Format $format -Width 80 -Theme Dark
            $lease.Dispose()
            $expected = $row | Export-ConsoleFrame -Format $format -Width 80 -Theme Dark
            if ($format -eq 'Png') {
                if ([Convert]::ToBase64String([byte[]]$actual) -cne [Convert]::ToBase64String([byte[]]$expected)) { throw "PNG fallback differs for $failure" }
            } elseif ($actual -cne $expected -or $actual -notmatch 'original-marker') { throw "Fallback differs for $failure/$format" }
        } finally { $lease.Dispose() }
    }
}
'RENDER_FALLBACK_OK'
'@
        $result = Invoke-TestChild -Runtime $script:ExportRuntime -Script ("try {`n" + $script:ExportSetup + "`n" + $body + "`n} catch { [Console]::Out.WriteLine((`$_ | Out-String)); exit 1 }") -TimeoutSeconds 180
        if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
        $result -join "`n" | Should -Match 'RENDER_FALLBACK_OK'
    }
}
