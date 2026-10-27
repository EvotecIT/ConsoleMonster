param(
    [int] $Width,
    [ValidateSet('All', 'Artifacts', 'Checks', 'Certificate', 'List')][string] $Part = 'All'
)
# Tables and complex data from PowerShell: Show-ConsoleTable, Show-ConsoleList and Show-ConsoleTree with
# presenters, column options, groups, totals and nested objects. -Width lays the output out for a given width (try 120, 80
# and 50 to see columns hide and rows turn into cards). The data is synthetic.
Import-Module ConsoleMonster

$now = Get-Date
$artifacts = @(
    [pscustomobject] @{ Name = 'ConsoleMonster.2.0.0.nupkg'; Kind = 'NuGet'; Length = 1482113; Time = [timespan]::FromSeconds(48.2); Created = $now.AddMinutes(-12); Path = 'C:\Support\GitHub\ConsoleMonster\Artefacts\Packed\ConsoleMonster.2.0.0.nupkg'; Signed = $true }
    [pscustomobject] @{ Name = 'ConsoleMonster.2.0.0.snupkg'; Kind = 'Symbols'; Length = 402331; Time = [timespan]::FromSeconds(3.1); Created = $now.AddMinutes(-12); Path = 'C:\Support\GitHub\ConsoleMonster\Artefacts\Packed\ConsoleMonster.2.0.0.snupkg'; Signed = $true }
    [pscustomobject] @{ Name = 'ConsoleMonster-2.0.0.zip'; Kind = 'Module'; Length = 2904576; Time = [timespan]::FromSeconds(11.7); Created = $now.AddMinutes(-11); Path = 'C:\Support\GitHub\ConsoleMonster\Artefacts\Packed\ConsoleMonster-2.0.0.zip'; Signed = $true }
    [pscustomobject] @{ Name = 'Docs.zip'; Kind = 'Module'; Length = 51302; Time = [timespan]::FromSeconds(1.2); Created = $now.AddDays(-2); Path = 'C:\Support\GitHub\ConsoleMonster\Artefacts\Packed\Docs.zip'; Signed = $false }
)
$common = @{}
if ($Width -gt 0) {
    $common.Width = $Width
}

if ($Part -in 'All', 'Artifacts') { $artifacts | Show-ConsoleTable -Property @(
    @{ Name = 'Name'; Key = $true; Overflow = 'Never' }
    @{ Name = 'Length'; Header = 'Size'; Presenter = 'Bytes'; Priority = 9 }
    @{ Name = 'Time'; Header = 'Built in'; Presenter = 'Duration'; Priority = 6 }
    @{ Name = 'Created'; Presenter = 'DateRelative'; Priority = 4 }
    @{ Name = 'Signed'; Presenter = 'Boolean'; Priority = 5 }
    @{ Name = 'Path'; Overflow = 'TruncateMiddle'; Priority = 1; MinWidth = 20 }
) -Title '[bold]Build artifacts[/]' -GroupBy Kind -ShowTotals }

$checks = @(
    [pscustomobject] @{ Check = 'SPF'; Status = 'Pass'; Detail = '9 DNS lookups (limit 10); ends with -all'; Took = [timespan]::FromMilliseconds(84) }
    [pscustomobject] @{ Check = 'DKIM'; Status = 'Warn'; Detail = 'selector2 key is 1024-bit; rotate it to a 2048-bit key'; Took = [timespan]::FromMilliseconds(212) }
    [pscustomobject] @{ Check = 'MTA-STS'; Status = 'Fail'; Detail = 'policy file returns 404 at https://mta-sts.evotec.xyz/.well-known/mta-sts.txt'; Took = [timespan]::FromSeconds(1.4) }
    [pscustomobject] @{ Check = 'DNSSEC'; Status = 'Warn'; Detail = 'zone is not signed'; Took = [timespan]::FromMilliseconds(95) }
)
if ($Part -in 'All', 'Checks') { $checks | Show-ConsoleTable @common -Title '[bold]evotec.xyz[/] [muted]· mail security checks[/]' -Property Check, @{ Name = 'Status'; Presenter = 'Status'; Priority = 10 }, @{ Name = 'Detail'; MaxLines = 2; Priority = 5 }, @{ Name = 'Took'; Presenter = 'Duration'; Priority = 1; WarnAbove = 1 } }

$certificate = [pscustomobject] @{
    Subject = 'CN=mail.evotec.xyz'
    Issuer = "CN=R11, O=Let's Encrypt, C=US"
    NotAfter = $now.AddDays(23)
    KeyAlgorithm = 'ECDSA P-256'
    SubjectAlternativeNames = @('mail.evotec.xyz', 'autodiscover.evotec.xyz', 'mta-sts.evotec.xyz')
    Chain = @(
        [pscustomobject] @{ Subject = 'CN=mail.evotec.xyz'; Expires = $now.AddDays(23); Key = 'ECDSA P-256' }
        [pscustomobject] @{ Subject = "CN=R11, O=Let's Encrypt"; Expires = $now.AddDays(147); Key = 'RSA 2048' }
        [pscustomobject] @{ Subject = 'CN=ISRG Root X1'; Expires = $now.AddYears(8); Key = 'RSA 4096' }
    )
}
if ($Part -in 'All', 'Certificate') { $certificate | Show-ConsoleObject @common -StaticSnapshot -Title 'mail.evotec.xyz certificate' }
if ($Part -in 'All', 'List') { $artifacts[0] | Show-ConsoleList @common -Property Name, Kind, @{ Name = 'Length'; Header = 'Size'; Presenter = 'Bytes' }, @{ Name = 'Time'; Header = 'Built in'; Presenter = 'Duration' }, Signed, Path }
