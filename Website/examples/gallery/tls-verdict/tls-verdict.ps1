# A deterministic TLS chain story. Data and timings are fictional; no hosts are contacted.
param(
    [ValidateSet('Healthy', 'ExpiredIntermediate')]
    [string] $Scenario = 'Healthy'
)
Import-Module ConsoleMonster

<# Real data plugs in here: map the output of your TLS checker to these rows.
   Keep connection results, certificate dates and revocation evidence separate.
   A successful handshake alone does not establish trust. #>
$expired = $Scenario -eq 'ExpiredIntermediate'
$chainStatus = if ($expired) { 'Fail' } else { 'Pass' }
$verdict = if ($expired) { 'UNTRUSTED' } else { 'TRUSTED' }
$hostName = if ($expired) { 'legacy.example.test' } else { 'portal.example.test' }
$steps = @(
    [pscustomobject] @{ Key = 'dns'; Label = 'Resolve DNS'; Status = 'Pass'; Detail = '203.0.113.42' }
    [pscustomobject] @{ Key = 'connect'; Label = 'Connect :443'; Status = 'Pass'; Detail = 'connected' }
    [pscustomobject] @{ Key = 'handshake'; Label = 'TLS handshake'; Status = 'Pass'; Detail = 'TLS 1.3' }
    [pscustomobject] @{ Key = 'chain'; Label = 'Validate chain'; Status = $chainStatus; Detail = $(if ($expired) { 'expired intermediate' } else { 'verified' }) }
    [pscustomobject] @{ Key = 'expiry'; Label = 'Expiry and revocation'; Status = $(if ($expired) { 'Skip' } else { 'Pass' }); Detail = $(if ($expired) { 'skipped' } else { 'valid / good' }) }
)
$context = @(
    [pscustomobject] @{ Key = 'Host'; Value = $hostName }
    [pscustomobject] @{ Key = 'IP'; Value = '203.0.113.42' }
    [pscustomobject] @{ Key = 'Protocol'; Value = 'TLS 1.3' }
    [pscustomobject] @{ Key = 'Cipher'; Value = 'AES_256_GCM_SHA384' }
    [pscustomobject] @{ Key = 'Issuer'; Value = 'Example Issuing CA' }
    [pscustomobject] @{ Key = 'Valid until'; Value = '2027-04-15 (leaf)' }
)
$nodes = @(
    [pscustomobject] @{ Kind = 'Node'; Key = 'client'; Label = 'client'; Status = 'Pass' }
    [pscustomobject] @{ Kind = 'Node'; Key = 'host'; Label = 'host:443'; Status = 'Pass' }
    [pscustomobject] @{ Kind = 'Node'; Key = 'leaf'; Label = 'leaf'; Status = 'Pass' }
    [pscustomobject] @{ Kind = 'Node'; Key = 'intermediate'; Label = 'intermediate'; Status = $chainStatus }
    [pscustomobject] @{ Kind = 'Node'; Key = 'root'; Label = 'root'; Status = $(if ($expired) { 'Skip' } else { 'Pass' }) }
)
$edges = @(
    [pscustomobject] @{ Kind = 'Edge'; Key = 'connect'; From = 'client'; To = 'host'; Label = ':443' }
    [pscustomobject] @{ Kind = 'Edge'; Key = 'tls'; From = 'host'; To = 'leaf'; Label = 'TLS' }
    [pscustomobject] @{ Kind = 'Edge'; Key = 'issuer'; From = 'leaf'; To = 'intermediate'; Label = 'issuer'; Status = $chainStatus; Cut = $expired }
    [pscustomobject] @{ Kind = 'Edge'; Key = 'anchor'; From = 'intermediate'; To = 'root'; Label = 'anchor' }
)
$flow = @($nodes; $edges) | New-ConsoleFlow -WidgetId chain
if ($expired) {
    $flow | Add-ConsoleItem {
        New-ConsoleFlowBranch -From intermediate -Label 'expired 2026-09-30' -Status Fail
    }
}
$proof = if ($expired) { 'Intermediate expired before 2026-10-25.' } else { 'All three certificates valid on 2026-10-25.' }
$action = if ($expired) { 'Renew the intermediate; serve the full chain.' } else { 'No repair needed; keep expiry monitoring on.' }
$exitCode = if ($expired) { 1 } else { 0 }

# demo: pause 1
New-ConsoleRows -Gap 1 {
    '[bold accent]TLS verdict[/]  [muted]Follow the chain. Verify the trust.[/]'
    New-ConsolePanel -Title 'Connection ledger' {
        $steps | New-ConsolePhases -StepCounter
    }
    New-ConsoleKeyValue -Columns 2 -KeyRole Accent -Content {
        foreach ($pair in $context) {
            New-ConsoleKeyValueItem -Key $pair.Key -Value $pair.Value
        }
    }
    $flow
    New-ConsoleRule -Title 'Trust decision'
    $verdictOptions = @{
        Title = $verdict
        Verdict = $true
        Status = $chainStatus
    }
    New-ConsolePanel @verdictOptions {
        New-ConsoleText -Text $(if ($expired) { 'Chain validation failed.' } else { 'Chain validation passed.' })
    }
    New-ConsoleKeyValue -KeyRole $chainStatus -Content {
        New-ConsoleKeyValueItem -Key 'Cause' -Value $(if ($expired) { 'Expired intermediate certificate.' } else { 'Trusted root and complete chain.' })
        New-ConsoleKeyValueItem -Key 'Proof' -Value $proof
        New-ConsoleKeyValueItem -Key 'Action' -Value $action
        New-ConsoleKeyValueItem -Key 'Exit' -Value "$exitCode · 184 ms"
    }
} | Show-Console
# demo: pause 6
