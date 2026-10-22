Import-Module ConsoleMonster

# Forty servers, eight at a time: three fail and one hangs past the four-second limit.
$servers = 1..40 | ForEach-Object { 'web{0:d2}' -f $_ }
$run = @{ Parallel = $true; ThrottleLimit = 8; TimeoutSeconds = 4; Title = 'Patching' }
$results = $servers | ForEach-ConsoleItem @run -ErrorAction SilentlyContinue {
    Start-Sleep -Milliseconds (500 + ([int] $_.Substring(3) * 137) % 900)
    if ($_ -in 'web13', 'web26', 'web39') { throw "$_ did not come back" }
    if ($_ -eq 'web22') { Start-Sleep -Seconds 10 }
    "$_ patched"
}
