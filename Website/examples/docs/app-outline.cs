using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;

Outline sections = new Outline("Sections")
{
    Items =
    {
        new OutlineItem("Install"),
        new OutlineItem("Windows", 2),
        new OutlineItem("Linux", 2),
        new OutlineItem("Configure"),
        new OutlineItem("Run")
    },
    SelectedIndex = 0
};
Breadcrumb where = new Breadcrumb("Where") { Ancestors = { "Guide" }, Outline = sections };
ConsoleApp.Run(new Rows(where, sections));
