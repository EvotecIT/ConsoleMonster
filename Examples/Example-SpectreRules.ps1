Import-Module .\ConsoleMonster.psd1 -Force

New-SpectreRule -Text 'This is my rules!', 'centered' -Color yellow, green -RuleColor blue
New-SpectreRule -Text 'This is my rules!', 'left aligned' -Color yellow, green -RuleColor blue -Align Left