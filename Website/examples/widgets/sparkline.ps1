$rate = 18, 21, 19, 24, 27, 26, 31, 29, 34, 38, 36, 41, 39, 44, 47, 45, 50, 48, 53, 57, 54, 59, 62, 60
New-ConsoleRows -Gap 1 {
    New-ConsoleSparkline -Label 'rules/s' -Unit '/s' -Value $rate
    New-ConsoleSparkline -Height 3 -Unit '/s' -Value $rate
} | Write-ConsoleWidget
