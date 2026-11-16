using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// One line per pipeline that folds to fit its column: neighbours of the current stage become +N earlier and +N more.
static Phases Stages(params StatusKind[] status)
{
    Phases stages = new Phases { Compact = true };
    string[] names = { "Fetch", "Build", "Test", "Pack", "Publish" };
    for (int i = 0; i < names.Length; i++)
    {
        stages.Set(names[i], status[i]);
    }
    return stages;
}

Table table = new Table().Column("Repository").Column("Stages");
table.Values("ConsoleMonster",
    Stages(StatusKind.Pass, StatusKind.Pass, StatusKind.Running, StatusKind.Pending,
        StatusKind.Pending));
table.Values("PSPublishModule",
    Stages(StatusKind.Pass, StatusKind.Fail, StatusKind.Pending, StatusKind.Pending,
        StatusKind.Pending));
table.Values("DnsClientX",
    Stages(StatusKind.Pass, StatusKind.Pass, StatusKind.Pass, StatusKind.Pass, StatusKind.Pass));
Terminal.Write(table);
