Import-Module ConsoleMonster
Enable-ConsoleFormatting

# Search text: matches highlighted, -Context lines dimmed.
$log = 'Started', 'Warning: api-02 is slow', 'Checked 12', 'Error: api-03 timed out', 'Done'
$log | Select-String -Pattern 'Warning|Error' -Context 1

# Who can open a file, and whether each rule is inherited.
Get-Acl -Path "$env:windir\win.ini"

# Three invented certificates, created in memory: expired, expiring and valid.
$key = [System.Security.Cryptography.RSA]::Create(2048)
$padding = [System.Security.Cryptography.RSASignaturePadding]::Pkcs1
$today = [datetime]::Today
foreach ($site in @{ vpn = -3; api = 9; portal = 120 }.GetEnumerator() | Sort-Object Value) {
    $arguments = "CN=$($site.Key).example.test", $key, 'SHA256', $padding
    $request = New-Object Security.Cryptography.X509Certificates.CertificateRequest $arguments
    $request.CreateSelfSigned($today.AddDays(-365), $today.AddDays($site.Value))
}

# Counts, statistics and elapsed time.
'Info', 'Warning', 'Info', 'Error', 'Info' | Group-Object | Sort-Object Count -Descending
12, 48, 7, 30 | Measure-Object -Sum -Average -Minimum -Maximum
New-TimeSpan -Start '2026-10-21 12:00' -End '2026-10-21 13:42:05'
