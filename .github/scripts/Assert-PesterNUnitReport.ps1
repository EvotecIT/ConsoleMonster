<#
.SYNOPSIS
Requires a readable Pester NUnit 2 report before CI can mark a shard successful.
.PARAMETER Path
Literal path to the NUnit XML written by the shard.
#>
param(
    [Parameter(Mandatory)]
    [string] $Path
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    throw "Pester NUnit report was not found at '$Path'."
}

$settings = [System.Xml.XmlReaderSettings]::new()
$settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
$settings.XmlResolver = $null
$report = [System.Xml.XmlDocument]::new()
$reader = [System.Xml.XmlReader]::Create((Resolve-Path -LiteralPath $Path).ProviderPath, $settings)
try {
    $report.Load($reader)
} catch [System.Xml.XmlException] {
    throw "Pester NUnit report at '$Path' contains malformed XML. $($_.Exception.Message)"
} finally {
    $reader.Dispose()
}

$results = $report.SelectSingleNode('/test-results[test-suite]')
if ($null -eq $results) {
    throw "Pester NUnit report at '$Path' must contain a test-results root with a test-suite."
}

foreach ($counter in 'total', 'errors', 'failures') {
    $value = 0
    if (-not [int]::TryParse($results.GetAttribute($counter), [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$value)) {
        throw "Pester NUnit report at '$Path' has a missing or invalid '$counter' count."
    }
}
