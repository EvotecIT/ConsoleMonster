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
migration.Item("Migrate", "alice", StatusKind.Pass, default, "1.1s");
migration.Item("Migrate", "bob", StatusKind.Pass, default, "1.5s");
migration.Item("Migrate", "carol", StatusKind.Pass, default, "1.9s");
migration.Item("Migrate", "dave", StatusKind.Pass, default, "2.3s");
migration.Item("Migrate", "erin", StatusKind.Running, default, "40%");
migration.Item("Migrate", "frank", StatusKind.Fail, default, "access denied");
migration.Item("Migrate", "grace");
migration.Item("Migrate", "heidi");
migration.Item("Migrate", "ivan");
migration.Item("Migrate", "judy");
migration.Item("Migrate", "mallory");
migration.Item("Migrate", "niaj");
Terminal.Write(migration);
