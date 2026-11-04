Import-Module ConsoleMonster

# One line for the whole run: a bar, counters, elapsed time and the time left.
New-ConsoleSummaryBar -Value 30 -Total 40 -ElapsedSeconds 42 `
    -Counter ([ordered] @{ passed = 27; failed = 2; skipped = 1 }) | Show-Console
