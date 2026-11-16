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
BarChart chart = Ui.BarChart().Maximum(100).Unit("%").Warning(80).Critical(90);
foreach (var volume in used)
    chart.Add(volume.Name, volume.Percent);
Tree tree = Ui.Tree().Title("./");
foreach (var folder in folders)
{
    TreeNode node = tree.Add(folder.Path);
    foreach (var child in folder.Children)
        node.Add(child.Label, null, child.Size);
}

Rows report = Ui.Rows().Gap(1).Content(rowsContent2 =>
{
    rowsContent2.Rule("[accent bold]Storage review[/] [muted]· capacity planning[/]");
    rowsContent2.Columns(columns => columns.Content(columnsContent =>
    {
        columnsContent.Panel(p => p.Title("Used / 100 GiB").Content(panelContent =>
        {
            panelContent.Add(chart);
        }));
        columnsContent.Panel(p => p.Title("Largest folders").Content(panelContent =>
        {
            panelContent.Add(tree);
        }));
    }));
    rowsContent2.Panel(p => p.Title("Next action").Content(panelContent =>
    {
        panelContent.Rows(r => r.Content(rowsContent =>
        {
            rowsContent.Markup(
                "[fail]Artifacts is at 94%.[/] Review the retention policy before the next build.");
            rowsContent.Markup(
                "[muted]Keep the current release; archive history older than 30 days.[/]");
        }));
    }));
});

// Behaviour: print once. Review recommendations before connecting any cleanup action.
report.Show();

record UsedRow(string Name, int Percent);

record FolderRow(string Path, (string Label, string Size)[] Children);
