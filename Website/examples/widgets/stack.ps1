$load = 12, 14, 13, 18, 22, 26, 25, 31, 36, 34, 40, 44, 41, 47, 52, 49, 55, 61, 58, 64, 70, 66, 73, 79
New-ConsolePanel -Title 'Stack' -Caption 'badge over a chart' {
    New-ConsoleStack {
        New-ConsoleSparkline -Height 3 -Value $load -NoLatest
        New-ConsoleAlign -Horizontal Right { '[pass] LIVE [/]' }
    }
} | Write-ConsoleWidget
