using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Phases migration = new Phases()
    .Set("Discover", StatusKind.Pass, default, "2.1s")
    .Set("Snapshot", StatusKind.Pass, default, "12s")
    .Set("Migrate", StatusKind.Running, default, "6/12")
    .Set("Verify")
    .Set("Report");

// The running phase fans out to its items: failed and running ones stay visible, the rest fold into +N more.
migration.SetItem("Migrate", "alice", StatusKind.Pass, default, "1.1s");
migration.SetItem("Migrate", "bob", StatusKind.Pass, default, "1.5s");
migration.SetItem("Migrate", "carol", StatusKind.Pass, default, "1.9s");
migration.SetItem("Migrate", "dave", StatusKind.Pass, default, "2.3s");
migration.SetItem("Migrate", "erin", StatusKind.Running, default, "40%");
migration.SetItem("Migrate", "frank", StatusKind.Fail, default, "access denied");
migration.SetItem("Migrate", "grace");
migration.SetItem("Migrate", "heidi");
migration.SetItem("Migrate", "ivan");
migration.SetItem("Migrate", "judy");
migration.SetItem("Migrate", "mallory");
migration.SetItem("Migrate", "niaj");
Terminal.Write(migration);
