using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// One cell per host and port, filled row by row, with a legend of counts.
var map = new CellMap { Columns = 4, LegendLines = 2 };
foreach (string host in new[] { "web-01", "web-02", "api-01", "db-01" }) {
    map.RowLabels.Add(host);
}
StatusKind[] states = {
    StatusKind.Pass, StatusKind.Pass, StatusKind.Skip, StatusKind.Skip,
    StatusKind.Pass, StatusKind.Warn, StatusKind.Skip, StatusKind.Skip,
    StatusKind.Pass, StatusKind.Pass, StatusKind.Fail, StatusKind.Pass,
    StatusKind.Skip, StatusKind.Skip, StatusKind.Pass, StatusKind.Pass
};
foreach (StatusKind state in states) {
    map.Add(state);
}
Terminal.Write(map);
