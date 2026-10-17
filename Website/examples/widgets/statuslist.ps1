$steps = New-ConsoleStatusList
$steps.Add('Restore NuGet packages', 'Pass', '1.6 s').
    Add('Compile ConsoleMonster', 'Pass', '4.7 s').
    Add('Run Pester on PowerShell 7', 'Warn', '1 skipped').
    Add('Sign module files', 'Skip', 'skipped').
    Add('Pack ConsoleMonster.2.0.0.nupkg', 'Running').
    Add('Publish to PowerShell Gallery', 'Pending') | Write-ConsoleWidget
