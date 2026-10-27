Import-Module ConsoleMonster

New-ConsoleTree '[bold]evotec.xyz[/]' -Content {
    [pscustomobject]@{ Label = 'MX'; Status = 'Pass'; Detail = '2 records'; Children = @(& {
        [pscustomobject]@{ Label = 'evotec-xyz.mail.protection.outlook.com'; Detail = 'priority 0' }
    }) }
    [pscustomobject]@{ Label = 'SPF'; Status = 'Warn'; Detail = '9 of 10 DNS lookups'; Children = @(& {
        'include:spf.protection.outlook.com'
        'include:_spf.salesforce.com'
    }) }
    [pscustomobject]@{ Label = 'DMARC'; Status = 'Fail'; Detail = 'p=none' }
    [pscustomobject]@{ Label = 'DKIM'; Status = 'Pass'; Detail = 'selector1, selector2' }
} | Show-Console
