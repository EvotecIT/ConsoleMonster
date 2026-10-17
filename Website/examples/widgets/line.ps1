$chart = New-ConsoleLineChart -Height 7
$null = $chart.AddSeries('completed', 'Accent')
$null = $chart.AddSeries('failed', 'Fail')
foreach ($i in 0..47) {
    $null = $chart.Add('completed', 20 + 9 * [math]::Sin($i / 7) + $i % 3)
    $null = $chart.Add('failed', 3 + 2 * [math]::Cos($i / 5))
}
$chart | Write-ConsoleWidget
