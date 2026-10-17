$tree = [ConsoleMonster.Widgets.Tree]::new('[bold]evotec.xyz[/]')
$mx = $tree.Add('MX', 'Pass', '2 records')
$mx.Add('evotec-xyz.mail.protection.outlook.com', $null, 'priority 0') | Out-Null
$spf = $tree.Add('SPF', 'Warn', '9 of 10 DNS lookups')
$spf.Add('include:spf.protection.outlook.com') | Out-Null
$spf.Add('include:_spf.salesforce.com') | Out-Null
$tree.Add('DMARC', 'Fail', 'p=none') | Out-Null
$tree.Add('DKIM', 'Pass', 'selector1, selector2') | Out-Null
$tree | Write-ConsoleWidget
