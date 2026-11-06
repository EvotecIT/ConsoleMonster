# An incident workbench: samples linked to their chart, a resizable split and a runbook.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.
$p50 = @(
    41, 43, 40, 44, 42, 45, 47, 46, 52, 88, 131, 164
    158, 120, 84, 61, 52, 48, 46, 44, 45, 43, 42, 44
)
$p95 = @(
    96, 98, 95, 101, 99, 104, 108, 112, 140, 262, 410, 486
    472, 355, 230, 160, 128, 117, 110, 104, 103, 100, 98, 101
)
$errors = @(0, 0, 0, 1, 0, 0, 1, 0, 3, 18, 47, 62, 58, 31, 12, 4, 1, 0, 0, 1, 0, 0, 0, 0)
$samples = for ($i = 0; $i -lt $p50.Count; $i++) {
    $minutes = $i * 5
    [pscustomobject]@{
        Time   = '{0:D2}:{1:D2}' -f (13 + [int][math]::Floor($minutes / 60)), ($minutes % 60)
        P50    = $p50[$i]
        P95    = $p95[$i]
        Errors = $errors[$i]
    }
}

# Layout: construct the widgets shown on screen.
$chart = New-ConsoleChart -Type Line -Unit ' ms' -Series @(
    [pscustomobject]@{ Key = 'p50'; Values = $p50 }
    [pscustomobject]@{ Key = 'p95'; Values = $p95 }
)
$latency = New-ConsoleSplitter -Key Panes -Ratio 0.4 -Fill -First {
    New-ConsoleDataGrid -Key Samples -InputObject $samples -LinkKey Sample
} -Second {
    New-ConsoleChartView -Key Latency -Chart $chart -PointLabels $samples.Time -LinkKey Sample
}
$done = @{ OnText = 'Done'; OffText = 'To do' }
$check = New-ConsoleCollapsible -Key Check -Expanded -Content {
    New-ConsoleToggle -Key Dashboards -Label 'Error rate confirmed on the dashboard' -Value $true @done
    New-ConsoleToggle -Key Deploys -Label 'Deploy at 13:40 matches the first spike' -Value $true @done
}
$mitigate = New-ConsoleCollapsible -Key Mitigate -Content {
    New-ConsoleToggle -Key Rollback -Label 'Roll back api-gateway to 4.12.1' @done
    New-ConsoleToggle -Key Drain -Label 'Drain the eu-west-2 pool' @done
}
$verify = New-ConsoleCollapsible -Key Verify -Content {
    New-ConsoleToggle -Key P95Normal -Label 'p95 back under 150 ms for 15 minutes' @done
}
$steps = New-ConsoleOutline -Key Steps -SelectedIndex 0 -Items @(
    [pscustomobject]@{ Title = 'Check'; Target = $check }
    [pscustomobject]@{ Title = 'Mitigate'; Target = $mitigate }
    [pscustomobject]@{ Title = 'Verify'; Target = $verify }
)
$runbook = New-ConsoleSplitter -Ratio 0.22 -Fill -First { $steps } -Second {
    New-ConsoleAccordion -Content { $check; $mitigate; $verify }
}
$content = {
    New-ConsoleTabs -Key View -Content {
        @{ Title = 'Latency'; Content = $latency }
        @{ Title = 'Runbook'; Content = $runbook }
    }
}

# Behaviour: click or arrow through samples and the chart follows; drag the divider, scroll the
# wheel over the chart to zoom, click a legend entry to hide its series, right-click for the
# keys that work there, and click a tab, an outline entry or a toggle. Escape exits.
Show-ConsoleApp -Title 'Incident 4127 · api-gateway latency' -Content $content
