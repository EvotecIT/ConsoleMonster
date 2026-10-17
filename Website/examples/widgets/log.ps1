$log = New-ConsoleLog -Height 5
$log.Write('Discovered 4 domain controllers in 2 domains', 'info', [timespan]::FromSeconds(2))
$log.Write('DC02: replication partner DC05 last success 3h ago', 'warn', [timespan]::FromSeconds(31))
$log.Write('DC02: USN rollback check failed (event 2095)', 'err', [timespan]::FromSeconds(44))
$log.Write('Collected 186 rule inputs from DC01', 'info', [timespan]::FromSeconds(52))
$log.Write('Using cached schema snapshot', 'verbose', [timespan]::FromSeconds(53))
$log | Write-ConsoleWidget
