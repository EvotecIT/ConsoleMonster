using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Tree tree = new Tree("[bold]evotec.xyz[/]");
TreeNode mx = tree.Add("MX", StatusKind.Pass, "2 records");
mx.Add("evotec-xyz.mail.protection.outlook.com", null, "priority 0");
TreeNode spf = tree.Add("SPF", StatusKind.Warn, "9 of 10 DNS lookups");
spf.Add("include:spf.protection.outlook.com");
spf.Add("include:_spf.salesforce.com");
tree.Add("DMARC", StatusKind.Fail, "p=none");
tree.Add("DKIM", StatusKind.Pass, "selector1, selector2");
Terminal.Write(tree);