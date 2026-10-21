Import-Module ConsoleMonster

# Forty servers patched eight at a time. Three fail and one hangs past the four-second limit;
# the view lists the failures and -ErrorVariable keeps them for the rest of the script.
$servers = 1..40 | ForEach-Object { 'web{0:d2}' -f $_ }
$results = $servers | ForEach-ConsoleItem -Parallel -ThrottleLimit 8 -TimeoutSeconds 4 -Title 'Patching the web farm' -ErrorVariable failed -ErrorAction SilentlyContinue {
    #region more
    $number = [int] $_.Substring(3)
    Start-Sleep -Milliseconds (500 + ($number * 137) % 900)
    if ($number % 13 -eq 0) { throw "$_ did not come back after the reboot" }
    if ($number -eq 22) { Start-Sleep -Seconds 10 }
    #endregion
    "$_ patched"
}
