$chart = New-ConsoleLineChart -Height 7 -Scatter -Unit 'ms'
$null = $chart.AddSeries('latency', 'Accent2')
foreach ($i in 0..59) {
    $null = $chart.Add('latency', 40 + ($i * 37) % 23 + $(if ($i % 7 -eq 0) { 30 } else { 0 }))
}
$chart | Write-ConsoleWidget
