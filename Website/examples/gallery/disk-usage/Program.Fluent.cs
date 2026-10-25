// Turn a storage inventory into threshold bars and a directory breakdown.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// Data: synthetic sizes in GiB and platform-neutral paths; no filesystem scan is performed.
var used = new[]
{
    (Name: "Workspace", Percent: 62),
    (Name: "Cache", Percent: 88),
    (Name: "Artifacts", Percent: 94),
    (Name: "Archive", Percent: 35)
};
var folders = new[]
{
    (Path: "./workspace", Children: new[] { ("src", "12 GiB"), ("packages", "31 GiB"), ("fixtures", "19 GiB") }),
    (Path: "./cache", Children: new[] { ("dependencies", "63 GiB"), ("downloads", "25 GiB") }),
    (Path: "./artifacts", Children: new[] { ("current", "18 GiB"), ("history", "76 GiB") })
};
// Layout: bars answer where pressure is; the tree explains what occupies the space.
BarChart chart = new BarChart().Maximum(100).Unit("%").Warning(80).Critical(90);
foreach (var volume in used)
    chart.Add(volume.Name, volume.Percent);
Tree tree = new Tree("./");
foreach (var folder in folders)
{
    TreeNode node = tree.Add(folder.Path);
    foreach (var child in folder.Children)
        node.Add(child.Item1, null, child.Item2);
}

Rows report = new Rows(new Rule("[bold]Storage review[/] [muted]· capacity planning[/]"), new Columns(new Panel().Title("Used / 100 GiB").Content(c => c.Add(chart)), new Panel().Title("Largest folders").Content(c => c.Add(tree))), new Panel().Title("Next action").Content(c => c.Add(new Rows(new TextBlock("[warn]Artifacts is at 94%.[/] Review the retention policy before the next build."), new TextBlock("[muted]Keep the current release; archive history older than 30 days.[/]"))))).Gap(1);
// Behaviour: print once. Review recommendations before connecting any cleanup action.
report.Show();
