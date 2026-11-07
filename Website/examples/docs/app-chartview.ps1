Import-Module ConsoleMonster

$times = '09:00', '09:05', '09:10', '09:15', '09:20', '09:25', '09:30', '09:35'
$cpu = 22, 25, 31, 64, 88, 71, 40, 28
$samples = for ($i = 0; $i -lt $times.Count; $i++) {
    [pscustomobject] @{ Time = $times[$i]; Cpu = $cpu[$i] }
}

$chart = New-ConsoleChart -Type Line -Unit '%' -Series @([pscustomobject] @{ Key = 'cpu'
    Values = $cpu })
Show-ConsoleApp -Content {
    New-ConsoleSplitter -Ratio 0.3 -First {
        New-ConsoleDataGrid -Key Samples -InputObject $samples -LinkKey Sample
    } -Second {
        New-ConsoleChartView -Key Cpu -Chart $chart -PointLabels $times -LinkKey Sample -Height 9
    }
}