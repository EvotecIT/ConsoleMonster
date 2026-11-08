Import-Module ConsoleMonster
Enable-ConsoleFormatting

# An invented release folder with fixed sizes and times.
$temp = if ($IsMacOS -or $IsLinux) { '/tmp' } else { $env:TEMP }
$root = Join-Path $temp 'release'
if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
$files = [ordered] @{
    'bin/ConsoleMonster.dll' = 1048576
    'bin/ConsoleMonster.xml' = 262144
    'bin/runtimes/libcm.so'  = 393216
    'docs/README.md'         = 15360
    'docs/CHANGELOG.md'      = 63488
    'install.ps1'            = 2048
}
foreach ($name in $files.Keys) {
    $path = Join-Path $root $name
    $null = New-Item -ItemType Directory -Path (Split-Path $path) -Force
    [System.IO.File]::WriteAllBytes($path, (New-Object byte[] $files[$name]))
}
$stamp = [datetime] '2026-10-21 12:00'
Get-ChildItem $root -Recurse -Force | ForEach-Object { $_.LastWriteTime = $stamp }
(Get-Item -LiteralPath $root).LastWriteTime = $stamp

# A recursive listing is one tree with folder sizes; the line under it sums it up.
Get-ChildItem -LiteralPath $root -Recurse

# Your own types can sum themselves up too.
$state = @{ Name = 'State'; Expression = { $_.State }; Presenter = 'StatusWord' }
$disk = @{ Name = 'Disk'; Expression = { $_.Disk }; Presenter = 'BytesBar' }
Register-ConsoleView -TypeName Contoso.Server -Property Name, $state, $disk -Summary {
    param($servers)
    @{ Value = $servers.Count; Label = 'servers' }
    $down = @($servers | Where-Object State -ne 'Running').Count
    if ($down) { @{ Value = $down; Label = 'stopped'; Role = 'Warn' } }
    $total = ($servers | Measure-Object Disk -Sum).Sum
    @{ Caption = 'disk'; Value = $total; Presenter = 'Bytes' }
}
$servers = @(
    [pscustomobject] @{ Name = 'web-01'; State = 'Running'; Disk = 120GB }
    [pscustomobject] @{ Name = 'web-02'; State = 'Running'; Disk = 96GB }
    [pscustomobject] @{ Name = 'db-01'; State = 'Stopped'; Disk = 410GB }
)
$servers | ForEach-Object { $_.PSObject.TypeNames.Insert(0, 'Contoso.Server') }
$servers
