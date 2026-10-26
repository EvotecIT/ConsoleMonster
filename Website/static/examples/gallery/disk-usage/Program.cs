// Turn a storage inventory into threshold bars and a directory breakdown.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// Data: synthetic sizes in GiB and platform-neutral paths; no filesystem scan is performed.
var used = new UsedRow[]
{
    new("Workspace", 62),
    new("Cache", 88),
    new("Artifacts", 94),
    new("Archive", 35)
};
var folders = new FolderRow[]
{
    new("./workspace", new[] { ("src", "12 GiB"), ("packages", "31 GiB"), ("fixtures", "19 GiB") }),
    new("./cache", new[] { ("dependencies", "63 GiB"), ("downloads", "25 GiB") }),
    new("./artifacts", new[] { ("current", "18 GiB"), ("history", "76 GiB") })
};
// Layout: bars answer where pressure is; the tree explains what occupies the space.
BarChart chart = new BarChart
{
    Maximum = 100,
    Unit = "%",
    Warning = 80,
    Critical = 90
};
foreach (var volume in used)
    chart.Add(volume.Name, volume.Percent);
Tree tree = new Tree("./");
foreach (var folder in folders)
{
    TreeNode node = tree.Add(folder.Path);
    foreach (var child in folder.Children)
        node.Add(child.Label, null, child.Size);
}

Rows report = new Rows(new Rule("[bold]Storage review[/] [muted]· capacity planning[/]"), new Columns(new Panel() { Title = "Used / 100 GiB", Content = chart }, new Panel() { Title = "Largest folders", Content = tree }), new Panel() { Title = "Next action", Content = new Rows(new TextBlock("[warn]Artifacts is at 94%.[/] Review the retention policy before the next build."), new TextBlock("[muted]Keep the current release; archive history older than 30 days.[/]")) })
{
    Gap = 1
};
// Behaviour: print once. Review recommendations before connecting any cleanup action.
Terminal.Write(report);

record UsedRow(string Name, int Percent);
record FolderRow(string Path, (string Label, string Size)[] Children);
