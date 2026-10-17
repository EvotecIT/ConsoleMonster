$results = New-ConsoleBarChart -Stacked
$results.Add('Passed', 412, 'Pass').
    Add('Warnings', 51, 'Warn').
    Add('Failed', 23, 'Fail').
    Add('Skipped', 14, 'Skip') | Write-ConsoleWidget
