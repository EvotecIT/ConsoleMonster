Import-Module ConsoleMonster
Enable-ConsoleFormatting

# A path list shows one folder per line; folders that do not exist are dimmed.
Get-ChildItem Env:PATH

# Drives with a usage bar, then the operating system as one record.
Get-CimInstance Win32_LogicalDisk
Get-CimInstance Win32_OperatingSystem

# Updates, optional features and firewall rules, each with a line that sums them up.
Get-HotFix
Get-WindowsOptionalFeature -Online
Get-NetFirewallRule

# A web response that failed: the status takes the colour of its class.
Invoke-WebRequest https://status.example.test/health -SkipHttpErrorCheck

# A file hash in full, with the path relative to the current folder.
Set-Location -Path ([IO.Path]::GetTempPath())
Set-Content -Path ./release.zip -Value 'ConsoleMonster example package' -NoNewline
Get-FileHash -Path ./release.zip
