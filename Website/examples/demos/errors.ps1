Import-Module ConsoleMonster

# An explicit sample ErrorRecord: the same error view without machine-specific paths or stack frames.
$missing = [IO.FileNotFoundException]::new('The weekly report could not be loaded.', 'Weekly-2026-43.pdf')
$problem = [Management.Automation.ErrorRecord]::new($missing, 'WeeklyReportMissing', 'ObjectNotFound', 'Weekly-2026-43.pdf')
$problem | Show-Console -NoStack -View Error
