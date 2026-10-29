# On a desktop with working Out-GridView, the native window remains in charge.
Import-Module ConsoleMonster
$previousFeatures = @((Get-ConsoleFormatting).Features)
Enable-ConsoleFormatting -Include GridView
$nodes = @(
    [pscustomobject] @{ Name = 'api-east'; Region = 'eu-west'; Version = '2.4.1' }
    [pscustomobject] @{ Name = 'worker-west'; Region = 'eu-west'; Version = '2.4.0' }
    [pscustomobject] @{ Name = 'build-north'; Region = 'eu-north'; Version = '2.4.1' }
)
try {
    $nodes | Out-GridView -Title 'Choose nodes' -OutputMode Multiple
} finally {
    $addedFeatures = @((Get-ConsoleFormatting).Features | Where-Object {
        $_ -notin $previousFeatures
    })
    if ($addedFeatures.Count -gt 0) {
        Disable-ConsoleFormatting -Feature $addedFeatures
    }
}
