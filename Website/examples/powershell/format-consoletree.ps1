Import-Module ConsoleMonster

$certificate = [pscustomobject] @{
    Subject                 = 'CN=mail.evotec.xyz'
    Issuer                  = "CN=R11, O=Let's Encrypt, C=US"
    KeyAlgorithm            = 'ECDSA P-256'
    SubjectAlternativeNames = @('mail.evotec.xyz', 'autodiscover.evotec.xyz', 'mta-sts.evotec.xyz')
    Chain                   = @(
        [pscustomobject] @{ Subject = 'CN=mail.evotec.xyz'; Key = 'ECDSA P-256'; Revoked = $false }
        [pscustomobject] @{ Subject = "CN=R11, O=Let's Encrypt"; Key = 'RSA 2048'; Revoked = $false }
        [pscustomobject] @{ Subject = 'CN=ISRG Root X1'; Key = 'RSA 4096'; Revoked = $false }
    )
}
$certificate | Format-ConsoleTree -AsInspector -Title 'mail.evotec.xyz certificate'
