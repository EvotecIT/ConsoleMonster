Import-Module ConsoleMonster

#region Using the object directly
# RecordView's sections are a C# composition API. Use its ordinary object from PowerShell, then Show-Console.

# Resolve advanced C# types without registering global type accelerators.
$DatePresenterType = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Presenters.DatePresenter',
    $true)
$RecordViewType = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Widgets.RecordView',
    $true)
$RoleRuleType = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Presenters.RoleRule',
    $true)
$BooleanPresenterType = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Presenters.BooleanPresenter',
    $true)

# RecordView exposes sections and nested widgets through the shared C# API.
$now = [datetimeoffset]::new(2026, 10, 16, 14, 30, 0, [timespan]::FromHours(2))
$view = $RecordViewType::new('[bold]mail.evotec.xyz[/]')
$view.Summary = "TLS certificate · ECDSA P-256 · Let's Encrypt"
$view.Presentation.Now = $now
$view.Presentation.TimeZone = [timezoneinfo]::Utc
$null = $view.AddSection('Certificate')
$null = $view.AddField('Subject', 'CN=mail.evotec.xyz')
$null = $view.AddField('Issuer', "CN=R11, O=Let's Encrypt, C=US")
$null = $view.AddField('Serial', '04:A1:7F:3C:9E:22:51:0B:6D:8E:F0:13:44:2A:BC:91')
$both = $DatePresenterType::new('Both')
$null = $view.AddField('Valid from (UTC)',
    [datetimeoffset]::new(2026, 8, 9, 12, 0, 0, [timespan]::Zero), $both)
$expiryRules = [Array]::CreateInstance($RoleRuleType, 2)
$expiryRules[0] = $RoleRuleType::Below(0, [ConsoleMonster.Styling.ThemeToken]::Fail)
$expiryRules[1] = $RoleRuleType::Below(30, [ConsoleMonster.Styling.ThemeToken]::Warn)
$null = $view.AddField('Valid to (UTC)',
    [datetimeoffset]::new(2026, 11, 8, 12, 0, 0, [timespan]::Zero), $both, $expiryRules)
$null = $view.AddField('Key', 'ECDSA P-256')
$null = $view.AddField('OCSP stapling', $true)
$null = $view.AddField('Revoked', $false, $BooleanPresenterType::new($true))
$null = $view.AddField('Notes', $null)
$null = $view.AddGroup('Subject alternative names', '3')
$names = @(
    [pscustomobject] @{ Name = 'mail.evotec.xyz' }
    [pscustomobject] @{ Name = 'autodiscover.evotec.xyz' }
    [pscustomobject] @{ Name = 'mta-sts.evotec.xyz' }
) | New-ConsoleTable -Border None -NoHeader -RecordViewRows 0 -Property Name
$null = $view.AddWidget($names)
$null = $view.AddGroup('Chain', '3 certificates')
$chain = @(
    [pscustomobject] @{ Subject = 'CN=mail.evotec.xyz'
        Expires = [datetimeoffset]::new(2026, 11, 8, 12, 0, 0, [timespan]::Zero)
        Key = 'ECDSA P-256' }
    [pscustomobject] @{ Subject = "CN=R11, O=Let's Encrypt"
        Expires = [datetimeoffset]::new(2027, 3, 12, 23, 59, 59, [timespan]::Zero)
        Key = 'RSA 2048' }
    [pscustomobject] @{ Subject = 'CN=ISRG Root X1'
        Expires = [datetimeoffset]::new(2035, 6, 4, 11, 4, 38, [timespan]::Zero); Key = 'RSA 4096' }
) |
    New-ConsoleTable -Border Minimal -RecordViewRows 0 -Presentation (@{ Now = $now
        TimeZone = [timezoneinfo]::Utc }) -Property @(
        @{ Name = 'Subject'; Key = $true }
        @{ Name = 'Expires'; Presenter = $DatePresenterType::new('Relative')
            Role = @{ Below = 30; Role = 'Warn' } }
        @{ Name = 'Key'; Priority = 1 }
    )
$null = $view.AddWidget($chain)
#endregion
$view | Show-Console
