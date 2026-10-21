# Twelve servers patched in parallel, each writing its progress straight to the console (PowerShell 7).
$servers = 1..12 | ForEach-Object { 'web{0:d2}' -f $_ }
$servers | ForEach-Object -ThrottleLimit 12 -Parallel {
    foreach ($step in 'copying', 'stopping', 'swapping', 'starting') {
        Write-Host "$_ $step" -NoNewline
        Start-Sleep -Milliseconds (Get-Random -Minimum 40 -Maximum 240)
        Write-Host ' ... done'
    }
}