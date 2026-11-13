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
