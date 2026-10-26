using ConsoleMonster;
using ConsoleMonster.Changes;

string before = string.Join(
    "\n",
    new[] { "# Web service", "host=web-01", "port=80", "protocol=http", "workers=4", "timeout=30",
        "retries=3", "log=information", "health=/status", "enabled=true", "" });
string after = string.Join(
    "\n",
    new[] { "# Web service", "host=web-01", "port=443", "protocol=https", "workers=4", "timeout=30",
        "retries=3", "log=information", "health=/status", "enabled=true", "" });

// The headings default to Before and After; use ReferenceLabel and DifferenceLabel to name them.
TextDiffResult comparison = TextDiff.Compare(before, after, new TextDiffOptions { Context = 1 });
Terminal.Write(new TextDiffView(comparison));
