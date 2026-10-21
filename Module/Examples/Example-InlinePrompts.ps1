param(
    [string] $Environment,
    [string[]] $Component,
    [string] $Release,
    [switch] $Force
)
# A normal script that asks only for what it was not given: each prompt draws below the cursor, answers stay in the
# scrollback as one line, and the chosen objects (not strings) come back. Pass the parameters to skip the prompts;
# in CI or with -NonInteractive the defaults are used, or an error names the parameter to pass.
# The data is invented.
Import-Module ConsoleMonster

$components = @(
    [pscustomobject] @{ Name = 'Web front end'; Id = 'web'; Area = 'Applications'; Summary = 'IIS site and static files'; Size = '42 MB' }
    [pscustomobject] @{ Name = 'Public API'; Id = 'api'; Area = 'Applications'; Summary = 'REST API behind the load balancer'; Size = '18 MB' }
    [pscustomobject] @{ Name = 'Worker service'; Id = 'worker'; Area = 'Applications'; Summary = 'Background jobs and schedules'; Size = '9 MB' }
    [pscustomobject] @{ Name = 'Database migrations'; Id = 'db'; Area = 'Data'; Summary = 'Schema changes, run once per release'; Size = '1 MB' }
    [pscustomobject] @{ Name = 'Search index rebuild'; Id = 'search'; Area = 'Data'; Summary = 'Takes about 20 minutes on production'; Size = $null }
    [pscustomobject] @{ Name = 'CDN purge'; Id = 'cdn'; Area = 'Edge'; Summary = 'Clears cached pages and assets'; Size = $null }
)

Write-ConsoleText 'Release helper' -Role Accent -Style Bold

if (-not $Environment) {
    $Environment = Read-ConsoleSelection 'Environment' -Choices 'Production', 'Staging', 'Lab' -Default 'Staging' -ParameterName '-Environment' -Describe {
        switch ($_) { 'Production' { 'Customers see this one: changes need an approved ticket.' } 'Staging' { 'A copy of production refreshed nightly.' } default { 'Your own sandbox.' } }
    }
}
if ($Component) {
    $selected = $components | Where-Object { $_.Id -in $Component }
} else {
    $selected = Read-ConsoleMultiSelection 'Components' -Choices $components -Display Name -GroupBy Area -Describe Summary -Badge Size -Selected 'Web front end', 'Public API' -Minimum 1 -ParameterName '-Component'
}
if (-not $Release) {
    $Release = Read-ConsoleText 'Release name' -Default ('release-' + (Get-Date -Format 'yyyyMMdd')) -Pattern '[a-z0-9][a-z0-9.-]{2,40}' -PatternMessage 'Lower case letters, digits, dots and dashes' -ParameterName '-Release'
}

$question = "Deploy $(@($selected).Count) components to ${Environment}?"
if ($Force -or (Read-ConsoleConfirm $question -Default ($Environment -ne 'Production') -Destructive:($Environment -eq 'Production'))) {
    foreach ($item in $selected) {
        Write-ConsoleStatus 'Deploying ', $item.Name -Color Gray, Cyan -Spinner -ShowElapsed
        Start-Sleep -Milliseconds 700
    }
    Write-ConsoleStatus 'Deployed ', $Release, ' to ', $Environment -Color Gray, Cyan, Gray, Green -Status Pass -ShowElapsed -Complete
} else {
    Write-ConsoleText 'Nothing was deployed.' -Role Muted
}
