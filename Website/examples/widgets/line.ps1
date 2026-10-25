Import-Module ConsoleMonster

$completed = foreach ($i in 0..47) { 20 + 9 * [math]::Sin($i / 7) + $i % 3 }
$failed = foreach ($i in 0..47) { 3 + 2 * [math]::Cos($i / 5) }
New-ConsoleChart -Height 7 -Content {
    [pscustomobject]@{ Key = 'completed'; Role = 'Accent'; Values = $completed }
    [pscustomobject]@{ Key = 'failed'; Role = 'Fail'; Values = $failed }
} -Type Line | Show-Console
