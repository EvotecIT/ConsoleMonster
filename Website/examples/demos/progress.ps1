Import-Module ConsoleMonster

# An ordinary Write-Progress loop, drawn by ConsoleMonster in a portable progress scope.
Enable-ConsoleFormatting -Feature Progress
Use-ConsoleProgress {
    $sites = 'Warsaw', 'Dublin', 'Berlin'
    for ($i = 0; $i -lt $sites.Count; $i++) {
        Write-Progress -Id 1 -Activity 'Collecting inventory' -Status $sites[$i] -PercentComplete ($i * 100 / $sites.Count)
        foreach ($server in 1..12) {
            Write-Progress -Id 2 -ParentId 1 -Activity "Servers in $($sites[$i])" -Status "SRV$server" -PercentComplete ($server * 100 / 12)
            # Redirected output has no live progress area; keep the ordinary pause there.
            if ($null -ne $ConsoleSession) {
                Wait-ConsoleDashboard -Milliseconds 110
            } else {
                Start-Sleep -Milliseconds 110
            }
        }
        Write-Host "$($sites[$i]) done: 12 servers"
    }
    Write-Progress -Id 2 -Activity 'Servers' -Completed
    Write-Progress -Id 1 -Activity 'Collecting inventory' -Completed
} -Keep
Disable-ConsoleFormatting
Write-ConsoleText 'Inventory collected: 36 servers across 3 sites.'
