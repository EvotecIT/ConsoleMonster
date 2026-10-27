Import-Module ConsoleMonster

# An explicit sample ErrorRecord: the same error view without machine-specific paths or stack frames.
$missing = New-Object IO.FileNotFoundException -ArgumentList 'The weekly report could not be loaded.', 'Weekly-2026-43.pdf'
$problem = New-Object Management.Automation.ErrorRecord -ArgumentList $missing, 'WeeklyReportMissing', 'ObjectNotFound', 'Weekly-2026-43.pdf'
$problem | Show-Console -NoStack -View Error
