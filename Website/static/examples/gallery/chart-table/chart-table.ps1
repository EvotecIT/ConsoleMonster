# Explore a load chart linked to the table of samples it was drawn from.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.
$cpu = @(
    32, 35, 41, 38, 44, 52, 61, 58, 66, 72, 69, 63
    57, 54, 60, 68, 75, 81, 77, 70, 62, 55, 49, 45
)
$memory = @(
    48, 49, 50, 51, 51, 52, 54, 55, 57, 58, 59, 60
    61, 61, 62, 63, 65, 66, 67, 68, 68, 69, 70, 71
)
$samples = for ($i = 0; $i -lt $cpu.Count; $i++) {
    $minutes = $i * 5
    [pscustomobject]@{
        Time   = '{0:D2}:{1:D2}' -f (9 + [int][math]::Floor($minutes / 60)), ($minutes % 60)
        Cpu    = $cpu[$i]
        Memory = $memory[$i]
    }
}

# Layout: construct the widgets shown on screen.
$chart = New-ConsoleChart -Type Line -Unit '%' -Series @(
    [pscustomobject]@{ Key = 'cpu'; Values = $cpu }
    [pscustomobject]@{ Key = 'memory'; Values = $memory }
)
$content = {
    New-ConsoleDataGrid -Key Samples -InputObject $samples -Height 7 -LinkKey Sample
    New-ConsoleChartView -Key Load -Chart $chart -PointLabels $samples.Time -LinkKey Sample
}

# Behaviour: Up/Down picks a sample and the crosshair follows; in the chart, Left/Right picks a
# point and the row follows, + and - zoom, 1 and 2 hide a series; Escape exits.
Show-ConsoleApp -Content $content
