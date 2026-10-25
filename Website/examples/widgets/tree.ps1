Import-Module ConsoleMonster

New-ConsoleTree '[bold]evotec.xyz[/]' -Content {
    New-ConsoleTreeNode 'MX' -Status Pass -Detail '2 records' -Content {
        New-ConsoleTreeNode 'evotec-xyz.mail.protection.outlook.com' -Detail 'priority 0'
    }
    New-ConsoleTreeNode 'SPF' -Status Warn -Detail '9 of 10 DNS lookups' -Content {
        'include:spf.protection.outlook.com'
        'include:_spf.salesforce.com'
    }
    New-ConsoleTreeNode 'DMARC' -Status Fail -Detail 'p=none'
    New-ConsoleTreeNode 'DKIM' -Status Pass -Detail 'selector1, selector2'
} | Show-Console
